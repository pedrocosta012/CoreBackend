namespace CoreBackend.Auth;

internal sealed record RegisterRequest(string FirstName, string LastName, string Cpf, string Email, string Phone, string Password);
internal sealed record RegisterOwnerRequest(string FirstName, string LastName, string Cpf, string Email, string Phone, string Password,
    string CompanyName, string OfficeType, string? TaxId);
internal sealed record RegisterWorkerRequest(string FirstName, string LastName, string Cpf, string Email, string Phone, string Password,
    string CompanyId);
internal sealed record LoginRequest(string Identifier, string Password);
internal sealed record AuthTokenRefreshRequest(string RefreshToken);
internal sealed record ForgotPasswordRequest(string Email);
internal sealed record ResetPasswordRequest(string Token, string NewPassword);
internal sealed record TokensResponse(string AuthToken, string RefreshToken);
internal sealed record MeResponse(string Id, string FirstName, string LastName, string Cpf, string Email, string Phone);
