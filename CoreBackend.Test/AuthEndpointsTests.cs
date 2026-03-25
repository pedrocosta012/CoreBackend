using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CoreBackend.Test.Support;

namespace CoreBackend.Test;

public sealed class AuthEndpointsTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly TestApiFactory _factory;

    public AuthEndpointsTests()
    {
        _factory = new TestApiFactory();
        _httpClient = _factory.CreateClient();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Register_ShouldReturn201AndTokens_WhenPayloadIsValid()
    {
        var request = new RegisterRequest(
            Username: $"register-{Guid.NewGuid():N}",
            Email: $"register-{Guid.NewGuid():N}@example.com",
            Phone: "11999990000",
            Password: "Strong@Pass1");

        var response = await _httpClient.PostAsJsonAsync("/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        var authToken = root.GetProperty("authToken").GetString() ?? string.Empty;
        var refreshToken = root.GetProperty("refreshToken").GetString() ?? string.Empty;
        Assert.False(string.IsNullOrWhiteSpace(authToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));
    }

    [Fact]
    public async Task Refresh_ShouldReturn200WithNewTokens_WhenRefreshTokenIsValid()
    {
        var loginPayload = new LoginRequest(TestUsers.ValidLoginIdentifier, TestUsers.ValidLoginPassword);
        var loginResponse = await _httpClient.PostAsJsonAsync("/auth/login", loginPayload);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        using var loginDocument = JsonDocument.Parse(loginBody);
        var refreshToken = loginDocument.RootElement.GetProperty("refreshToken").GetString() ?? string.Empty;

        var refreshResponse = await _httpClient.PostAsJsonAsync("/auth/refresh", new RefreshRequest(refreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshBody = await refreshResponse.Content.ReadAsStringAsync();
        using var refreshDocument = JsonDocument.Parse(refreshBody);
        Assert.False(string.IsNullOrWhiteSpace(refreshDocument.RootElement.GetProperty("authToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(refreshDocument.RootElement.GetProperty("refreshToken").GetString()));
    }

    [Fact]
    public async Task Me_ShouldReturn200_WhenAuthTokenIsValid()
    {
        var loginResponse = await _httpClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(TestUsers.ValidLoginIdentifier, TestUsers.ValidLoginPassword));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        using var loginDocument = JsonDocument.Parse(loginBody);
        var authToken = loginDocument.RootElement.GetProperty("authToken").GetString() ?? string.Empty;

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        var meResponse = await _httpClient.GetAsync("/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldInvalidateRefreshToken_WhenUserIsAuthenticated()
    {
        var loginResponse = await _httpClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(TestUsers.ValidLoginIdentifier, TestUsers.ValidLoginPassword));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        using var loginDocument = JsonDocument.Parse(loginBody);
        var authToken = loginDocument.RootElement.GetProperty("authToken").GetString() ?? string.Empty;
        var refreshToken = loginDocument.RootElement.GetProperty("refreshToken").GetString() ?? string.Empty;

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        var logoutResponse = await _httpClient.PostAsync("/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshResponse = await _httpClient.PostAsJsonAsync("/auth/refresh", new RefreshRequest(refreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturn200_ForUnknownEmail()
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/auth/forgot-password",
            new ForgotPasswordRequest($"unknown-{Guid.NewGuid():N}@example.com"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn401_WhenTokenIsInvalid()
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/auth/reset-password",
            new ResetPasswordRequest(Guid.NewGuid().ToString(), "New@Pass123"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

internal sealed record RegisterRequest(string Username, string Email, string Phone, string Password);
internal sealed record RefreshRequest(string RefreshToken);
internal sealed record ForgotPasswordRequest(string Email);
internal sealed record ResetPasswordRequest(string Token, string NewPassword);

