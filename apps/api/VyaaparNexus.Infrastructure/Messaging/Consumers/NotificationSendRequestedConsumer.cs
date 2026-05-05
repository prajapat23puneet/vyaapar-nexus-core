using MassTransit;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Application.Interfaces;
using VyaaparNexus.Domain.Entities;
using VyaaparNexus.Domain.Enums;
using VyaaparNexus.Infrastructure.Messaging.Contracts;
using VyaaparNexus.Infrastructure.Observability;
using VyaaparNexus.Infrastructure.Persistence;

namespace VyaaparNexus.Infrastructure.Messaging.Consumers;

public class NotificationSendRequestedConsumer : IConsumer<NotificationSendRequested>
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;

    public NotificationSendRequestedConsumer(AppDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<NotificationSendRequested> context)
    {
        var message = context.Message;
        var consumerName = nameof(NotificationSendRequestedConsumer);
        var messageId = context.MessageId?.ToString() ?? message.MessageId;
        var now = DateTimeOffset.UtcNow;

        if (await _context.InboxMessages.AnyAsync(i => i.MessageId == messageId && i.ConsumerName == consumerName, context.CancellationToken))
            return;

        await using var tx = await _context.Database.BeginTransactionAsync(context.CancellationToken);

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId, context.CancellationToken);
        var saga = await _context.SagaStates.FirstOrDefaultAsync(s => s.OrderId == message.OrderId, context.CancellationToken);
        if (order == null || saga == null)
            return;

        await _notificationService.SendAsync(message.OrderId, message.Channel, message.CustomerEmail, context.CancellationToken);

        order.Status = OrderStatus.OrderCompleted;
        order.CompletedAt = now;
        order.UpdatedAt = now;

        saga.CurrentState = OrderStatus.OrderCompleted.ToString();
        saga.NotificationSent = true;
        saga.CompletedAt = now;
        saga.DurationMs = (int)(now - saga.StartedAt).TotalMilliseconds;

        _context.SagaEventLogs.AddRange(
            new SagaEventLog
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                EventType = "NotificationSent",
                ServiceName = "Notification",
                PreviousState = OrderStatus.ShippingDispatched.ToString(),
                CurrentState = OrderStatus.ShippingDispatched.ToString(),
                Message = "Notification sent successfully",
                CreatedAt = now
            },
            new SagaEventLog
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                EventType = "OrderCompleted",
                ServiceName = "Saga",
                PreviousState = OrderStatus.ShippingDispatched.ToString(),
                CurrentState = OrderStatus.OrderCompleted.ToString(),
                DurationMs = saga.DurationMs,
                Message = "Order saga completed",
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

        MetricsRegistry.OrdersCompletedTotal.Inc();
        MetricsRegistry.SagaDurationMs.Observe(saga.DurationMs ?? 0);
    }
}
