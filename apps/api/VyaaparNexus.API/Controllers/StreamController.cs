using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace VyaaparNexus.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class StreamController : ControllerBase
{
    [HttpGet("sse")]
    public IActionResult GetSseStream()
    {
        return StatusCode(501, "Not Implemented");
    }
}
