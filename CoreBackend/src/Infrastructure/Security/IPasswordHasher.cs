namespace CoreBackend.Infrastructure.Security;

internal interface IPasswordHasher
{
    string Hash(string plainTextPassword);
    bool Verify(string plainTextPassword, string hashedPassword);
}

