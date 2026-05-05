using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Application.Interfaces;
using VyaaparNexus.Domain.Entities;
using VyaaparNexus.Domain.Enums;
using VyaaparNexus.Infrastructure.Messaging.Contracts;
using VyaaparNexus.Infrastructure.Persistence;

namespace VyaaparNexus.Infrastructure.Messaging.Consumers;

public class ShippingDispatchRequestedConsumer : IConsumer<ShippingDispatchRequested>
{
    private readonly AppDbContext _context;
    private readonly IShippingService _shippingService;

    public ShippingDispatchRequestedConsumer(AppDbContext context, IShippingService shippingService)
    {
        _context = context;
        _shippingService = shippingService;
    }

    public async Task Consume(ConsumeContext<ShippingDispatchRequested> context)
    {
        var message = context.Message;
        var consumerName = nameof(ShippingDispatchRequestedConsumer);
        var messageId = context.MessageId?.ToString() ?? message.MessageId;
        var now = DateTimeOffset.UtcNow;

        if (await _context.InboxMessages.AnyAsync(i => i.MessageId == messageId && i.ConsumerName == consumerName, context.CancellationToken))
            return;

        await using var tx = await _context.Database.BeginTransactionAsync(context.CancellationToken);

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId, context.CancellationToken);
        var saga = await _context.SagaStates.FirstOrDefaultAsync(s => s.OrderId == message.OrderId, context.CancellationToken);
        if (order == null || saga == null)
            return;

        order.Status = OrderStatus.ShippingDispatching;
        order.UpdatedAt = now;
        saga.CurrentState = OrderStatus.ShippingDispatching.ToString();

        _context.SagaEventLogs.Add(new SagaEventLog
        {
            CorrelationId = message.CorrelationId,
            OrderId = message.OrderId,
            EventType = "ShippingDispatching",
            ServiceName = "Shipping",
            PreviousState = OrderStatus.PaymentProcessed.ToString(),
            CurrentState = OrderStatus.ShippingDispatching.ToString(),
            Message = "Shipping dispatch started",
            CreatedAt = now
        });

        var shipmentRef = await _shippingService.DispatchAsync(message.OrderId, message.ShippingAddress, context.CancellationToken);

        order.Status = OrderStatus.ShippingDispatched;
        order.UpdatedAt = now;
        saga.CurrentState = OrderStatus.ShippingDispatched.ToString();
        saga.ShippingDispatched = true;

        _context.SagaEventLogs.Add(new SagaEventLog
        {
            CorrelationId = message.CorrelationId,
            OrderId = message.OrderId,
            EventType = "ShippingDispatched",
            ServiceName = "Shipping",
            PreviousState = OrderStatus.ShippingDispatching.ToString(),
            CurrentState = OrderStatus.ShippingDispatched.ToString(),
            Message = $"Shipping dispatched with reference {shipmentRef}",
            CreatedAt = now
        });

        var customerEmail = await _context.Customers
            .Where(c => c.Id == order.CustomerId)
            .Select(c => c.Email)
            .FirstOrDefaultAsync(context.CancellationToken);

        var notificationRequest = new NotificationSendRequested
        {
            MessageId = Guid.NewGuid().ToString("N"),
            CorrelationId = message.CorrelationId,
            OrderId = message.OrderId,
            OccurredAt = DateTime.UtcNow,
            Channel = "email",
            CustomerEmail = customerEmail
        };

        _context.OutboxMessages.Add(new OutboxMessage
        {
            CorrelationId = message.CorrelationId,
            MessageType = nameof(NotificationSendRequested),
            Payload = JsonSerializer.Serialize(notificationRequest),
            Exchange = "order.events",
            RoutingKey = "notification.send.requested",
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
}
