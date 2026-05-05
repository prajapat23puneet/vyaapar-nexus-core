using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Domain.Entities;
using VyaaparNexus.Domain.Enums;
using VyaaparNexus.Infrastructure.Caching;
using VyaaparNexus.Infrastructure.Messaging.Contracts;
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

        if (await _context.InboxMessages.AnyAsync(i => i.MessageId == messageId && i.ConsumerName == consumerName, context.CancellationToken))
            return;

        await using var tx = await _context.Database.BeginTransactionAsync(context.CancellationToken);

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId, context.CancellationToken);
        var saga = await _context.SagaStates.FirstOrDefaultAsync(s => s.OrderId == message.OrderId, context.CancellationToken);
        var orderItems = await _context.OrderItems.Where(i => i.OrderId == message.OrderId).ToListAsync(context.CancellationToken);
        if (order == null || saga == null)
            return;

        var lockKeys = orderItems.Select(i => $"inventory:product:{i.ProductId}").Distinct().OrderBy(x => x).ToList();
        var acquired = new List<string>();

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
                throw new InvalidOperationException("Order contains unavailable products.");

            var insufficient = orderItems.FirstOrDefault(i =>
            {
                var product = products.First(p => p.Id == i.ProductId);
                return product.StockQuantity < i.Quantity;
            });

            if (insufficient != null)
                throw new InvalidOperationException($"Insufficient stock for product {insufficient.ProductId}.");

            foreach (var item in orderItems)
            {
                var product = products.First(p => p.Id == item.ProductId);
                product.StockQuantity -= item.Quantity;
                product.UpdatedAt = now;
            }

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
                PreviousState = OrderStatus.Submitted.ToString(),
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
                PaymentMethod = message.PaymentMethod
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
