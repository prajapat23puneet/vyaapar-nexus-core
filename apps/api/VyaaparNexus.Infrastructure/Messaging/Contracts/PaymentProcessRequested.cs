using System;

namespace VyaaparNexus.Infrastructure.Messaging.Contracts;

public record PaymentProcessRequested
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString();
    public Guid CorrelationId { get; init; }
    public Guid OrderId { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public decimal TotalAmount { get; init; }
    public string PaymentMethod { get; init; } = null!;
}
