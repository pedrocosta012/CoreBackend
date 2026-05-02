using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CoreBackend.Test;

public sealed class CategoryCrudTests : IDisposable
{
    private readonly TestApiFactory _factory = new();
    private readonly HttpClient _httpClient;

    public CategoryCrudTests()
    {
        _httpClient = _factory.CreateClient();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _factory.Dispose();
    }

    private async Task<(HttpResponseMessage Response, JsonElement Body)> CreateCategory(
        string name, string type = "product")
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/categories",
            new CategoryRequest(name, type));

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (!contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            return (response, default);

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
            return (response, default);

        using var document = JsonDocument.Parse(content);
        return (response, document.RootElement.Clone());
    }

    [Fact]
    public async Task CategoryTableMustExistOnDatabase()
    {
        var (response, _) = await CreateCategory($"probe-{Guid.NewGuid():N}");

        Assert.True(
            (int)response.StatusCode < 500,
            $"A rota /categories retornou {(int)response.StatusCode}, indicando que a tabela 'category' pode nao existir no banco.");
    }

    [Fact]
    public async Task CreateCategoryMustReturn201WithBody()
    {
        var categoryName = $"test-{Guid.NewGuid():N}";

        var (response, body) = await CreateCategory(categoryName);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Assert.True(body.TryGetProperty("id", out var idElement), "O campo 'id' deve existir.");
        Assert.True(body.TryGetProperty("name", out var nameElement), "O campo 'name' deve existir.");
        Assert.True(body.TryGetProperty("type", out var typeElement), "O campo 'type' deve existir.");

        Assert.False(string.IsNullOrWhiteSpace(idElement.ToString()), "O id deve ser preenchido.");
        Assert.Equal(categoryName, nameElement.GetString());
        Assert.Equal("product", typeElement.GetString());
    }

    [Fact]
    public async Task ListCategoriesMustReturn200WithArray()
    {
        await CreateCategory($"list-{Guid.NewGuid():N}");

        var response = await _httpClient.GetAsync("/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);

        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.True(document.RootElement.GetArrayLength() > 0, "A lista de categorias deve conter ao menos um item.");
    }

    [Fact]
    public async Task GetCategoryByIdMustReturn200()
    {
        var categoryName = $"get-{Guid.NewGuid():N}";

        var (createResponse, createdBody) = await CreateCategory(categoryName);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var id = createdBody.GetProperty("id").ToString();

        var response = await _httpClient.GetAsync($"/categories/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        Assert.Equal(id, root.GetProperty("id").ToString());
        Assert.Equal(categoryName, root.GetProperty("name").GetString());
        Assert.Equal("product", root.GetProperty("type").GetString());
    }

    [Fact]
    public async Task UpdateCategoryMustReturn200WithUpdatedData()
    {
        var (createResponse, createdBody) = await CreateCategory($"before-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var id = createdBody.GetProperty("id").ToString();
        var updatedName = $"after-{Guid.NewGuid():N}";

        var updateResponse = await _httpClient.PutAsJsonAsync(
            $"/categories/{id}",
            new CategoryRequest(updatedName, "product"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var content = await updateResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        Assert.Equal(id, root.GetProperty("id").ToString());
        Assert.Equal(updatedName, root.GetProperty("name").GetString());
        Assert.Equal("product", root.GetProperty("type").GetString());
    }

    [Fact]
    public async Task DeleteCategoryMustReturn204()
    {
        var (createResponse, createdBody) = await CreateCategory($"delete-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var id = createdBody.GetProperty("id").ToString();

        var deleteResponse = await _httpClient.DeleteAsync($"/categories/{id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task GetNonExistentCategoryMustReturn404()
    {
        var response = await _httpClient.GetAsync($"/categories/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

internal sealed record CategoryRequest(string Name, string Type);
