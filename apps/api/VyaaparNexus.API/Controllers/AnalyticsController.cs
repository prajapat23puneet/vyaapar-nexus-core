using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace VyaaparNexus.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AnalyticsController : ControllerBase
{
    [HttpGet("metrics")]
    public IActionResult GetMetrics()
    {
        return StatusCode(501, "Not Implemented");
    }
}
