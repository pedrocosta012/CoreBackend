using System.Net.Http;
using CoreBackend.Test.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CoreBackend.Test;

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseFilePath;

    public string SqliteConnectionString { get; }

    public TestApiFactory()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "corebackend-tests");
        Directory.CreateDirectory(databaseDirectory);

        _databaseFilePath = Path.Combine(databaseDirectory, $"db-{Guid.NewGuid():N}.sqlite");
        SqliteConnectionString = $"Data Source={_databaseFilePath}";

        const string jwtSecret = "TestOnlySecretKey_ForJwtTokenGeneration_AtLeast32Bytes!!";
        Environment.SetEnvironmentVariable("JWT_SECRET", jwtSecret);
        Environment.SetEnvironmentVariable("TEST_LOGIN_EMAIL", TestUsers.ValidLoginIdentifier);
        Environment.SetEnvironmentVariable("TEST_LOGIN_PASSWORD", TestUsers.ValidLoginPassword);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = SqliteConnectionString,
                ["JWT_SECRET"] = "TestOnlySecretKey_ForJwtTokenGeneration_AtLeast32Bytes!!",
                ["Resend:APITOKEN"] = "test",
                ["Jwt:Issuer"] = "CoreBackend",
                ["Jwt:Audience"] = "CoreBackend",
                ["Jwt:RefreshTokenExpiryDays"] = "7"
            };

            config.AddInMemoryCollection(overrides);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        try
        {
            if (File.Exists(_databaseFilePath))
            {
                File.Delete(_databaseFilePath);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }
}

