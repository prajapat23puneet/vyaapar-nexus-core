using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VyaaparNexus.Domain.Entities;
using VyaaparNexus.Infrastructure.Persistence;

namespace VyaaparNexus.Infrastructure.HostedServices;

/// <summary>
/// Background service that drains the outbox_messages table and publishes each
/// pending row to RabbitMQ via MassTransit IBus.
///
/// Design rules (prd-agent-v2.txt § 5.1 / build-order-plan.txt § 5.1):
///   • Poll every OUTBOX_PUBLISH_INTERVAL_MS (default 500 ms)
///   • Batch size controlled by OUTBOX_BATCH_SIZE (default 10)
///   • On success: set published_at = UtcNow
///   • On failure: increment retry_count, write last_error — do NOT throw
///   • Uses IServiceScopeFactory so EF Core is correctly scoped
/// </summary>
public sealed class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBus                 _bus;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly int _intervalMs;
    private readonly int _batchSize;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        IBus                 bus,
        IConfiguration       configuration,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _bus          = bus;
        _logger       = logger;
        _intervalMs   = configuration.GetValue<int>("OUTBOX_PUBLISH_INTERVAL_MS", 500);
        _batchSize    = configuration.GetValue<int>("OUTBOX_BATCH_SIZE", 10);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OutboxPublisherService started. Interval={IntervalMs}ms, BatchSize={BatchSize}",
            _intervalMs, _batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DrainBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown — exit cleanly
                break;
            }
            catch (Exception ex)
            {
                // Log and continue — a crash here would stop all message publishing
                _logger.LogError(ex, "Unexpected error in OutboxPublisherService drain loop");
            }

            await Task.Delay(_intervalMs, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("OutboxPublisherService stopped.");
    }

    private async Task DrainBatchAsync(CancellationToken ct)
    {
        await using var scope   = _scopeFactory.CreateAsyncScope();
        var             db      = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Fetch a batch of unpublished messages ordered by creation time
        var messages = await db.OutboxMessages
            .Where(m => m.PublishedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(_batchSize)
            .ToListAsync(ct);

        if (messages.Count == 0)
            return;

        _logger.LogDebug("OutboxPublisher draining {Count} message(s)", messages.Count);

        foreach (var msg in messages)
        {
            await PublishMessageAsync(db, msg, ct);
        }

        // Persist all updates (published_at or retry_count/last_error) in one round-trip
        await db.SaveChangesAsync(ct);
    }

    private async Task PublishMessageAsync(AppDbContext db, OutboxMessage msg, CancellationToken ct)
    {
        try
        {
            // Deserialize the stored JSON payload into a plain dictionary and publish
            // using the send endpoint resolved from the exchange + routing key stored
            // in the outbox row.  MassTransit routes from the endpoint URI.
            var endpoint = await _bus.GetSendEndpoint(
                BuildEndpointUri(msg.Exchange, msg.RoutingKey));

            // Deserialize payload back to a dictionary for generic publishing
            var payloadObj = JsonSerializer.Deserialize<Dictionary<string, object>>(msg.Payload)
                             ?? new Dictionary<string, object>();

            await endpoint.Send<IDictionary<string, object>>(payloadObj, ctx =>
            {
                ctx.CorrelationId = msg.CorrelationId;
                ctx.Headers.Set("message-type",  msg.MessageType);
                ctx.Headers.Set("routing-key",   msg.RoutingKey);
                ctx.Headers.Set("exchange",      msg.Exchange);
            }, ct);

            // ✅ Mark published
            msg.PublishedAt = DateTimeOffset.UtcNow;

            _logger.LogInformation(
                "Outbox published {MessageType} | CorrelationId={CorrelationId}",
                msg.MessageType, msg.CorrelationId);
        }
        catch (Exception ex)
        {
            // ❌ Do NOT rethrow — increment retry and store error
            msg.RetryCount++;
            msg.LastError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;

            _logger.LogWarning(ex,
                "Failed to publish outbox message {Id} (retry #{RetryCount}): {Error}",
                msg.Id, msg.RetryCount, msg.LastError);
        }
    }

    /// <summary>
    /// Builds a RabbitMQ send-endpoint URI in the form understood by MassTransit:
    ///   rabbitmq://host/exchange?routing-key=key&amp;type=direct
    /// For Phase 5 we use the exchange name as the queue address — consumers created
    /// in Phase 6 will bind their queues to this exchange.
    /// </summary>
    private static Uri BuildEndpointUri(string exchange, string routingKey)
    {
        // MassTransit RabbitMQ endpoint format: exchange:exchange-name?routing-key=key
        return new Uri($"exchange:{exchange}?routing-key={Uri.EscapeDataString(routingKey)}");
    }
}
