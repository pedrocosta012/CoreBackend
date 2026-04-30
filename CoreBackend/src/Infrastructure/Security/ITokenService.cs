namespace CoreBackend.Infrastructure.Security;

internal sealed record AuthenticatedUser(string Id, string FirstName, string LastName, string Email);

internal interface ITokenService
{
    string GenerateAccessToken(AuthenticatedUser user);
    string GenerateRefreshToken();
}
