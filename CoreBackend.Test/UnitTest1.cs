using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreBackend.Test.Support;
using Microsoft.Data.Sqlite;

namespace CoreBackend.Test;

public sealed class LoginRouteTests
{
    [Fact]
    public async Task UserTableMustExistOnDatabase()
    {
        using var factory = new TestApiFactory();
        using var _ = factory.CreateClient();

        await using var connection = new SqliteConnection(factory.SqliteConnectionString);
        await connection.OpenAsync();

        const string sql = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'user' LIMIT 1;";
        await using var command = new SqliteCommand(sql, connection);
        var result = await command.ExecuteScalarAsync();
        Assert.Equal("user", Convert.ToString(result));
    }

    [Fact]
    public async Task LoginMustReturnTokensAndStatus200WhenSuccess()
    {
        using var factory = new TestApiFactory();
        using var httpClient = factory.CreateClient();

        var response = await httpClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(TestUsers.ValidLoginIdentifier, TestUsers.ValidLoginPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(body), "O corpo da resposta deve conter os tokens.");

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("authToken", out var authTokenElement), "O campo 'authToken' deve existir.");
        Assert.True(root.TryGetProperty("refreshToken", out var refreshTokenElement), "O campo 'refreshToken' deve existir.");

        var authToken = authTokenElement.GetString() ?? string.Empty;
        var refreshToken = refreshTokenElement.GetString() ?? string.Empty;

        Assert.False(string.IsNullOrWhiteSpace(authToken), "O authToken deve ser preenchido.");
        Assert.False(string.IsNullOrWhiteSpace(refreshToken), "O refreshToken deve ser preenchido.");
        Assert.Equal(2, authToken.Count(static character => character == '.'));
        Assert.True(refreshToken.Length >= 32, "O refreshToken deve ter entropia suficiente.");
    }

    [Fact]
    public async Task LoginMustReturn401WithoutBodyWhenFail()
    {
        using var factory = new TestApiFactory();
        using var httpClient = factory.CreateClient();

        var response = await httpClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest($"invalid-{Guid.NewGuid():N}@example.com", "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(string.IsNullOrEmpty(body), "Quando retornar 401, o body deve estar vazio.");
    }
}

internal sealed record LoginRequest(string Identifier, string Password);
