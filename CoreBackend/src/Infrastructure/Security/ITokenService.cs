namespace CoreBackend.Infrastructure.Security;

internal sealed record AuthenticatedUser(string Id, string Username, string Email);

internal interface ITokenService
{
    string GenerateAccessToken(AuthenticatedUser user);
    string GenerateRefreshToken();
}

