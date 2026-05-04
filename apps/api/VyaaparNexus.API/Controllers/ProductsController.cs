using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VyaaparNexus.Application.Commands;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Application.Queries;

namespace VyaaparNexus.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] Guid? category = null, [FromQuery] string? search = null, [FromQuery] bool includeInactive = false)
    {
        var result = await _mediator.Send(new GetProductsQuery(page, size, category, search, includeInactive));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{id}/stock")]
    public async Task<IActionResult> GetProductStock(Guid id)
    {
        var result = await _mediator.Send(new GetProductStockQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("/api/v1/categories")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _mediator.Send(new GetCategoriesQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var id = await _mediator.Send(new CreateProductCommand(request));
        return CreatedAtAction(nameof(GetProduct), new { id }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
    {
        await _mediator.Send(new UpdateProductCommand(id, request));
        return NoContent();
    }

    [HttpPatch("{id}/stock")]
    public async Task<IActionResult> AdjustStock(Guid id, [FromBody] AdjustStockRequest request)
    {
        var result = await _mediator.Send(new AdjustStockCommand(id, request));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        await _mediator.Send(new DeleteProductCommand(id));
        return NoContent();
    }
}
