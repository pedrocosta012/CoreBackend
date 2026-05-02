using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreBackend.Auth;

[ApiController]
[Route("me")]
public sealed class MeController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public Task<IResult> Get(
        [FromServices] AuthService authService,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return authService.MeAsync(userId, cancellationToken);
    }
}
