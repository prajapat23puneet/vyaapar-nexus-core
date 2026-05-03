using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace VyaaparNexus.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetProducts()
    {
        return StatusCode(501, "Not Implemented");
    }

    [HttpGet("{id}")]
    public IActionResult GetProduct(string id)
    {
        return StatusCode(501, "Not Implemented");
    }
}
