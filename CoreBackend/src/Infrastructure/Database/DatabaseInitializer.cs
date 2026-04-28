using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CoreBackend.Infrastructure.Database;

internal static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection deve ser configurado.");
        }

        using var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
        if (db is SqliteConnection sqliteConnection)
        {
            await sqliteConnection.OpenAsync();
        }

        RunMigrations(db);
        await SeedTestUserAsync(db);
    }

    private static void RunMigrations(IDbConnection connection)
    {
        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS schema_versions (
                script_name TEXT NOT NULL PRIMARY KEY,
                applied_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """);

        var assembly = Assembly.GetExecutingAssembly();
        var migrationResources = assembly
            .GetManifestResourceNames()
            .Where(name =>
                name.Contains(".Infrastructure.Database.Migrations.", StringComparison.OrdinalIgnoreCase) &&
                name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var applied = connection
            .Query<string>("SELECT script_name FROM schema_versions")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var resourceName in migrationResources)
        {
            if (applied.Contains(resourceName))
            {
                continue;
            }

            var script = ReadEmbeddedResource(assembly, resourceName);

            if (!string.IsNullOrWhiteSpace(script))
            {
                connection.Execute(script);
            }

            connection.Execute(
                "INSERT INTO schema_versions (script_name) VALUES (@Name)",
                new { Name = resourceName });
        }
    }

    private static string ReadEmbeddedResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Migration resource não encontrado: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static async Task SeedTestUserAsync(IDbConnection db)
    {
        var seedEmail = Environment.GetEnvironmentVariable("TEST_LOGIN_EMAIL");
        var seedPassword = Environment.GetEnvironmentVariable("TEST_LOGIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(seedEmail) || string.IsNullOrWhiteSpace(seedPassword))
        {
            return;
        }

        var exists = await db.QuerySingleOrDefaultAsync<string>(
            """
            SELECT id
            FROM user
            WHERE email = @Email AND deletedAt IS NULL
            LIMIT 1
            """,
            new { Email = seedEmail });

        if (!string.IsNullOrWhiteSpace(exists))
        {
            return;
        }

        var id = Guid.NewGuid().ToString();
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(seedPassword);

        await db.ExecuteAsync(
            """
            INSERT INTO user (id, username, email, phone, password)
            VALUES (@Id, @Username, @Email, @Phone, @Password)
            """,
            new
            {
                Id = id,
                Username = "admin",
                Email = seedEmail,
                Phone = string.Empty,
                Password = hashedPassword
            });
    }
}
