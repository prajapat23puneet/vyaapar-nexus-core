using System.Data;
using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Domain.Entities;
using VyaaparNexus.Domain.Enums;
using VyaaparNexus.Infrastructure.Caching;
using VyaaparNexus.Infrastructure.Messaging.Contracts;
using VyaaparNexus.Application.Observability;
using VyaaparNexus.Infrastructure.Persistence;

namespace VyaaparNexus.Infrastructure.Messaging.Consumers;


public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly AppDbContext _context;
    private readonly LockService _lockService;

    public OrderCreatedConsumer(AppDbContext context, LockService lockService)
    {
        _context = context;
        _lockService = lockService;
    }

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var message = context.Message;
        var consumerName = nameof(OrderCreatedConsumer);
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
        var orderItems = await _context.OrderItems.Where(i => i.OrderId == message.OrderId).ToListAsync(context.CancellationToken);
        if (order == null || saga == null)
            return;

        if (string.Equals(message.ForceFailure, "inventory", StringComparison.OrdinalIgnoreCase))
        {
            const string reason = "Forced inventory failure";
            var previousState = saga.CurrentState;

            // Issue 1: Write InventoryChecking event before failing
            order.Status = OrderStatus.InventoryChecking;
            saga.CurrentState = OrderStatus.InventoryChecking.ToString();
            order.UpdatedAt = now;

            var eventChecking = new SagaEventLog
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                EventType = "InventoryChecking",
                ServiceName = "Inventory",
                PreviousState = previousState,
                CurrentState = OrderStatus.InventoryChecking.ToString(),
                Message = "Checking inventory and acquiring locks",
                CreatedAt = now
            };

            var previousStateAfterChecking = saga.CurrentState;

            order.Status = OrderStatus.InventoryFailed;
            saga.CurrentState = OrderStatus.InventoryFailed.ToString();
            saga.LastError = reason;

            var event1 = new SagaEventLog
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                EventType = "InventoryFailed",
                ServiceName = "Inventory",
                PreviousState = previousStateAfterChecking,
                CurrentState = OrderStatus.InventoryFailed.ToString(),
                Message = reason,
                CreatedAt = now
            };

            order.Status = OrderStatus.OrderCancelled;
            order.FailureReason = reason;
            order.CancelledAt = now;
            order.UpdatedAt = now;

            saga.CurrentState = OrderStatus.OrderCancelled.ToString();
            saga.CompletedAt = now;
            saga.DurationMs = (int)(now - saga.StartedAt).TotalMilliseconds;

            var event2 = new SagaEventLog
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                EventType = "OrderCancelled",
                ServiceName = "Saga",
                PreviousState = OrderStatus.InventoryFailed.ToString(),
                CurrentState = OrderStatus.OrderCancelled.ToString(),
                DurationMs = saga.DurationMs,
                Message = reason,
                CreatedAt = now
            };

            _context.SagaEventLogs.AddRange(eventChecking, event1, event2);

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
            return;
        }

        var lockKeys = orderItems.Select(i => $"inventory:product:{i.ProductId}").Distinct().OrderBy(x => x).ToList();
        var acquired = new List<string>();
        string? failureReason = null;

        // Gap C: Set InventoryChecking state
        var prevStateBeforeLock = saga.CurrentState;
        order.Status = OrderStatus.InventoryChecking;
        order.UpdatedAt = now;
        saga.CurrentState = OrderStatus.InventoryChecking.ToString();

        _context.SagaEventLogs.Add(new SagaEventLog
        {
            CorrelationId = message.CorrelationId,
            OrderId = message.OrderId,
            EventType = "InventoryChecking",
            ServiceName = "Inventory",
            PreviousState = prevStateBeforeLock,
            CurrentState = OrderStatus.InventoryChecking.ToString(),
            Message = "Checking inventory and acquiring locks",
            CreatedAt = now
        });

        try
        {
            foreach (var key in lockKeys)
            {
                var ok = await _lockService.AcquireAsync(key, TimeSpan.FromSeconds(5), context.CancellationToken);
                if (!ok) throw new InvalidOperationException("Unable to acquire inventory lock.");
                acquired.Add(key);
            }

            var productIds = orderItems.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(context.CancellationToken);

            if (products.Count != productIds.Count || products.Any(p => !p.IsActive))
            {
                failureReason = "Order contains unavailable products.";
            }
            else
            {
                var insufficient = orderItems.FirstOrDefault(i =>
                {
                    var product = products.First(p => p.Id == i.ProductId);
                    return product.StockQuantity < i.Quantity;
                });

                if (insufficient != null)
                {
                    failureReason = $"Insufficient stock for product {insufficient.ProductId}.";
                }
            }

            if (failureReason != null)
            {
                var previousState = saga.CurrentState;

                order.Status = OrderStatus.InventoryFailed;
                saga.CurrentState = OrderStatus.InventoryFailed.ToString();
                saga.LastError = failureReason;

                var event1 = new SagaEventLog
                {
                    CorrelationId = message.CorrelationId,
                    OrderId = message.OrderId,
                    EventType = "InventoryFailed",
                    ServiceName = "Inventory",
                    PreviousState = previousState,
                    CurrentState = OrderStatus.InventoryFailed.ToString(),
                    Message = failureReason,
                    CreatedAt = now
                };

                order.Status = OrderStatus.OrderCancelled;
                order.FailureReason = failureReason;
                order.CancelledAt = now;
                order.UpdatedAt = now;

                saga.CurrentState = OrderStatus.OrderCancelled.ToString();
                saga.CompletedAt = now;
                saga.DurationMs = (int)(now - saga.StartedAt).TotalMilliseconds;

                var event2 = new SagaEventLog
                {
                    CorrelationId = message.CorrelationId,
                    OrderId = message.OrderId,
                    EventType = "OrderCancelled",
                    ServiceName = "Saga",
                    PreviousState = OrderStatus.InventoryFailed.ToString(),
                    CurrentState = OrderStatus.OrderCancelled.ToString(),
                    DurationMs = saga.DurationMs,
                    Message = failureReason,
                    CreatedAt = now
                };

                _context.SagaEventLogs.AddRange(event1, event2);
                MetricsRegistry.OrdersCancelledTotal.Inc();

                _context.InboxMessages.Add(new InboxMessage
                {
                    MessageId = messageId,
                    ConsumerName = consumerName,
                    CorrelationId = message.CorrelationId,
                    ProcessedAt = now
                });

                await _context.SaveChangesAsync(context.CancellationToken);
                await tx.CommitAsync(context.CancellationToken);
                return;
            }
            else
            {
                // Happy path
                foreach (var item in orderItems)
                {
                    var product = products.First(p => p.Id == item.ProductId);
                    product.StockQuantity -= item.Quantity;
                    product.UpdatedAt = now;
                }

                var previousState = saga.CurrentState;
                saga.CurrentState = OrderStatus.InventoryReserved.ToString();
                saga.InventoryReserved = true;
                order.Status = OrderStatus.InventoryReserved;
                order.UpdatedAt = now;

                _context.SagaEventLogs.Add(new SagaEventLog
                {
                    CorrelationId = message.CorrelationId,
                    OrderId = message.OrderId,
                    EventType = "InventoryReserved",
                    ServiceName = "Inventory",
                    PreviousState = previousState,
                    CurrentState = OrderStatus.InventoryReserved.ToString(),
                    Message = "Inventory reserved successfully",
                    CreatedAt = now
                });

                var paymentRequest = new PaymentProcessRequested
                {
                    MessageId = Guid.NewGuid().ToString("N"),
                    CorrelationId = message.CorrelationId,
                    OrderId = message.OrderId,
                    OccurredAt = DateTime.UtcNow,
                    TotalAmount = order.TotalAmount,
                    PaymentMethod = message.PaymentMethod,
                    ForceFailure = message.ForceFailure // Gap 10 implemented here
                };

                _context.OutboxMessages.Add(new OutboxMessage
                {
                    CorrelationId = message.CorrelationId,
                    MessageType = nameof(PaymentProcessRequested),
                    Payload = JsonSerializer.Serialize(paymentRequest),
                    Exchange = "order.events",
                    RoutingKey = "payment.process.requested",
                    CreatedAt = now
                });
            }

            // Gap B: Moved SaveChanges and Commit inside the try block
            _context.InboxMessages.Add(new InboxMessage
            {
                MessageId = messageId,
                ConsumerName = consumerName,
                CorrelationId = message.CorrelationId,
                ProcessedAt = now
            });

            await _context.SaveChangesAsync(context.CancellationToken);
            await tx.CommitAsync(context.CancellationToken);
        }
        finally
        {
            foreach (var key in acquired)
                await _lockService.ReleaseAsync(key, context.CancellationToken);
        }
    }
}