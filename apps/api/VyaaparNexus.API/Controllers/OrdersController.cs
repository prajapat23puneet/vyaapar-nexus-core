using MediatR;
using Microsoft.AspNetCore.Mvc;
using VyaaparNexus.Application.Commands;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Application.Queries;
using VyaaparNexus.Domain.Enums;
using VyaaparNexus.Infrastructure.Observability;

namespace VyaaparNexus.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var forceFailure = Request.Headers["X-Force-Failure"].ToString();
        var result = await _mediator.Send(new CreateOrderCommand(request, string.IsNullOrWhiteSpace(forceFailure) ? null : forceFailure));
        MetricsRegistry.OrdersSubmittedTotal.Inc(); 
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] OrderStatus? status = null, [FromQuery] Guid? customerId = null)
    {
        var result = await _mediator.Send(new GetOrdersQuery(page, size, status, customerId));
        return Ok(result);
    }

    [HttpGet("{id}/saga")]
    public async Task<IActionResult> GetOrderSaga(Guid id)
    {
        var result = await _mediator.Send(new GetOrderSagaQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{id}/trace")]
    public async Task<IActionResult> GetOrderTrace(Guid id)
    {
        var result = await _mediator.Send(new GetOrderTraceQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("demo")]
    public async Task<IActionResult> CreateDemoOrder()
    {
        var forceFailure = Request.Headers["X-Force-Failure"].ToString();
        var result = await _mediator.Send(new CreateDemoOrderCommand(string.IsNullOrWhiteSpace(forceFailure) ? null : forceFailure));
        MetricsRegistry.OrdersSubmittedTotal.Inc();
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("demo")]
    public async Task<IActionResult> CreateDemoOrderGet()
    {
        var forceFailure = Request.Headers["X-Force-Failure"].ToString();
        var result = await _mediator.Send(new CreateDemoOrderCommand(string.IsNullOrWhiteSpace(forceFailure) ? null : forceFailure));
        MetricsRegistry.OrdersSubmittedTotal.Inc();
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
