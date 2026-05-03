using System;

namespace VyaaparNexus.Domain.Entities;

public class MetricsSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ActiveSagas { get; set; }
    public int DeadLetterCount { get; set; }
    public int OutboxPending { get; set; }
    public decimal OrdersPerMinute { get; set; }
    public decimal SagaSuccessRate { get; set; }
    public int P95LatencyMs { get; set; }
    public decimal CpuPercent { get; set; }
    public decimal MemoryPercent { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
