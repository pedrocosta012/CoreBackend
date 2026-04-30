using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreBackend.Test.Support;

namespace CoreBackend.Test;

public sealed class RegistrationFlowTests : IDisposable
{
    private readonly TestApiFactory _factory = new();
    private readonly HttpClient _httpClient;

    public RegistrationFlowTests()
    {
        _httpClient = _factory.CreateClient();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _factory.Dispose();
    }

    private static string Uid() => Guid.NewGuid().ToString("N");

    [Fact]
    public async Task RegisterOwner_ShouldReturn201_WithTokens()
    {
        var response = await _httpClient.PostAsJsonAsync("/auth/register/owner",
            new RegisterOwnerPayload("João", "Dono", TestCpf.Generate(), $"owner-{Uid()}@test.com", "11999990000",
                "Strong@1", "My Company", "headquarters", "12345678000100"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("authToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("refreshToken").GetString()));
    }

    [Fact]
    public async Task RegisterOwner_ShouldCreateCompanyAndMember()
    {
        var companyName = $"Company-{Uid()}";
        var ownerResponse = await _httpClient.PostAsJsonAsync("/auth/register/owner",
            new RegisterOwnerPayload("Ana", "Check", TestCpf.Generate(), $"ownercheck-{Uid()}@test.com", "11999990000",
                "Strong@1", companyName, "branch", "99999999000100"));

        Assert.Equal(HttpStatusCode.Created, ownerResponse.StatusCode);

        var companiesResponse = await _httpClient.GetAsync("/companies");
        Assert.Equal(HttpStatusCode.OK, companiesResponse.StatusCode);

        var companiesBody = await companiesResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(companiesBody);
        var found = doc.RootElement.EnumerateArray()
            .Any(c => c.GetProperty("name").GetString() == companyName);
        Assert.True(found, $"Company '{companyName}' should appear in /companies list");
    }

    [Fact]
    public async Task RegisterOwner_ShouldReturn400_WhenCompanyNameMissing()
    {
        var response = await _httpClient.PostAsJsonAsync("/auth/register/owner",
            new RegisterOwnerPayload("João", "Teste", TestCpf.Generate(), $"owner-{Uid()}@test.com", "11999990000",
                "Strong@1", "", "headquarters", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterOwner_ShouldReturn400_WhenCpfIsInvalid()
    {
        var response = await _httpClient.PostAsJsonAsync("/auth/register/owner",
            new RegisterOwnerPayload("João", "Bad", "00000000000", $"owner-{Uid()}@test.com", "11999990000",
                "Strong@1", "Company", "headquarters", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterOwner_ShouldReturn409_WhenDuplicateEmail()
    {
        var email = $"dup-{Uid()}@test.com";
        var first = await _httpClient.PostAsJsonAsync("/auth/register/owner",
            new RegisterOwnerPayload("First", "User", TestCpf.Generate(), email, "11999990000",
                "Strong@1", "Comp1", "headquarters", ""));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _httpClient.PostAsJsonAsync("/auth/register/owner",
            new RegisterOwnerPayload("Second", "User", TestCpf.Generate(), email, "11999990000",
                "Strong@1", "Comp2", "headquarters", ""));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task RegisterWorker_ShouldReturn201_WithTokens()
    {
        var ownerResponse = await _httpClient.PostAsJsonAsync("/auth/register/owner",
            new RegisterOwnerPayload("Owner", "Wk", TestCpf.Generate(), $"wkowner-{Uid()}@test.com", "11999990000",
                "Strong@1", $"WkCompany-{Uid()}", "headquarters", ""));
        Assert.Equal(HttpStatusCode.Created, ownerResponse.StatusCode);

        var companiesResponse = await _httpClient.GetAsync("/companies");
        var companiesBody = await companiesResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(companiesBody);
        var companyId = doc.RootElement.EnumerateArray().First().GetProperty("id").GetString()!;

        var workerResponse = await _httpClient.PostAsJsonAsync("/auth/register/worker",
            new RegisterWorkerPayload("Worker", "User", TestCpf.Generate(), $"worker-{Uid()}@test.com", "11999990000",
                "Strong@1", companyId));

        Assert.Equal(HttpStatusCode.Created, workerResponse.StatusCode);

        var workerBody = await workerResponse.Content.ReadAsStringAsync();
        using var workerDoc = JsonDocument.Parse(workerBody);
        Assert.False(string.IsNullOrWhiteSpace(workerDoc.RootElement.GetProperty("authToken").GetString()));
    }

    [Fact]
    public async Task RegisterWorker_ShouldReturn400_WhenCompanyNotFound()
    {
        var response = await _httpClient.PostAsJsonAsync("/auth/register/worker",
            new RegisterWorkerPayload("Worker", "Fail", TestCpf.Generate(), $"worker-{Uid()}@test.com", "11999990000",
                "Strong@1", Guid.NewGuid().ToString()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterWorker_ShouldReturn400_WhenCompanyIdEmpty()
    {
        var response = await _httpClient.PostAsJsonAsync("/auth/register/worker",
            new RegisterWorkerPayload("Worker", "Empty", TestCpf.Generate(), $"worker-{Uid()}@test.com", "11999990000",
                "Strong@1", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterWorker_ShouldReturn400_WhenCpfIsInvalid()
    {
        var ownerResponse = await _httpClient.PostAsJsonAsync("/auth/register/owner",
            new RegisterOwnerPayload("Owner", "Cpf", TestCpf.Generate(), $"owncpf-{Uid()}@test.com", "11999990000",
                "Strong@1", $"CpfComp-{Uid()}", "headquarters", ""));
        Assert.Equal(HttpStatusCode.Created, ownerResponse.StatusCode);

        var companiesResponse = await _httpClient.GetAsync("/companies");
        var companiesBody = await companiesResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(companiesBody);
        var companyId = doc.RootElement.EnumerateArray().First().GetProperty("id").GetString()!;

        var response = await _httpClient.PostAsJsonAsync("/auth/register/worker",
            new RegisterWorkerPayload("Worker", "BadCpf", "12345678900", $"wkbad-{Uid()}@test.com", "11999990000",
                "Strong@1", companyId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompanyList_ShouldReturn200WithArray()
    {
        var response = await _httpClient.GetAsync("/companies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }
}

internal sealed record RegisterOwnerPayload(string FirstName, string LastName, string Cpf, string Email, string Phone, string Password,
    string CompanyName, string OfficeType, string? TaxId);
internal sealed record RegisterWorkerPayload(string FirstName, string LastName, string Cpf, string Email, string Phone, string Password,
    string CompanyId);
