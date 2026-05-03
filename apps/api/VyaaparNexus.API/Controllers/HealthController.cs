using Microsoft.AspNetCore.Mvc;

namespace VyaaparNexus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "pong" });
    }
}
