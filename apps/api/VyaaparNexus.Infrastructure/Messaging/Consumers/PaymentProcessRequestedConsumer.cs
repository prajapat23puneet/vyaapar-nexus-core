using System.Data;
using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Polly.CircuitBreaker;
using Polly.Registry;
using VyaaparNexus.Application.Interfaces;
using VyaaparNexus.Domain.Entities;
using VyaaparNexus.Domain.Enums;
using VyaaparNexus.Infrastructure.Messaging.Contracts;
using VyaaparNexus.Application.Observability;
using VyaaparNexus.Infrastructure.Observability;
using VyaaparNexus.Infrastructure.Persistence;

namespace VyaaparNexus.Infrastructure.Messaging.Consumers;

public class PaymentProcessRequestedConsumer : IConsumer<PaymentProcessRequested>
{
    private readonly AppDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IPolicyRegistry<string> _pollyRegistry;
    private readonly CircuitBreakerStateMonitor _circuitMonitor;

    public PaymentProcessRequestedConsumer(
        AppDbContext context,
        IPaymentService paymentService,
        IPolicyRegistry<string> pollyRegistry,
        CircuitBreakerStateMonitor circuitMonitor)
    {
        _context = context;
        _paymentService = paymentService;
        _pollyRegistry = pollyRegistry;
        _circuitMonitor = circuitMonitor;
    }

    public async Task Consume(ConsumeContext<PaymentProcessRequested> context)
    {
        var message = context.Message;
        var consumerName = nameof(PaymentProcessRequestedConsumer);
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
            return;

        var previousState = saga.CurrentState;
        order.Status = OrderStatus.PaymentProcessing;
        order.UpdatedAt = now;
        saga.CurrentState = OrderStatus.PaymentProcessing.ToString();

        _context.SagaEventLogs.Add(new SagaEventLog
        {
            CorrelationId = message.CorrelationId,
            OrderId = message.OrderId,
            EventType = "PaymentProcessing",
            ServiceName = "Payment",
            PreviousState = previousState,
            CurrentState = OrderStatus.PaymentProcessing.ToString(),
            Message = "Payment processing started",
            CreatedAt = now
        });

        string? failureReason = null;
        string? paymentRef = null;

        var policy = _pollyRegistry.Get<Polly.IAsyncPolicy<string>>("PaymentCircuitBreaker");
        try
        {
            paymentRef = await policy.ExecuteAsync(() =>
                _paymentService.ProcessPaymentAsync(
                    message.OrderId,
                    message.TotalAmount,
                    message.PaymentMethod,
                    message.ForceFailure,
                    context.CancellationToken));
        }
        catch (BrokenCircuitException)
        {
            failureReason = "Payment circuit open";
            _circuitMonitor.SetState("payment", VyaaparNexus.Domain.Enums.CircuitState.Open);
        }
        catch (Exception ex)
        {
            failureReason = ex.Message;
        }

        if (!string.IsNullOrWhiteSpace(failureReason))
        {
            var pState2 = saga.CurrentState;
            order.Status = OrderStatus.PaymentFailed;
            order.FailureReason = failureReason;
            order.UpdatedAt = now;

            saga.CurrentState = OrderStatus.PaymentFailed.ToString();
            saga.LastError = failureReason;

            _context.SagaEventLogs.Add(new SagaEventLog
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                EventType = "PaymentFailed",
                ServiceName = "Payment",
                PreviousState = pState2,
                CurrentState = OrderStatus.PaymentFailed.ToString(),
                Message = failureReason,
                CreatedAt = now
            });

            var reservedItems = await _context.OrderItems
                .Where(i => i.OrderId == message.OrderId)
                .Select(i => new InventoryReleaseItemContract
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                })
                .ToListAsync(context.CancellationToken);

            var releaseRequest = new InventoryReleaseRequested
            {
                MessageId = Guid.NewGuid().ToString("N"),
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                OccurredAt = DateTime.UtcNow,
                Items = reservedItems
            };

            _context.OutboxMessages.Add(new OutboxMessage
            {
                CorrelationId = message.CorrelationId,
                MessageType = nameof(InventoryReleaseRequested),
                Payload = JsonSerializer.Serialize(releaseRequest),
                Exchange = "order.events",
                RoutingKey = "inventory.release.requested",
                CreatedAt = now
            });
        }
        else
        {
            _circuitMonitor.SetState("payment", VyaaparNexus.Domain.Enums.CircuitState.Closed);
            var pState3 = saga.CurrentState;
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
                PreviousState = pState3,
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
        }

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
