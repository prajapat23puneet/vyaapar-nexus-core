using System.Data;
using System.Linq;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VyaaparNexus.Domain.Entities;
using VyaaparNexus.Domain.Enums;
using VyaaparNexus.Infrastructure.Messaging.Contracts;
using VyaaparNexus.Application.Observability;
using VyaaparNexus.Infrastructure.Persistence;

namespace VyaaparNexus.Infrastructure.Messaging.Consumers;

public class InventoryReleaseRequestedConsumer : IConsumer<InventoryReleaseRequested>
{
    private readonly AppDbContext _context;
    private readonly ILogger<InventoryReleaseRequestedConsumer> _logger;

    public InventoryReleaseRequestedConsumer(AppDbContext context, ILogger<InventoryReleaseRequestedConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InventoryReleaseRequested> context)
    {
        var message = context.Message;
        var consumerName = nameof(InventoryReleaseRequestedConsumer);
        var messageId = context.MessageId?.ToString() ?? message.MessageId;
        var now = DateTimeOffset.UtcNow;

        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, context.CancellationToken);

        if (await _context.InboxMessages.AnyAsync(i => i.MessageId == messageId && i.ConsumerName == consumerName, context.CancellationToken))
        {
            await tx.RollbackAsync(context.CancellationToken);
            return;
        }

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId, context.CancellationToken);
        var saga = await _context.SagaStates.FirstOrDefaultAsync(s => s.OrderId == message.OrderId, context.CancellationToken);
        if (order == null || saga == null)
        {
            _logger.LogError("Order {OrderId} not found in InventoryReleaseRequestedConsumer", message.OrderId);
            return;
        }

        var productIds = message.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(context.CancellationToken);

        foreach (var item in message.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product == null)
                continue;

            product.StockQuantity += item.Quantity;
            product.UpdatedAt = now;
        }

        var cancellationReason = saga.LastError ?? order.FailureReason ?? "Compensation completed after payment failure";
        var previousState = saga.CurrentState;

        order.Status = OrderStatus.OrderCancelled;
        order.FailureReason = cancellationReason;
        order.CancelledAt = now;
        order.UpdatedAt = now;

        saga.CurrentState = OrderStatus.OrderCancelled.ToString();
        saga.CompletedAt = now;
        saga.DurationMs = (int)(now - saga.StartedAt).TotalMilliseconds;
        saga.LastError = cancellationReason;

        _context.SagaEventLogs.AddRange(
            new SagaEventLog
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                EventType = "InventoryReleased",
                ServiceName = "Inventory",
                PreviousState = previousState,
                CurrentState = previousState,
                Message = "Inventory restored for compensated order",
                CreatedAt = now
            },
            new SagaEventLog
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                EventType = "OrderCancelled",
                ServiceName = "Saga",
                PreviousState = previousState,
                CurrentState = OrderStatus.OrderCancelled.ToString(),
                DurationMs = saga.DurationMs,
                Message = cancellationReason,
                CreatedAt = now
            });

        _context.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = consumerName,
            CorrelationId = message.CorrelationId,
            ProcessedAt = now
        });

        MetricsRegistry.OrdersCancelledTotal.Inc();

        await _context.SaveChangesAsync(context.CancellationToken);
        await tx.CommitAsync(context.CancellationToken);
    }
}
