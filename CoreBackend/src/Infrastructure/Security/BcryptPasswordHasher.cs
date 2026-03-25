namespace CoreBackend.Infrastructure.Security;

internal sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string plainTextPassword) => BCrypt.Net.BCrypt.HashPassword(plainTextPassword);

    public bool Verify(string plainTextPassword, string hashedPassword) =>
        BCrypt.Net.BCrypt.Verify(plainTextPassword, hashedPassword);
}

