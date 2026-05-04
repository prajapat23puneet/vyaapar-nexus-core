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
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var result = await _mediator.Send(new GetCustomersQuery(page, size));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        var id = await _mediator.Send(new CreateCustomerCommand(request));
        return CreatedAtAction(nameof(GetCustomer), new { id }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        await _mediator.Send(new UpdateCustomerCommand(id, request));
        return NoContent();
    }
}
