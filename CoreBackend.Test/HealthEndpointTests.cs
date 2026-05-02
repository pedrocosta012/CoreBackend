using System.Net;

namespace CoreBackend.Test;

public sealed class HealthEndpointTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly TestApiFactory _factory;

    public HealthEndpointTests()
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
    public async Task Health_ShouldReturn200_WithEmptyBody()
    {
        var response = await _httpClient.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(string.Empty, body);
    }
}
