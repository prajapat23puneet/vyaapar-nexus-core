using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VyaaparNexus.Infrastructure.Observability;

namespace VyaaparNexus.API.Controllers;

/// <summary>
/// GET /api/stream — Server-Sent Events endpoint.
///
/// Spec: build-order-plan.txt § 5.4 / prd-agent-v2.txt § 13.1
///   • Content-Type: text/event-stream
///   • Cache-Control: no-cache
///   • X-Accel-Buffering: no  (disables nginx buffering)
///   • Emits one SSE "data: {json}\n\n" event per second
///   • Disconnects cleanly when the client closes the connection
///   • Auth: handled by ApiKeyMiddleware (all /api/* routes require X-Api-Key)
/// </summary>
[ApiController]
[Route("api")]
public class StreamController : ControllerBase
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling              = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    private readonly StreamSnapshotStore              _store;
    private readonly ILogger<StreamController>        _logger;

    public StreamController(
        StreamSnapshotStore        store,
        ILogger<StreamController>  logger)
    {
        _store  = store;
        _logger = logger;
    }

    [HttpGet("stream")]
    public async Task GetSseStream(CancellationToken cancellationToken)
    {
        var response = Response;

        response.Headers["Content-Type"]       = "text/event-stream";
        response.Headers["Cache-Control"]      = "no-cache";
        response.Headers["X-Accel-Buffering"]  = "no";
        response.Headers["Connection"]         = "keep-alive";

        // Disable response buffering so chunks flush immediately
        await response.Body.FlushAsync(cancellationToken);
    
        _logger.LogInformation("SSE client connected from {RemoteIp}",
            HttpContext.Connection.RemoteIpAddress);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var snapshot = _store.CurrentSnapshot;
                var json     = JsonSerializer.Serialize(snapshot, _jsonOptions);

                // SSE wire format: "data: {payload}\n\n"
                var data   = $"data: {json}\n\n";
                var bytes  = System.Text.Encoding.UTF8.GetBytes(data);

                await response.Body.WriteAsync(bytes, cancellationToken);
                await response.Body.FlushAsync(cancellationToken);

                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — this is normal, not an error
            _logger.LogInformation("SSE client disconnected.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SSE stream terminated with error.");
        }
    }
}
