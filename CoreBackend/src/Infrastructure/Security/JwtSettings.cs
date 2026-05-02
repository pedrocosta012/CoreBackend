namespace CoreBackend.Infrastructure.Security;

internal sealed class JwtSettings
{
    public string Issuer { get; init; } = "CoreBackend";
    public string Audience { get; init; } = "CoreBackend";
    public int AuthTokenExpiryMinutes { get; init; } = 60;
    public int RefreshTokenExpiryDays { get; init; } = 7;
}

