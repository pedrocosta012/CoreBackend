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
    public async Task RegisterOwner_ShouldReturn201AndTokens_WhenPayloadIsValid()
    {
        var cpf = TestCpf.Generate();
        var response = await _httpClient.PostAsJsonAsync("/auth/register/owner",
            new AuthOwnerRegisterPayload(
                "João", "Silva", cpf,
                $"register-{Guid.NewGuid():N}@example.com",
                "11999990000",
                "Strong@Pass1",
                $"Company-{Guid.NewGuid():N}",
                "headquarters",
                ""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("authToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("refreshToken").GetString()));
    }

    [Fact]
    public async Task RegisterOwner_ShouldReturn400_WhenCpfIsInvalid()
    {
        var response = await _httpClient.PostAsJsonAsync("/auth/register/owner",
            new AuthOwnerRegisterPayload("João", "Silva", "11111111111",
                $"badcpf-{Guid.NewGuid():N}@example.com", "11999990000", "Strong@Pass1",
                "Company", "headquarters", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
    public async Task Me_ShouldReturn200WithUserData_WhenAuthTokenIsValid()
    {
        var cpf = TestCpf.Generate();
        var registerResponse = await _httpClient.PostAsJsonAsync("/auth/register/owner",
            new AuthOwnerRegisterPayload("Maria", "Santos", cpf,
                $"me-{Guid.NewGuid():N}@example.com", "11999990000", "Strong@Pass1",
                $"Company-{Guid.NewGuid():N}", "headquarters", ""));
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registerBody = await registerResponse.Content.ReadAsStringAsync();
        using var registerDoc = JsonDocument.Parse(registerBody);
        var authToken = registerDoc.RootElement.GetProperty("authToken").GetString() ?? string.Empty;

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        var meResponse = await _httpClient.GetAsync("/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var meBody = await meResponse.Content.ReadAsStringAsync();
        using var meDoc = JsonDocument.Parse(meBody);
        Assert.Equal("Maria", meDoc.RootElement.GetProperty("firstName").GetString());
        Assert.Equal("Santos", meDoc.RootElement.GetProperty("lastName").GetString());
        Assert.Equal(cpf, meDoc.RootElement.GetProperty("cpf").GetString());
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

internal sealed record AuthOwnerRegisterPayload(string FirstName, string LastName, string Cpf, string Email, string Phone,
    string Password, string CompanyName, string OfficeType, string? TaxId);
internal sealed record RefreshRequest(string RefreshToken);
internal sealed record ForgotPasswordRequest(string Email);
internal sealed record ResetPasswordRequest(string Token, string NewPassword);
