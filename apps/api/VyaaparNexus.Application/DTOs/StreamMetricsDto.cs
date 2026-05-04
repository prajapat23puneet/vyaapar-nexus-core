using System;
using System.Collections.Generic;

namespace VyaaparNexus.Application.DTOs;

/// <summary>
/// Canonical SSE payload — Section 13.1 of prd-agent-v2.txt.
/// Every field must be sourced from a real backend measurement.
/// No fake values, no Math.Random().
/// </summary>
public class StreamMetricsDto
{
    // ── Saga / Order counts ──────────────────────────────────────────
    public int    ActiveSagas      { get; set; }
    public int    DeadLetterCount  { get; set; }
    public int    OutboxPending    { get; set; }
    public double OrdersPerMinute  { get; set; }

    // ── Derived saga health ──────────────────────────────────────────
    /// <summary>Trailing-24h completed / (completed + cancelled). 0 when no terminal sagas exist.</summary>
    public double SagaSuccessRate  { get; set; }

    /// <summary>95th-percentile saga duration_ms across completed sagas. 0 when no data.</summary>
    public int    P95LatencyMs     { get; set; }

    // ── Process metrics ──────────────────────────────────────────────
    public double CpuPercent       { get; set; }
    public double MemoryPercent    { get; set; }

    // ── Circuit breaker states (per service) ────────────────────────
    public Dictionary<string, string> CircuitStates { get; set; } = new();

    // ── Active order being watched (latest non-terminal saga) ────────
    public ActiveOrderDto? ActiveOrder { get; set; }

    // ── Rolling log buffer (max 50) ─────────────────────────────────
    public List<LogEntryDto> RecentLogs { get; set; } = new();

    // ── Envelope ────────────────────────────────────────────────────
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public class ActiveOrderDto
{
    public Guid   OrderId        { get; set; }
    public Guid   CorrelationId  { get; set; }
    public string CurrentState   { get; set; } = string.Empty;
}

public class LogEntryDto
{
    public DateTimeOffset Timestamp     { get; set; }
    public string         Level         { get; set; } = string.Empty;
    public string         Service       { get; set; } = string.Empty;
    public string         Message       { get; set; } = string.Empty;
    public string?        CorrelationId { get; set; }
}
