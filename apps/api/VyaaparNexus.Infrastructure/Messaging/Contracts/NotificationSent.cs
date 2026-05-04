using System;

namespace VyaaparNexus.Infrastructure.Messaging.Contracts;

public record NotificationSent
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString();
    public Guid CorrelationId { get; init; }
    public Guid OrderId { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public string? NotificationReference { get; init; }
}
