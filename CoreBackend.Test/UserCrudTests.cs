using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreBackend.Test.Support;

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
        var request = new CreateUserRequest("Test", "User", TestCpf.Generate(), uniqueEmail, "11999999999", "Valid@Pass1");
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
        var cpf = TestCpf.Generate();
        var request = new CreateUserRequest("João", "Silva", cpf, uniqueEmail, "11988887777", "Str0ng@Pass");

        var response = await CreateUserAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("id").GetString()));
        Assert.Equal("João", root.GetProperty("firstName").GetString());
        Assert.Equal("Silva", root.GetProperty("lastName").GetString());
        Assert.Equal(cpf, root.GetProperty("cpf").GetString());
        Assert.Equal(uniqueEmail, root.GetProperty("email").GetString());
        Assert.Equal("11988887777", root.GetProperty("phone").GetString());
        Assert.False(root.TryGetProperty("password", out _), "A senha nao deve ser retornada na resposta.");
    }

    [Fact]
    public async Task CreateUser_ShouldReturn400_WhenEmailIsEmpty()
    {
        var request = new CreateUserRequest("João", "Silva", TestCpf.Generate(), "", "11999999999", "Str0ng@Pass");
        var response = await CreateUserAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ShouldReturn400_WhenEmailIsInvalid()
    {
        var request = new CreateUserRequest("João", "Silva", TestCpf.Generate(), "not-an-email", "11999999999", "Str0ng@Pass");
        var response = await CreateUserAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ShouldReturn400_WhenFirstNameIsEmpty()
    {
        var uniqueEmail = $"noname-{Guid.NewGuid():N}@example.com";
        var request = new CreateUserRequest("", "Silva", TestCpf.Generate(), uniqueEmail, "11999999999", "Str0ng@Pass");
        var response = await CreateUserAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ShouldReturn400_WhenPasswordIsEmpty()
    {
        var uniqueEmail = $"nopass-{Guid.NewGuid():N}@example.com";
        var request = new CreateUserRequest("João", "Silva", TestCpf.Generate(), uniqueEmail, "11999999999", "");
        var response = await CreateUserAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ShouldReturn400_WhenCpfIsInvalid()
    {
        var uniqueEmail = $"badcpf-{Guid.NewGuid():N}@example.com";
        var request = new CreateUserRequest("João", "Silva", "99999999999", uniqueEmail, "11999999999", "Str0ng@Pass");
        var response = await CreateUserAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ShouldReturn409_WhenEmailAlreadyExists()
    {
        var uniqueEmail = $"dup-{Guid.NewGuid():N}@example.com";
        var request1 = new CreateUserRequest("João", "Silva", TestCpf.Generate(), uniqueEmail, "11999999999", "Str0ng@Pass");

        var firstResponse = await CreateUserAsync(request1);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var request2 = new CreateUserRequest("João", "Silva", TestCpf.Generate(), uniqueEmail, "11999999999", "Str0ng@Pass");
        var duplicateResponse = await CreateUserAsync(request2);
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

        Assert.Equal(id, root.GetProperty("id").GetString());
        Assert.True(root.TryGetProperty("firstName", out _), "Resposta deve conter 'firstName'.");
        Assert.True(root.TryGetProperty("cpf", out _), "Resposta deve conter 'cpf'.");
        Assert.True(root.TryGetProperty("email", out _), "Resposta deve conter 'email'.");
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
        Assert.True(firstUser.TryGetProperty("id", out _));
        Assert.True(firstUser.TryGetProperty("firstName", out _));
        Assert.True(firstUser.TryGetProperty("cpf", out _));
        Assert.True(firstUser.TryGetProperty("email", out _));
        Assert.False(firstUser.TryGetProperty("password", out _));
    }

    #endregion

    #region PUT /users/{id}

    [Fact]
    public async Task UpdateUser_ShouldReturn200_WhenDataIsValid()
    {
        var id = await CreateUserAndGetIdAsync();
        var updatedEmail = $"updated-{Guid.NewGuid():N}@example.com";
        var newCpf = TestCpf.Generate();
        var request = new UpdateUserRequest("Maria", "Costa", newCpf, updatedEmail, "11977776666");

        var response = await _httpClient.PutAsJsonAsync($"/users/{id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal(id, root.GetProperty("id").GetString());
        Assert.Equal("Maria", root.GetProperty("firstName").GetString());
        Assert.Equal(newCpf, root.GetProperty("cpf").GetString());
        Assert.Equal(updatedEmail, root.GetProperty("email").GetString());
        Assert.False(root.TryGetProperty("password", out _));
    }

    [Fact]
    public async Task UpdateUser_ShouldReturn404_WhenUserDoesNotExist()
    {
        var request = new UpdateUserRequest("Maria", "Costa", TestCpf.Generate(), $"update-{Guid.NewGuid():N}@example.com", "11911112222");
        var response = await _httpClient.PutAsJsonAsync($"/users/{Guid.NewGuid()}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturn400_WhenFirstNameIsEmpty()
    {
        var id = await CreateUserAndGetIdAsync();
        var request = new UpdateUserRequest("", "Costa", TestCpf.Generate(), $"valid-{Guid.NewGuid():N}@example.com", "11911112222");
        var response = await _httpClient.PutAsJsonAsync($"/users/{id}", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturn400_WhenEmailIsInvalid()
    {
        var id = await CreateUserAndGetIdAsync();
        var request = new UpdateUserRequest("Maria", "Costa", TestCpf.Generate(), "not-an-email", "11911112222");
        var response = await _httpClient.PutAsJsonAsync($"/users/{id}", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturn400_WhenCpfIsInvalid()
    {
        var id = await CreateUserAndGetIdAsync();
        var request = new UpdateUserRequest("Maria", "Costa", "11111111111", $"valid-{Guid.NewGuid():N}@example.com", "11911112222");
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

internal sealed record CreateUserRequest(string FirstName, string LastName, string Cpf, string Email, string Phone, string Password);
internal sealed record UpdateUserRequest(string FirstName, string LastName, string Cpf, string Email, string Phone);
