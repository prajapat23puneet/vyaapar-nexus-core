using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VyaaparNexus.Application.Queries;

namespace VyaaparNexus.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalyticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var result = await _mediator.Send(new GetAnalyticsSummaryQuery());
        return Ok(result);
    }

    [HttpGet("orders-over-time")]
    public async Task<IActionResult> GetOrdersOverTime([FromQuery] int days = 30)
    {
        var result = await _mediator.Send(new GetOrdersOverTimeQuery(days));
        return Ok(result);
    }

    [HttpGet("saga-success-rate")]
    public async Task<IActionResult> GetSagaSuccessRate([FromQuery] int days = 7)
    {
        var result = await _mediator.Send(new GetSagaSuccessRateQuery(days));
        return Ok(result);
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 10)
    {
        var result = await _mediator.Send(new GetTopProductsQuery(limit));
        return Ok(result);
    }
}
