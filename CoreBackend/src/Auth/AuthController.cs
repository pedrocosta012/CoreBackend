using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreBackend.Auth;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("register/owner")]
    public Task<IResult> RegisterOwner(
        [FromBody] RegisterOwnerRequest request,
        [FromServices] AuthService authService,
        CancellationToken cancellationToken) =>
        authService.RegisterOwnerAsync(request, cancellationToken);

    [HttpPost("register/worker")]
    public Task<IResult> RegisterWorker(
        [FromBody] RegisterWorkerRequest request,
        [FromServices] AuthService authService,
        CancellationToken cancellationToken) =>
        authService.RegisterWorkerAsync(request, cancellationToken);

    [HttpPost("login")]
    public Task<IResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] AuthService authService,
        CancellationToken cancellationToken) =>
        authService.LoginAsync(request, cancellationToken);

    [HttpPost("refresh")]
    public Task<IResult> Refresh(
        [FromBody] AuthTokenRefreshRequest request,
        [FromServices] AuthService authService,
        CancellationToken cancellationToken) =>
        authService.RefreshAsync(request, cancellationToken);

    [HttpPost("forgot-password")]
    public Task<IResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        [FromServices] AuthService authService,
        CancellationToken cancellationToken) =>
        authService.ForgotPasswordAsync(request, cancellationToken);

    [HttpPost("reset-password")]
    public Task<IResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        [FromServices] AuthService authService,
        CancellationToken cancellationToken) =>
        authService.ResetPasswordAsync(request, cancellationToken);

    [HttpPost("logout")]
    [Authorize]
    public Task<IResult> Logout(
        [FromServices] AuthService authService,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return authService.LogoutAsync(userId, cancellationToken);
    }
}
