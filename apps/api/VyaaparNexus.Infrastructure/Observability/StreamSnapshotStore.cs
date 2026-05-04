using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using Serilog.Events;
using VyaaparNexus.Application.DTOs;

namespace VyaaparNexus.Infrastructure.Observability;

/// <summary>
/// Singleton in-memory store for the current SSE metrics snapshot and a rolling
/// buffer of the 50 most recent log entries.
///
/// Wire-up:
///   services.AddSingleton&lt;StreamSnapshotStore&gt;();
///   Serilog: .WriteTo.Sink(sp.GetRequiredService&lt;StreamSnapshotStore&gt;())
/// </summary>
public sealed class StreamSnapshotStore : ILogEventSink
{
    private const int MaxLogEntries = 50;

    private volatile StreamMetricsDto _currentSnapshot = new()
    {
        CircuitStates = new Dictionary<string, string>
        {
            ["inventory"]    = "Closed",
            ["payment"]      = "Closed",
            ["shipping"]     = "Closed",
            ["notification"] = "Closed",
        },
        Timestamp = DateTimeOffset.UtcNow,
    };

    private readonly ConcurrentQueue<LogEntryDto> _recentLogs = new();

    // ── Public snapshot API ──────────────────────────────────────────────────

    /// <summary>Latest snapshot written by MetricsSnapshotService.</summary>
    public StreamMetricsDto CurrentSnapshot => _currentSnapshot;

    /// <summary>Called by MetricsSnapshotService every ~1 s.</summary>
    public void UpdateSnapshot(StreamMetricsDto snapshot)
    {
        // Attach the current rolling log list before swapping
        snapshot.RecentLogs = _recentLogs.ToList();
        _currentSnapshot    = snapshot;
    }

    // ── Log buffer API ───────────────────────────────────────────────────────

    public void AppendLog(string level, string service, string message, string? correlationId)
    {
        var entry = new LogEntryDto
        {
            Timestamp     = DateTimeOffset.UtcNow,
            Level         = level,
            Service       = service,
            Message       = message,
            CorrelationId = correlationId,
        };

        _recentLogs.Enqueue(entry);

        // Trim overflow — ConcurrentQueue.Count is O(1) in .NET 8
        while (_recentLogs.Count > MaxLogEntries)
            _recentLogs.TryDequeue(out _);
    }

    // ── ILogEventSink (Serilog) ──────────────────────────────────────────────

    /// <summary>
    /// Called by Serilog for every log event. Extracts the Source/Service from the
    /// "SourceContext" property (set automatically by ILogger&lt;T&gt;), strips the
    /// namespace prefix so the terminal shows a short name like "OutboxPublisher".
    /// </summary>
    void ILogEventSink.Emit(LogEvent logEvent)
    {
        var level   = logEvent.Level.ToString();
        var message = logEvent.RenderMessage();

        // Extract service name from SourceContext property
        var service = "API";
        if (logEvent.Properties.TryGetValue("SourceContext", out var ctx))
        {
            var raw = ctx.ToString().Trim('"');
            // Take the last segment after the final '.'
            var dot = raw.LastIndexOf('.');
            service = dot >= 0 ? raw[(dot + 1)..] : raw;
        }

        // Extract CorrelationId from Serilog enriched context if available
        string? correlationId = null;
        if (logEvent.Properties.TryGetValue("CorrelationId", out var corrProp))
            correlationId = corrProp.ToString().Trim('"');

        AppendLog(level, service, message, correlationId);
    }
}
