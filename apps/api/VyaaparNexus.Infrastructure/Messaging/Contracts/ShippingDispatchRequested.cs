using System;

namespace VyaaparNexus.Infrastructure.Messaging.Contracts;

public record ShippingDispatchRequested
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString();
    public Guid CorrelationId { get; init; }
    public Guid OrderId { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public ShippingAddressContract ShippingAddress { get; init; } = null!;
    public int ItemCount { get; init; }
}
