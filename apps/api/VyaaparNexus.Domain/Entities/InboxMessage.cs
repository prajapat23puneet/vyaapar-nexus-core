using System;

namespace VyaaparNexus.Domain.Entities;

public class InboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MessageId { get; set; } = string.Empty;
    public string ConsumerName { get; set; } = string.Empty;
    public Guid? CorrelationId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
