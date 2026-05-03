using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace VyaaparNexus.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController : ControllerBase
{
    [HttpPost]
    public IActionResult CreateOrder()
    {
        return StatusCode(501, "Not Implemented");
    }

    [HttpGet("{id}")]
    public IActionResult GetOrder(string id)
    {
        return StatusCode(501, "Not Implemented");
    }
}
