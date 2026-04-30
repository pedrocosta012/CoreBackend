namespace CoreBackend.Users;

internal sealed record CreateUserRequest(string FirstName, string LastName, string Cpf, string Email, string Phone, string Password);
internal sealed record UpdateUserRequest(string FirstName, string LastName, string Cpf, string Email, string Phone);
internal sealed record UserRow(string Id, string FirstName, string LastName, string Cpf, string Email, string Phone);
