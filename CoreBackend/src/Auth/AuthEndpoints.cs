using System.Security.Claims;

namespace CoreBackend.Auth;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", (RegisterEmployeeRequest request, AuthService authService, CancellationToken cancellationToken) =>
            authService.RegisterAsync(request, cancellationToken));

        app.MapPost("/auth/register/owner", (RegisterOwnerRequest request, AuthService authService, CancellationToken cancellationToken) =>
            authService.RegisterOwnerAsync(request, cancellationToken));

        app.MapPost("/auth/register/worker", (RegisterWorkerRequest request, AuthService authService, CancellationToken cancellationToken) =>
            authService.RegisterWorkerAsync(request, cancellationToken));

        app.MapPost("/auth/login", (LoginRequest request, AuthService authService, CancellationToken cancellationToken) =>
            authService.LoginAsync(request, cancellationToken));

        app.MapPost("/auth/refresh", (AuthTokenRefreshRequest request, AuthService authService, CancellationToken cancellationToken) =>
            authService.RefreshAsync(request, cancellationToken));

        app.MapPost("/auth/forgot-password", (ForgotPasswordRequest request, AuthService authService, CancellationToken cancellationToken) =>
            authService.ForgotPasswordAsync(request, cancellationToken));

        app.MapPost("/auth/reset-password", (ResetPasswordRequest request, AuthService authService, CancellationToken cancellationToken) =>
            authService.ResetPasswordAsync(request, cancellationToken));

        app.MapPost("/auth/logout", (ClaimsPrincipal principal, AuthService authService, CancellationToken cancellationToken) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            return authService.LogoutAsync(userId, cancellationToken);
        }).RequireAuthorization();

        app.MapGet("/me", (ClaimsPrincipal principal, AuthService authService, CancellationToken cancellationToken) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            return authService.MeAsync(userId, cancellationToken);
        }).RequireAuthorization();
    }
}

