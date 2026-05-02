namespace CoreBackend.Users;

public sealed record CreateUserRequest(string FirstName, string LastName, string Cpf, string Email, string Phone, string Password);
public sealed record UpdateUserRequest(string FirstName, string LastName, string Cpf, string Email, string Phone);
internal sealed record UserRow(string Id, string FirstName, string LastName, string Cpf, string Email, string Phone);
