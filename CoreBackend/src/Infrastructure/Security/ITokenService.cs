namespace CoreBackend.Infrastructure.Security;

public sealed record AuthenticatedUser(string Id, string FirstName, string LastName, string Email);

public interface ITokenService
{
    string GenerateAccessToken(AuthenticatedUser user);
    string GenerateRefreshToken();
}
