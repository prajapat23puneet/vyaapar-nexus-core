using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace VyaaparNexus.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CustomersController : ControllerBase
{
    [HttpGet]
    public IActionResult GetCustomers()
    {
        return StatusCode(501, "Not Implemented");
    }
}
