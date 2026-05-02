using Microsoft.AspNetCore.Mvc;

namespace CoreBackend.Health;

[ApiController]
[Route("health")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
