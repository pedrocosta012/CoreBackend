namespace CoreBackend.Auth;

public sealed record RegisterOwnerRequest(string FirstName, string LastName, string Cpf, string Email, string Phone, string Password,
    string CompanyName, string OfficeType, string? TaxId);
public sealed record RegisterWorkerRequest(string FirstName, string LastName, string Cpf, string Email, string Phone, string Password,
    string CompanyId);
public sealed record LoginRequest(string Identifier, string Password);
public sealed record AuthTokenRefreshRequest(string RefreshToken);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
internal sealed record TokensResponse(string AuthToken, string RefreshToken);
internal sealed record MeResponse(string Id, string FirstName, string LastName, string Cpf, string Email, string Phone);
