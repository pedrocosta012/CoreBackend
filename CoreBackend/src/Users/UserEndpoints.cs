using System.Data;
using System.Net.Mail;
using CoreBackend.Exceptions;
using CoreBackend.Infrastructure.Security;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CoreBackend.Users;

internal static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users", async (CreateUserRequest request, IDbConnection db, IPasswordHasher hasher) =>
        {
            FieldsContentException error = new();
            string[] requiredFields = ["Username", "Email", "Password"];
            System.Console.WriteLine("Chegou na rota");

            if (string.IsNullOrWhiteSpace(request.Username))
                error.AddError("Username", "Username is required.");
            if (string.IsNullOrWhiteSpace(request.Email))
                error.AddError("Email", "Email is required.");
            if (!IsValidEmail(request.Email))
                error.AddError("Email", "Valid email is required.");
            if (string.IsNullOrWhiteSpace(request.Password))
                error.AddError("Password", "Password is required.");

            if (error.HasErrors())
            {
                return Results.BadRequest(error);
            }

            // Deve ser tratado em try/catch para menor consumo do banco de dados
            /* var existing = await db.QuerySingleOrDefaultAsync<string>(
                db is SqliteConnection
                    ? """
                      SELECT id
                      FROM user
                      WHERE deletedAt IS NULL
                      AND (email = @Email OR username = @Username)
                      LIMIT 1
                      """
                    : """
                      SELECT CAST(id AS CHAR) AS id
                      FROM user
                      WHERE deletedAt IS NULL
                      AND (email = @Email OR username = @Username)
                      LIMIT 1
                      """,
                new { request.Email, request.Username });

            if (!string.IsNullOrWhiteSpace(existing))
            {
                return Results.Conflict(new { error = "Email or username already exists." });
            } */

            try
            {
                var id = Guid.NewGuid().ToString();
                var hashedPassword = hasher.Hash(request.Password);
                Console.WriteLine("Pré INSERT");
                await db.ExecuteAsync(
                    """
                INSERT INTO user (id, username, email, phone, password)
                VALUES (@Id, @Username, @Email, @Phone, @Password)
                """,
                    new
                    {
                        Id = id,
                        request.Username,
                        request.Email,
                        request.Phone,
                        Password = hashedPassword
                    });

                return Results.Created($"/users/{id}", new { id, request.Username, request.Email, request.Phone });
            }
            catch (MySqlConnector.MySqlException err) when (
                err.Message.Contains("Duplicate entry", System.StringComparison.OrdinalIgnoreCase))
            {
                return Results.Conflict(new { error = "Email or username already exists." });
            }
            catch (SqliteException err) when (
                err.Message.Contains("UNIQUE constraint failed", System.StringComparison.OrdinalIgnoreCase))
            {
                return Results.Conflict(new { error = "Email or username already exists." });
            }
        });

        app.MapGet("/users", async (IDbConnection db) =>
        {
            var users = await db.QueryAsync<UserRow>(
                db is SqliteConnection
                    ? """
                      SELECT id AS id, username, email, COALESCE(phone, '') AS phone
                      FROM user
                      WHERE deletedAt IS NULL
                      ORDER BY createdAt DESC
                      """
                    : """
                      SELECT CAST(id AS CHAR) AS id, username, email, COALESCE(phone, '') AS phone
                      FROM user
                      WHERE deletedAt IS NULL
                      ORDER BY createdAt DESC
                      """);
            return Results.Ok(users);
        });

        app.MapGet("/users/{id}", async (string id, IDbConnection db) =>
        {
            var user = await db.QuerySingleOrDefaultAsync<UserRow>(
                db is SqliteConnection
                    ? """
                      SELECT id AS id, username, email, COALESCE(phone, '') AS phone
                      FROM user
                      WHERE id = @Id AND deletedAt IS NULL
                      """
                    : """
                      SELECT CAST(id AS CHAR) AS id, username, email, COALESCE(phone, '') AS phone
                      FROM user
                      WHERE id = @Id AND deletedAt IS NULL
                      """,
                new { Id = id });

            return user is not null ? Results.Ok(user) : Results.NotFound();
        });

        app.MapPut("/users/{id}", async (string id, UpdateUserRequest request, IDbConnection db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return Results.BadRequest(new { error = "Username is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
            {
                return Results.BadRequest(new { error = "Valid email is required." });
            }

            var rows = await db.ExecuteAsync(
                """
                UPDATE user
                SET username = @Username,
                    email = @Email,
                    phone = @Phone
                WHERE id = @Id
                AND deletedAt IS NULL
                """,
                new { Id = id, request.Username, request.Email, request.Phone });

            if (rows == 0)
            {
                return Results.NotFound();
            }

            return Results.Ok(new { id, request.Username, request.Email, request.Phone });
        });

        app.MapDelete("/users/{id}", async (string id, IDbConnection db) =>
        {
            var rows = await db.ExecuteAsync(
                db is SqliteConnection
                    ? """
                      UPDATE user
                      SET deletedAt = CURRENT_TIMESTAMP
                      WHERE id = @Id AND deletedAt IS NULL
                      """
                    : """
                      UPDATE user
                      SET deletedAt = UTC_TIMESTAMP()
                      WHERE id = @Id AND deletedAt IS NULL
                      """,
                new { Id = id });

            return rows > 0 ? Results.NoContent() : Results.NotFound();
        });
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return address.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
