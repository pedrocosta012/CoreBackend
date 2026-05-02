using System.Data;
using System.Net.Mail;
using CoreBackend.Exceptions;
using CoreBackend.Infrastructure.Security;
using CoreBackend.Infrastructure.Validation;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace CoreBackend.Users;

[ApiController]
[Route("users")]
public sealed class UserController : ControllerBase
{
    [HttpPost]
    public async Task<IResult> Create(
        [FromBody] CreateUserRequest request,
        [FromServices] IDbConnection db,
        [FromServices] IPasswordHasher hasher)
    {
        FieldsContentException error = new();

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            error.AddError("FirstName", "First name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            error.AddError("Email", "Email is required.");
        }

        if (!IsValidEmail(request.Email))
        {
            error.AddError("Email", "Valid email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            error.AddError("Password", "Password is required.");
        }

        if (!string.IsNullOrWhiteSpace(request.Cpf) && !CpfValidator.IsValid(request.Cpf))
        {
            error.AddError("Cpf", "Invalid CPF.");
        }

        if (error.HasErrors())
        {
            return Results.BadRequest(error);
        }

        try
        {
            var id = Guid.NewGuid().ToString();
            var username = $"u-{Guid.NewGuid():N}";
            var cpfDigits = ExtractDigits(request.Cpf);
            var hashedPassword = hasher.Hash(request.Password);
            await db.ExecuteAsync(
                """
                INSERT INTO user (id, username, firstName, lastName, cpf, email, phone, password)
                VALUES (@Id, @Username, @FirstName, @LastName, @Cpf, @Email, @Phone, @Password)
                """,
                new
                {
                    Id = id,
                    Username = username,
                    request.FirstName,
                    LastName = request.LastName ?? "",
                    Cpf = cpfDigits,
                    request.Email,
                    request.Phone,
                    Password = hashedPassword
                });

            return Results.Created($"/users/{id}", new { id, request.FirstName, request.LastName, Cpf = cpfDigits, request.Email, request.Phone });
        }
        catch (SqliteException err) when (
            err.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Conflict(new { error = "Email or CPF already exists." });
        }
    }

    [HttpGet]
    public async Task<IResult> List([FromServices] IDbConnection db)
    {
        var users = await db.QueryAsync<UserRow>(
            """
            SELECT id, COALESCE(firstName, '') AS firstName, COALESCE(lastName, '') AS lastName,
                   COALESCE(cpf, '') AS cpf, COALESCE(email, '') AS email, COALESCE(phone, '') AS phone
            FROM user
            WHERE deletedAt IS NULL
            ORDER BY createdAt DESC
            """);

        return Results.Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IResult> GetById(string id, [FromServices] IDbConnection db)
    {
        var user = await db.QuerySingleOrDefaultAsync<UserRow>(
            """
            SELECT id, COALESCE(firstName, '') AS firstName, COALESCE(lastName, '') AS lastName,
                   COALESCE(cpf, '') AS cpf, COALESCE(email, '') AS email, COALESCE(phone, '') AS phone
            FROM user
            WHERE id = @Id AND deletedAt IS NULL
            """,
            new { Id = id });

        return user is not null ? Results.Ok(user) : Results.NotFound();
    }

    [HttpPut("{id}")]
    public async Task<IResult> Update(string id, [FromBody] UpdateUserRequest request, [FromServices] IDbConnection db)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return Results.BadRequest(new { error = "First name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
        {
            return Results.BadRequest(new { error = "Valid email is required." });
        }

        if (!string.IsNullOrWhiteSpace(request.Cpf) && !CpfValidator.IsValid(request.Cpf))
        {
            return Results.BadRequest(new { error = "Invalid CPF." });
        }

        var cpfDigits = ExtractDigits(request.Cpf);

        var rows = await db.ExecuteAsync(
            """
            UPDATE user
            SET firstName = @FirstName,
                lastName = @LastName,
                cpf = @Cpf,
                email = @Email,
                phone = @Phone,
                updatedAt = CURRENT_TIMESTAMP
            WHERE id = @Id
            AND deletedAt IS NULL
            """,
            new { Id = id, request.FirstName, LastName = request.LastName ?? "", Cpf = cpfDigits, request.Email, request.Phone });

        if (rows == 0)
        {
            return Results.NotFound();
        }

        return Results.Ok(new { id, request.FirstName, request.LastName, Cpf = cpfDigits, request.Email, request.Phone });
    }

    [HttpDelete("{id}")]
    public async Task<IResult> Delete(string id, [FromServices] IDbConnection db)
    {
        var rows = await db.ExecuteAsync(
            """
            UPDATE user
            SET deletedAt = CURRENT_TIMESTAMP
            WHERE id = @Id AND deletedAt IS NULL
            """,
            new { Id = id });

        return rows > 0 ? Results.NoContent() : Results.NotFound();
    }

    private static string ExtractDigits(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : new string(value.Where(char.IsDigit).ToArray());

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
