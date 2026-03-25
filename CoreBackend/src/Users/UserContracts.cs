namespace CoreBackend.Users;

internal sealed record CreateUserRequest(string Username, string Email, string Phone, string Password);
internal sealed record UpdateUserRequest(string Username, string Email, string Phone);
internal sealed record UserRow(string Id, string Username, string Email, string Phone);

