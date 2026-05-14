using Microsoft.AspNetCore.Mvc;
using VyaaparNexus.Infrastructure.Persistence;
using StackExchange.Redis;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace VyaaparNexus.API.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly IBus _bus;
    private readonly ILogger<HealthController> _logger;

    public HealthController(AppDbContext dbContext, IConnectionMultiplexer redis, IBus bus, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _redis = redis;
        _bus = bus;
        _logger = logger;
    }

    [HttpGet("api/health/ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "pong" });
    }

    [HttpGet("health/live")]
    public IActionResult Live()
    {
        return Ok(new { status = "Healthy" });
    }

    [HttpGet("health/ready")]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        var dbStatus = "fail";
        var redisStatus = "fail";
        var rabbitStatus = "fail";

        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(ct);
            if (canConnect) dbStatus = "ok";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check: DB failure");
        }

        try
        {
            var db = _redis.GetDatabase();
            var ping = await db.PingAsync();
            if (ping.TotalMilliseconds >= 0) redisStatus = "ok";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check: Redis failure");
        }

        try
        {
            // For MassTransit bus, we can check if its Address is available
            if (_bus.Topology != null) rabbitStatus = "ok";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check: RabbitMQ failure");
        }

        var isHealthy = dbStatus == "ok" && redisStatus == "ok" && rabbitStatus == "ok";

        var response = new
        {
            status = isHealthy ? "Healthy" : "Degraded",
            database = dbStatus,
            redis = redisStatus,
            rabbitmq = rabbitStatus
        };

        if (isHealthy)
            return Ok(response);
        else
            return StatusCode(503, response);
    }
}
