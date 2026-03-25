using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CoreBackend.Test;

public sealed class UserCrudTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly TestApiFactory _factory;

    public UserCrudTests()
    {
        _factory = new TestApiFactory();
        _httpClient = _factory.CreateClient();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _factory.Dispose();
    }

    private async Task<HttpResponseMessage> CreateUserAsync(CreateUserRequest request)
    {
        return await _httpClient.PostAsJsonAsync("/users", request);
    }

    private async Task<string> CreateUserAndGetIdAsync()
    {
        var uniqueEmail = $"test-{Guid.NewGuid():N}@example.com";
        var request = new CreateUserRequest($"user-{Guid.NewGuid():N}", uniqueEmail, "11999999999", "Valid@Pass1");
        var response = await CreateUserAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetString() ?? string.Empty;
    }

    #region POST /users

    [Fact]
    public async Task CreateUser_ShouldReturn201_WhenDataIsValid()
    {
        var uniqueEmail = $"create-{Guid.NewGuid():N}@example.com";
        var username = $"john-{Guid.NewGuid():N}";
        var request = new CreateUserRequest(username, uniqueEmail, "11988887777", "Str0ng@Pass");

        var response = await CreateUserAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("id", out var idElement), "Resposta deve conter 'id'.");
        Assert.False(string.IsNullOrWhiteSpace(idElement.GetString()), "O id deve ser preenchido.");

        Assert.True(root.TryGetProperty("username", out var usernameElement), "Resposta deve conter 'username'.");
        Assert.Equal(username, usernameElement.GetString());

        Assert.True(root.TryGetProperty("email", out var emailElement), "Resposta deve conter 'email'.");
        Assert.Equal(uniqueEmail, emailElement.GetString());

        Assert.True(root.TryGetProperty("phone", out var phoneElement), "Resposta deve conter 'phone'.");
        Assert.Equal("11988887777", phoneElement.GetString());

        Assert.False(root.TryGetProperty("password", out _), "A senha nao deve ser retornada na resposta.");
    }

    [Fact]
    public async Task CreateUser_ShouldReturn400_WhenEmailIsEmpty()
    {
        var request = new CreateUserRequest($"john-{Guid.NewGuid():N}", "", "11999999999", "Str0ng@Pass");

        var response = await CreateUserAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ShouldReturn400_WhenEmailIsInvalid()
    {
        var request = new CreateUserRequest($"john-{Guid.NewGuid():N}", "not-an-email", "11999999999", "Str0ng@Pass");

        var response = await CreateUserAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ShouldReturn400_WhenUsernameIsEmpty()
    {
        var uniqueEmail = $"noname-{Guid.NewGuid():N}@example.com";
        var request = new CreateUserRequest("", uniqueEmail, "11999999999", "Str0ng@Pass");

        var response = await CreateUserAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ShouldReturn400_WhenPasswordIsEmpty()
    {
        var uniqueEmail = $"nopass-{Guid.NewGuid():N}@example.com";
        var request = new CreateUserRequest($"john-{Guid.NewGuid():N}", uniqueEmail, "11999999999", "");

        var response = await CreateUserAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ShouldReturn409_WhenEmailAlreadyExists()
    {
        var uniqueEmail = $"dup-{Guid.NewGuid():N}@example.com";
        var request = new CreateUserRequest($"john-{Guid.NewGuid():N}", uniqueEmail, "11999999999", "Str0ng@Pass");

        var firstResponse = await CreateUserAsync(request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await CreateUserAsync(request);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    #endregion

    #region GET /users/{id}

    [Fact]
    public async Task GetUserById_ShouldReturn200_WhenUserExists()
    {
        var id = await CreateUserAndGetIdAsync();

        var response = await _httpClient.GetAsync($"/users/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("id", out var idElement), "Resposta deve conter 'id'.");
        Assert.Equal(id, idElement.GetString());
        Assert.True(root.TryGetProperty("username", out _), "Resposta deve conter 'username'.");
        Assert.True(root.TryGetProperty("email", out _), "Resposta deve conter 'email'.");
        Assert.True(root.TryGetProperty("phone", out _), "Resposta deve conter 'phone'.");
        Assert.False(root.TryGetProperty("password", out _), "A senha nao deve ser retornada.");
    }

    [Fact]
    public async Task GetUserById_ShouldReturn404_WhenUserDoesNotExist()
    {
        var response = await _httpClient.GetAsync($"/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GET /users

    [Fact]
    public async Task GetAllUsers_ShouldReturn200_WithUserList()
    {
        await CreateUserAndGetIdAsync();

        var response = await _httpClient.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.True(root.GetArrayLength() > 0, "A lista de usuarios deve conter pelo menos um item.");

        var firstUser = root[0];
        Assert.True(firstUser.TryGetProperty("id", out _), "Cada usuario deve conter 'id'.");
        Assert.True(firstUser.TryGetProperty("username", out _), "Cada usuario deve conter 'username'.");
        Assert.True(firstUser.TryGetProperty("email", out _), "Cada usuario deve conter 'email'.");
        Assert.True(firstUser.TryGetProperty("phone", out _), "Cada usuario deve conter 'phone'.");
        Assert.False(firstUser.TryGetProperty("password", out _), "A senha nao deve ser retornada.");
    }

    #endregion

    #region PUT /users/{id}

    [Fact]
    public async Task UpdateUser_ShouldReturn200_WhenDataIsValid()
    {
        var id = await CreateUserAndGetIdAsync();
        var updatedUsername = $"updated-{Guid.NewGuid():N}";
        var updatedEmail = $"updated-{Guid.NewGuid():N}@example.com";
        var request = new UpdateUserRequest(updatedUsername, updatedEmail, "11977776666");

        var response = await _httpClient.PutAsJsonAsync($"/users/{id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal(id, root.GetProperty("id").GetString());
        Assert.Equal(updatedUsername, root.GetProperty("username").GetString());
        Assert.Equal(updatedEmail, root.GetProperty("email").GetString());
        Assert.Equal("11977776666", root.GetProperty("phone").GetString());
        Assert.False(root.TryGetProperty("password", out _), "A senha nao deve ser retornada.");
    }

    [Fact]
    public async Task UpdateUser_ShouldReturn404_WhenUserDoesNotExist()
    {
        var request = new UpdateUserRequest($"updated-{Guid.NewGuid():N}", $"update-{Guid.NewGuid():N}@example.com", "11911112222");

        var response = await _httpClient.PutAsJsonAsync($"/users/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturn400_WhenUsernameIsEmpty()
    {
        var id = await CreateUserAndGetIdAsync();
        var request = new UpdateUserRequest("", $"valid-{Guid.NewGuid():N}@example.com", "11911112222");

        var response = await _httpClient.PutAsJsonAsync($"/users/{id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturn400_WhenEmailIsInvalid()
    {
        var id = await CreateUserAndGetIdAsync();
        var request = new UpdateUserRequest($"valid-{Guid.NewGuid():N}", "not-an-email", "11911112222");

        var response = await _httpClient.PutAsJsonAsync($"/users/{id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region DELETE /users/{id}

    [Fact]
    public async Task DeleteUser_ShouldReturn204_WhenUserExists()
    {
        var id = await CreateUserAndGetIdAsync();

        var response = await _httpClient.DeleteAsync($"/users/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _httpClient.GetAsync($"/users/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturn404_WhenUserDoesNotExist()
    {
        var response = await _httpClient.DeleteAsync($"/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}

internal sealed record CreateUserRequest(string Username, string Email, string Phone, string Password);
internal sealed record UpdateUserRequest(string Username, string Email, string Phone);
