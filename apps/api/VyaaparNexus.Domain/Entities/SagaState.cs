using System;

namespace VyaaparNexus.Domain.Entities;

public class SagaState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public bool InventoryReserved { get; set; } = false;
    public bool PaymentProcessed { get; set; } = false;
    public bool ShippingDispatched { get; set; } = false;
    public bool NotificationSent { get; set; } = false;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public int? DurationMs { get; set; }
    public string? LastError { get; set; }
}
