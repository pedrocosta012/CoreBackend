namespace CoreBackend.Auth;

internal sealed record RegisterRequest(string Username, string Email, string Phone, string Password);
internal sealed record LoginRequest(string Identifier, string Password);
internal sealed record AuthTokenRefreshRequest(string RefreshToken);
internal sealed record ForgotPasswordRequest(string Email);
internal sealed record ResetPasswordRequest(string Token, string NewPassword);
internal sealed record TokensResponse(string AuthToken, string RefreshToken);
internal sealed record MeResponse(string Id, string Username, string Email, string Phone);

