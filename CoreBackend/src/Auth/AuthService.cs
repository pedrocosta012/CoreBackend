using System.Data;
using System.Net.Mail;
using CoreBackend.Infrastructure.Email;
using CoreBackend.Infrastructure.Security;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace CoreBackend.Auth;

internal sealed class AuthService
{
    private readonly IDbConnection _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly JwtSettings _jwtSettings;
    private readonly ResendSettings _resendSettings;

    public AuthService(
        IDbConnection db,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEmailService emailService,
        IOptions<JwtSettings> jwtSettings,
        IOptions<ResendSettings> resendSettings)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailService = emailService;
        _jwtSettings = jwtSettings.Value;
        _resendSettings = resendSettings.Value;
    }

    public async Task<IResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return Results.BadRequest(new { error = "Username is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
        {
            return Results.BadRequest(new { error = "Valid email is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Password is required." });
        }

        // var duplicate = await _db.QuerySingleOrDefaultAsync<string>(
        //     IsSqlite()
        //         ? """
        //           SELECT id
        //           FROM user
        //           WHERE deletedAt IS NULL
        //           AND (username = @Username OR email = @Email)
        //           LIMIT 1
        //           """
        //         : """
        //           SELECT CAST(id AS CHAR) AS id
        //           FROM user
        //           WHERE deletedAt IS NULL
        //           AND (username = @Username OR email = @Email)
        //           LIMIT 1
        //           """,
        //     new { request.Username, request.Email });

        // if (!string.IsNullOrWhiteSpace(duplicate))
        // {
        //     return Results.Conflict(new { error = "Email or username already exists." });
        // }
        try
        {
            var userId = Guid.NewGuid().ToString();
            var hashedPassword = _passwordHasher.Hash(request.Password);
            await _db.ExecuteAsync(
                """
                INSERT INTO user (id, username, email, phone, password)
                VALUES (@Id, @Username, @Email, @Phone, @Password)
                """,
                new
                {
                    Id = userId,
                    request.Username,
                    request.Email,
                    request.Phone,
                    Password = hashedPassword
                });

            var response = await CreateTokensAsync(new AuthenticatedUser(userId, request.Username, request.Email));
            return Results.Created($"/users/{userId}", response);
        }
        catch (MySqlConnector.MySqlException err)
        {
            if (err.Message.Contains("Duplicate entry"))
            {
                var duplicateKeys = new List<string>();
                var matches = System.Text.RegularExpressions.Regex.Matches(
                    err.Message,
                    @"key '([^']+)'");

                for (int i = 0; i < matches.Count; i++)
                {
                    var fullKey = matches[i].Groups[1].Value;
                    if (string.IsNullOrWhiteSpace(fullKey))
                    {
                        continue;
                    }

                    var parts = fullKey.Split('.');
                    duplicateKeys.Add(
                        parts.Length > 1
                            ? string.Join('.', parts, 1, parts.Length - 1)
                            : fullKey);
                }

                System.Console.WriteLine(
                    duplicateKeys.Count > 0
                        ? $"Chaves duplicadas: {string.Join(", ", duplicateKeys)}"
                        : err.Message);
            }
            else
            {
                System.Console.WriteLine(err.Message);
            }

            throw;
        }
    }

    public async Task<IResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Identifier and password are required." });
        }

        var user = await _db.QuerySingleOrDefaultAsync<LoginUserRow>(
            IsSqlite()
                ? """
                  SELECT id AS id, username, COALESCE(email, '') AS email, password
                  FROM user
                  WHERE deletedAt IS NULL
                  AND (email = @Identifier OR username = @Identifier)
                  LIMIT 1
                  """
                : """
                  SELECT CAST(id AS CHAR) AS id, username, COALESCE(email, '') AS email, password
                  FROM user
                  WHERE deletedAt IS NULL
                  AND (email = @Identifier OR username = @Identifier)
                  LIMIT 1
                  """,
            new { request.Identifier });

        if (user is null || !_passwordHasher.Verify(request.Password, user.Password))
        {
            return Results.Unauthorized();
        }

        var response = await CreateTokensAsync(new AuthenticatedUser(user.Id, user.Username, user.Email));
        return Results.Ok(response);
    }

    public async Task<IResult> RefreshAsync(AuthTokenRefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Results.BadRequest(new { error = "Refresh token is required." });
        }

        var tokenRow = await _db.QuerySingleOrDefaultAsync<RefreshTokenRow>(
            IsSqlite()
                ? """
                  SELECT rt.id AS id,
                         rt.userId AS userId,
                         rt.expiresAt,
                         rt.revokedAt,
                         u.username,
                         COALESCE(u.email, '') AS email
                  FROM refresh_token rt
                  INNER JOIN user u ON u.id = rt.userId
                  WHERE rt.token = @Token AND u.deletedAt IS NULL
                  LIMIT 1
                  """
                : """
                  SELECT CAST(rt.id AS CHAR) AS id,
                         CAST(rt.userId AS CHAR) AS userId,
                         rt.expiresAt,
                         rt.revokedAt,
                         u.username,
                         COALESCE(u.email, '') AS email
                  FROM refresh_token rt
                  INNER JOIN user u ON u.id = rt.userId
                  WHERE rt.token = @Token AND u.deletedAt IS NULL
                  LIMIT 1
                  """,
            new { Token = request.RefreshToken });

        if (tokenRow is null)
        {
            return Results.Unauthorized();
        }

        var revokedAt = ParseNullableUtc(tokenRow.RevokedAt);
        var expiresAt = ParseUtc(tokenRow.ExpiresAt);

        if (revokedAt.HasValue || expiresAt <= DateTime.UtcNow)
        {
            return Results.Unauthorized();
        }

        await _db.ExecuteAsync(
            IsSqlite()
                ? "UPDATE refresh_token SET revokedAt = CURRENT_TIMESTAMP WHERE id = @Id"
                : "UPDATE refresh_token SET revokedAt = UTC_TIMESTAMP() WHERE id = @Id",
            new { tokenRow.Id });

        var response = await CreateTokensAsync(new AuthenticatedUser(tokenRow.UserId, tokenRow.Username, tokenRow.Email));
        return Results.Ok(response);
    }

    public async Task<IResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
        {
            return Results.BadRequest(new { error = "Valid email is required." });
        }

        var user = await _db.QuerySingleOrDefaultAsync<LoginUserRow>(
            IsSqlite()
                ? """
                  SELECT id AS id, username, COALESCE(email, '') AS email, password
                  FROM user
                  WHERE email = @Email AND deletedAt IS NULL
                  LIMIT 1
                  """
                : """
                  SELECT CAST(id AS CHAR) AS id, username, COALESCE(email, '') AS email, password
                  FROM user
                  WHERE email = @Email AND deletedAt IS NULL
                  LIMIT 1
                  """,
            new { request.Email });

        if (user is not null)
        {
            var token = _tokenService.GenerateRefreshToken();
            await _db.ExecuteAsync(
                """
                INSERT INTO password_reset_token (id, userId, token, expiresAt)
                VALUES (@Id, @UserId, @Token, @ExpiresAt)
                """,
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = user.Id,
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(1)
                });

            var resetLink = $"{_resendSettings.ResetPasswordBaseUrl}?token={Uri.EscapeDataString(token)}";
            await _emailService.SendPasswordResetAsync(request.Email, resetLink, cancellationToken);
        }

        return Results.Ok(new { message = "If the email exists, reset instructions were sent." });
    }

    public async Task<IResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Results.BadRequest(new { error = "Token and new password are required." });
        }

        var resetRow = await _db.QuerySingleOrDefaultAsync<PasswordResetTokenRow>(
            IsSqlite()
                ? """
                  SELECT id AS id,
                         userId AS userId,
                         expiresAt,
                         usedAt
                  FROM password_reset_token
                  WHERE token = @Token
                  LIMIT 1
                  """
                : """
                  SELECT CAST(id AS CHAR) AS id,
                         CAST(userId AS CHAR) AS userId,
                         expiresAt,
                         usedAt
                  FROM password_reset_token
                  WHERE token = @Token
                  LIMIT 1
                  """,
            new { Token = request.Token });

        if (resetRow is null)
        {
            return Results.Unauthorized();
        }

        var usedAt = ParseNullableUtc(resetRow.UsedAt);
        var resetExpiresAt = ParseUtc(resetRow.ExpiresAt);

        if (usedAt.HasValue || resetExpiresAt <= DateTime.UtcNow)
        {
            return Results.Unauthorized();
        }

        var newHashedPassword = _passwordHasher.Hash(request.NewPassword);
        await _db.ExecuteAsync(
            IsSqlite()
                ? """
                  UPDATE user
                  SET password = @Password,
                      updatedAt = CURRENT_TIMESTAMP
                  WHERE id = @UserId AND deletedAt IS NULL
                  """
                : """
                  UPDATE user
                  SET password = @Password,
                      updatedAt = UTC_TIMESTAMP()
                  WHERE id = @UserId AND deletedAt IS NULL
                  """,
            new { Password = newHashedPassword, resetRow.UserId });

        await _db.ExecuteAsync(
            IsSqlite()
                ? "UPDATE password_reset_token SET usedAt = CURRENT_TIMESTAMP WHERE id = @Id"
                : "UPDATE password_reset_token SET usedAt = UTC_TIMESTAMP() WHERE id = @Id",
            new { resetRow.Id });

        await _db.ExecuteAsync(
            IsSqlite()
                ? """
                  UPDATE refresh_token
                  SET revokedAt = CURRENT_TIMESTAMP
                  WHERE userId = @UserId
                  AND revokedAt IS NULL
                  """
                : """
                  UPDATE refresh_token
                  SET revokedAt = UTC_TIMESTAMP()
                  WHERE userId = @UserId
                  AND revokedAt IS NULL
                  """,
            new { resetRow.UserId });

        return Results.Ok();
    }

    public async Task<IResult> LogoutAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        await _db.ExecuteAsync(
            IsSqlite()
                ? """
                  UPDATE refresh_token
                  SET revokedAt = CURRENT_TIMESTAMP
                  WHERE userId = @UserId AND revokedAt IS NULL
                  """
                : """
                  UPDATE refresh_token
                  SET revokedAt = UTC_TIMESTAMP()
                  WHERE userId = @UserId AND revokedAt IS NULL
                  """,
            new { UserId = userId });

        return Results.NoContent();
    }

    public async Task<IResult> MeAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var me = await _db.QuerySingleOrDefaultAsync<MeResponse>(
            IsSqlite()
                ? """
                  SELECT id AS id, username, COALESCE(email, '') AS email, COALESCE(phone, '') AS phone
                  FROM user
                  WHERE id = @Id AND deletedAt IS NULL
                  LIMIT 1
                  """
                : """
                  SELECT CAST(id AS CHAR) AS id, username, COALESCE(email, '') AS email, COALESCE(phone, '') AS phone
                  FROM user
                  WHERE id = @Id AND deletedAt IS NULL
                  LIMIT 1
                  """,
            new { Id = userId });

        return me is not null ? Results.Ok(me) : Results.NotFound();
    }

    private async Task<TokensResponse> CreateTokensAsync(AuthenticatedUser user)
    {
        var authToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        await _db.ExecuteAsync(
            """
            INSERT INTO refresh_token (id, userId, token, expiresAt)
            VALUES (@Id, @UserId, @Token, @ExpiresAt)
            """,
            new
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
            });

        return new TokensResponse(authToken, refreshToken);
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

    private sealed record LoginUserRow(string Id, string Username, string Email, string Password);
    private sealed record RefreshTokenRow(string Id, string UserId, string ExpiresAt, string? RevokedAt, string Username, string Email);
    private sealed record PasswordResetTokenRow(string Id, string UserId, string ExpiresAt, string? UsedAt);

    private bool IsSqlite() => _db is SqliteConnection;

    private static DateTime ParseUtc(string value)
    {
        if (DateTime.TryParse(value, out var parsed))
        {
            return parsed.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : parsed.ToUniversalTime();
        }

        return DateTime.UnixEpoch;
    }

    private static DateTime? ParseNullableUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ParseUtc(value);
    }
}

