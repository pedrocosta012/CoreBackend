using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreBackend.Test.Support;

namespace CoreBackend.Test;

public sealed class CustomerCrudTests : IDisposable
{
    private readonly TestApiFactory _factory = new();
    private readonly HttpClient _httpClient;

    public CustomerCrudTests()
    {
        _httpClient = _factory.CreateClient();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _factory.Dispose();
    }

    private async Task<(HttpResponseMessage Response, JsonElement Body)> CreateCustomer(
        string name, string[]? tags = null)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/customers",
            new CreateCustomerRequest(name, "12345678900", $"{Guid.NewGuid():N}@test.com",
                "11999990000", "retail", "active", 0, 0, "", 0,
                "Rua Teste 123", "São Paulo", "SP", "", tags ?? []));

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
    public async Task CreateCustomer_ShouldReturn201WithBody()
    {
        var customerName = $"customer-{Guid.NewGuid():N}";

        var (response, body) = await CreateCustomer(customerName, ["vip", "promo"]);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(customerName, body.GetProperty("name").GetString());
        Assert.Equal("retail", body.GetProperty("segment").GetString());
        Assert.Equal("active", body.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("id").GetString()));

        var tags = body.GetProperty("tags");
        Assert.Equal(JsonValueKind.Array, tags.ValueKind);
        Assert.Equal(2, tags.GetArrayLength());
        Assert.Equal("vip", tags[0].GetString());
        Assert.Equal("promo", tags[1].GetString());
    }

    [Fact]
    public async Task CreateCustomer_ShouldReturn400_WhenNameIsEmpty()
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/customers",
            new CreateCustomerRequest("", null, null, null, null, null, null, null, null, null,
                null, null, null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListCustomers_ShouldReturn200WithArray()
    {
        await CreateCustomer($"list-{Guid.NewGuid():N}");

        var response = await _httpClient.GetAsync("/customers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.True(document.RootElement.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetCustomerById_ShouldReturn200()
    {
        var (createResponse, createdBody) = await CreateCustomer($"get-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var id = createdBody.GetProperty("id").GetString();

        var response = await _httpClient.GetAsync($"/customers/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        Assert.Equal(id, document.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public async Task UpdateCustomer_ShouldReturn200WithUpdatedData()
    {
        var (createResponse, createdBody) = await CreateCustomer($"before-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var id = createdBody.GetProperty("id").GetString();
        var updatedName = $"after-{Guid.NewGuid():N}";

        var updateResponse = await _httpClient.PutAsJsonAsync(
            $"/customers/{id}",
            new UpdateCustomerRequest(updatedName, null, null, null, "vip", null, 1500.50, 10,
                null, 500, null, null, null, "VIP customer", ["vip"]));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var content = await updateResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        Assert.Equal(updatedName, root.GetProperty("name").GetString());
        Assert.Equal("vip", root.GetProperty("segment").GetString());
        Assert.Equal(1500.50, root.GetProperty("totalSpent").GetDouble());
        Assert.Equal(10, root.GetProperty("ordersCount").GetInt32());
        Assert.Equal(500, root.GetProperty("loyaltyPoints").GetInt32());
        Assert.Equal("VIP customer", root.GetProperty("notes").GetString());

        var tags = root.GetProperty("tags");
        Assert.Equal(1, tags.GetArrayLength());
        Assert.Equal("vip", tags[0].GetString());
    }

    [Fact]
    public async Task DeleteCustomer_ShouldReturn204()
    {
        var (createResponse, createdBody) = await CreateCustomer($"delete-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var id = createdBody.GetProperty("id").GetString();

        var deleteResponse = await _httpClient.DeleteAsync($"/customers/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _httpClient.GetAsync($"/customers/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetNonExistentCustomer_ShouldReturn404()
    {
        var response = await _httpClient.GetAsync($"/customers/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

internal sealed record CreateCustomerRequest(
    string Name, string? Document, string? Email, string? Phone,
    string? Segment, string? Status, double? TotalSpent, int? OrdersCount,
    string? LastPurchaseDate, int? LoyaltyPoints, string? Address,
    string? City, string? State, string? Notes, string[]? Tags);

internal sealed record UpdateCustomerRequest(
    string? Name, string? Document, string? Email, string? Phone,
    string? Segment, string? Status, double? TotalSpent, int? OrdersCount,
    string? LastPurchaseDate, int? LoyaltyPoints, string? Address,
    string? City, string? State, string? Notes, string[]? Tags);
