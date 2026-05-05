using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Application.Interfaces;
using VyaaparNexus.Domain.Entities;
using VyaaparNexus.Domain.Enums;
using VyaaparNexus.Infrastructure.Messaging.Contracts;
using VyaaparNexus.Infrastructure.Persistence;

namespace VyaaparNexus.Infrastructure.Messaging.Consumers;

public class PaymentProcessRequestedConsumer : IConsumer<PaymentProcessRequested>
{
    private readonly AppDbContext _context;
    private readonly IPaymentService _paymentService;

    public PaymentProcessRequestedConsumer(AppDbContext context, IPaymentService paymentService)
    {
        _context = context;
        _paymentService = paymentService;
    }

    public async Task Consume(ConsumeContext<PaymentProcessRequested> context)
    {
        var message = context.Message;
        var consumerName = nameof(PaymentProcessRequestedConsumer);
        var messageId = context.MessageId?.ToString() ?? message.MessageId;
        var now = DateTimeOffset.UtcNow;

        if (await _context.InboxMessages.AnyAsync(i => i.MessageId == messageId && i.ConsumerName == consumerName, context.CancellationToken))
            return;

        await using var tx = await _context.Database.BeginTransactionAsync(context.CancellationToken);

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId, context.CancellationToken);
        var saga = await _context.SagaStates.FirstOrDefaultAsync(s => s.OrderId == message.OrderId, context.CancellationToken);
        if (order == null || saga == null)
            return;

        order.Status = OrderStatus.PaymentProcessing;
        order.UpdatedAt = now;
        saga.CurrentState = OrderStatus.PaymentProcessing.ToString();

        _context.SagaEventLogs.Add(new SagaEventLog
        {
            CorrelationId = message.CorrelationId,
            OrderId = message.OrderId,
            EventType = "PaymentProcessing",
            ServiceName = "Payment",
            PreviousState = OrderStatus.InventoryReserved.ToString(),
            CurrentState = OrderStatus.PaymentProcessing.ToString(),
            Message = "Payment processing started",
            CreatedAt = now
        });

        var paymentRef = await _paymentService.ProcessPaymentAsync(
            message.OrderId,
            message.TotalAmount,
            message.PaymentMethod,
            null,
            context.CancellationToken);

        order.Status = OrderStatus.PaymentProcessed;
        order.PaymentReference = paymentRef;
        order.UpdatedAt = now;
        saga.CurrentState = OrderStatus.PaymentProcessed.ToString();
        saga.PaymentProcessed = true;

        _context.SagaEventLogs.Add(new SagaEventLog
        {
            CorrelationId = message.CorrelationId,
            OrderId = message.OrderId,
            EventType = "PaymentProcessed",
            ServiceName = "Payment",
            PreviousState = OrderStatus.PaymentProcessing.ToString(),
            CurrentState = OrderStatus.PaymentProcessed.ToString(),
            Message = "Payment processed successfully",
            CreatedAt = now
        });

        var orderItems = await _context.OrderItems
            .Where(i => i.OrderId == message.OrderId)
            .ToListAsync(context.CancellationToken);

        var shippingAddress = JsonSerializer.Deserialize<ShippingAddressContract>(order.ShippingAddress)
            ?? new ShippingAddressContract
            {
                Line1 = string.Empty,
                Line2 = null,
                City = string.Empty,
                State = string.Empty,
                Pincode = string.Empty,
                Country = "India"
            };

        var shippingRequest = new ShippingDispatchRequested
        {
            MessageId = Guid.NewGuid().ToString("N"),
            CorrelationId = message.CorrelationId,
            OrderId = message.OrderId,
            OccurredAt = DateTime.UtcNow,
            ShippingAddress = shippingAddress,
            ItemCount = orderItems.Sum(i => i.Quantity)
        };

        _context.OutboxMessages.Add(new OutboxMessage
        {
            CorrelationId = message.CorrelationId,
            MessageType = nameof(ShippingDispatchRequested),
            Payload = JsonSerializer.Serialize(shippingRequest),
            Exchange = "order.events",
            RoutingKey = "shipping.dispatch.requested",
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
