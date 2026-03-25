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
            RunSqliteSchema(sqliteConnection);
            await SeedTestUserAsync(sqliteConnection);
            return;
        }

        RunMySqlMigrations(connectionString);
        await SeedTestUserAsync(db);
    }

    private static void RunMySqlMigrations(string connectionString)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var migrationResources = assembly
            .GetManifestResourceNames()
            .Where(name =>
                name.Contains(".Infrastructure.Database.Migrations.", StringComparison.OrdinalIgnoreCase) &&
                name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        using var connection = new MySqlConnector.MySqlConnection(connectionString);
        connection.Open();

        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS schema_versions (
                script_name VARCHAR(255) NOT NULL PRIMARY KEY,
                applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            )
            """);

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

            using var transaction = connection.BeginTransaction();
            if (!string.IsNullOrWhiteSpace(script))
            {
                connection.Execute(script, transaction: transaction);
            }

            connection.Execute(
                "INSERT INTO schema_versions (script_name, applied_at) VALUES (@Name, @AppliedAt)",
                new { Name = resourceName, AppliedAt = DateTime.UtcNow },
                transaction: transaction);

            transaction.Commit();
        }
    }

    private static void RunSqliteSchema(IDbConnection connection)
    {
        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS schema_versions (
                script_name TEXT NOT NULL PRIMARY KEY,
                applied_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """);

        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS user (
                id TEXT NOT NULL PRIMARY KEY,
                username TEXT NOT NULL UNIQUE,
                email TEXT UNIQUE,
                phone TEXT,
                password TEXT NOT NULL,
                createdAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                deletedAt TEXT NULL
            );
            """);

        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS refresh_token (
                id TEXT NOT NULL PRIMARY KEY,
                userId TEXT NOT NULL,
                token TEXT NOT NULL UNIQUE,
                expiresAt TEXT NOT NULL,
                revokedAt TEXT NULL,
                createdAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (userId) REFERENCES user(id)
            );
            """);

        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS password_reset_token (
                id TEXT NOT NULL PRIMARY KEY,
                userId TEXT NOT NULL,
                token TEXT NOT NULL UNIQUE,
                expiresAt TEXT NOT NULL,
                usedAt TEXT NULL,
                createdAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (userId) REFERENCES user(id)
            );
            """);

        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS category (
                id TEXT NOT NULL PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                type TEXT NOT NULL,
                createdAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                deletedAt TEXT NULL
            );
            """);
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
            SELECT CAST(id AS CHAR) AS id
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

