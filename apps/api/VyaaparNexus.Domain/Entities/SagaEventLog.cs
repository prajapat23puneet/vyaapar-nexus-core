using System;

namespace VyaaparNexus.Domain.Entities;

public class SagaEventLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CorrelationId { get; set; }
    public Guid OrderId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? PreviousState { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public string? Message { get; set; }
    public int? DurationMs { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Metadata { get; set; } // stored as JSONB
}
