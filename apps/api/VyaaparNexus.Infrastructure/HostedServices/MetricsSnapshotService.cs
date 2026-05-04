using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Domain.Enums;
using VyaaparNexus.Infrastructure.Observability;
using VyaaparNexus.Infrastructure.Persistence;

namespace VyaaparNexus.Infrastructure.HostedServices;

/// <summary>
/// Background service that runs every METRICS_SNAPSHOT_INTERVAL_MS (default 1 000 ms)
/// and computes a fresh StreamMetricsDto from real DB / Redis / process data.
///
/// Spec: build-order-plan.txt § 5.2 — ALL fields sourced from real data.
///   activeSagas        → COUNT saga_states WHERE status NOT IN terminal states
///   outboxPending      → COUNT outbox_messages WHERE published_at IS NULL
///   ordersPerMinute    → COUNT orders WHERE created_at > NOW() - 60s
///   sagaSuccessRate    → trailing 24h completed / (completed + cancelled)
///   p95LatencyMs       → percentile of completed saga duration_ms values (trailing 24h)
///   deadLetterCount    → Redis key "dl:count" or 0
///   cpuPercent         → process CPU time delta
///   memoryPercent      → GC working set as % of 1 GB reference (process-level proxy)
///   circuitStates      → CircuitBreakerStateMonitor.GetAll()
///   activeOrder        → latest non-terminal saga_states row
/// </summary>
public sealed class MetricsSnapshotService : BackgroundService
{
    private static readonly HashSet<string> TerminalStates =
        new(StringComparer.OrdinalIgnoreCase) { "OrderCompleted", "OrderCancelled" };

    private readonly IServiceScopeFactory          _scopeFactory;
    private readonly StreamSnapshotStore           _store;
    private readonly CircuitBreakerStateMonitor    _circuitMonitor;
    private readonly IConnectionMultiplexer        _redis;
    private readonly ILogger<MetricsSnapshotService> _logger;
    private readonly int _intervalMs;

    // CPU tracking
    private TimeSpan   _lastCpuTime    = TimeSpan.Zero;
    private DateTime   _lastCpuSample  = DateTime.UtcNow;

    public MetricsSnapshotService(
        IServiceScopeFactory       scopeFactory,
        StreamSnapshotStore        store,
        CircuitBreakerStateMonitor circuitMonitor,
        IConnectionMultiplexer     redis,
        IConfiguration             configuration,
        ILogger<MetricsSnapshotService> logger)
    {
        _scopeFactory   = scopeFactory;
        _store          = store;
        _circuitMonitor = circuitMonitor;
        _redis          = redis;
        _logger         = logger;
        _intervalMs     = configuration.GetValue<int>("METRICS_SNAPSHOT_INTERVAL_MS", 1000);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "MetricsSnapshotService started. Interval={IntervalMs}ms", _intervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var snapshot = await BuildSnapshotAsync(stoppingToken);
                _store.UpdateSnapshot(snapshot);

                // Update Prometheus gauges from the snapshot (real values, not duplicated queries)
                MetricsRegistry.ActiveSagasGauge.Set(snapshot.ActiveSagas);
                MetricsRegistry.OutboxPendingGauge.Set(snapshot.OutboxPending);
                MetricsRegistry.DeadLetterCountGauge.Set(snapshot.DeadLetterCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing metrics snapshot");
            }

            await Task.Delay(_intervalMs, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("MetricsSnapshotService stopped.");
    }

    private async Task<StreamMetricsDto> BuildSnapshotAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now        = DateTimeOffset.UtcNow;
        var windowStart24h = now.AddHours(-24);
        var windowStart1m  = now.AddSeconds(-60);

        // ── 1. activeSagas ────────────────────────────────────────────────────
        var activeSagas = await db.SagaStates
            .CountAsync(s => !TerminalStates.Contains(s.CurrentState), ct);

        // ── 2. outboxPending ─────────────────────────────────────────────────
        var outboxPending = await db.OutboxMessages
            .CountAsync(m => m.PublishedAt == null, ct);

        // ── 3. ordersPerMinute ───────────────────────────────────────────────
        var recentOrderCount = await db.Orders
            .CountAsync(o => o.CreatedAt > windowStart1m, ct);
        var ordersPerMinute = (double)recentOrderCount; // count in the trailing 60 s window

        // ── 4. sagaSuccessRate ───────────────────────────────────────────────
        var completed24h = await db.SagaStates
            .CountAsync(s => s.CurrentState == "OrderCompleted"
                          && s.CompletedAt != null
                          && s.CompletedAt > windowStart24h, ct);

        var cancelled24h = await db.SagaStates
            .CountAsync(s => s.CurrentState == "OrderCancelled"
                          && s.CompletedAt != null
                          && s.CompletedAt > windowStart24h, ct);

        var totalTerminal = completed24h + cancelled24h;
        var sagaSuccessRate = totalTerminal > 0
            ? Math.Round((double)completed24h / totalTerminal, 4)
            : 0.0;

        // ── 5. p95LatencyMs ──────────────────────────────────────────────────
        var durations = await db.SagaStates
            .Where(s => s.CurrentState == "OrderCompleted"
                     && s.DurationMs != null
                     && s.CompletedAt != null
                     && s.CompletedAt > windowStart24h)
            .Select(s => s.DurationMs!.Value)
            .ToListAsync(ct);

        var p95Latency = ComputeP95(durations);

        // ── 6. deadLetterCount ───────────────────────────────────────────────
        var deadLetterCount = await GetDeadLetterCountAsync();

        // ── 7. cpuPercent / memoryPercent ─────────────────────────────────────
        var (cpuPercent, memPercent) = ComputeProcessMetrics();

        // ── 8. circuitStates ─────────────────────────────────────────────────
        var circuitStates = _circuitMonitor
            .GetAll()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

        // Ensure all 4 known services are always present
        foreach (var svc in new[] { "inventory", "payment", "shipping", "notification" })
            circuitStates.TryAdd(svc, CircuitState.Closed.ToString());

        // ── 9. activeOrder ───────────────────────────────────────────────────
        ActiveOrderDto? activeOrder = null;
        var latestActive = await db.SagaStates
            .Where(s => !TerminalStates.Contains(s.CurrentState))
            .OrderByDescending(s => s.StartedAt)
            .Select(s => new { s.OrderId, s.CorrelationId, s.CurrentState })
            .FirstOrDefaultAsync(ct);

        if (latestActive is not null)
        {
            activeOrder = new ActiveOrderDto
            {
                OrderId       = latestActive.OrderId,
                CorrelationId = latestActive.CorrelationId,
                CurrentState  = latestActive.CurrentState,
            };
        }

        return new StreamMetricsDto
        {
            ActiveSagas     = activeSagas,
            OutboxPending   = outboxPending,
            OrdersPerMinute = Math.Round(ordersPerMinute, 2),
            SagaSuccessRate = sagaSuccessRate,
            P95LatencyMs    = p95Latency,
            DeadLetterCount = deadLetterCount,
            CpuPercent      = cpuPercent,
            MemoryPercent   = memPercent,
            CircuitStates   = circuitStates,
            ActiveOrder     = activeOrder,
            Timestamp       = now,
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static int ComputeP95(List<int> values)
    {
        if (values.Count == 0) return 0;

        values.Sort();
        var index = (int)Math.Ceiling(0.95 * values.Count) - 1;
        return values[Math.Max(0, index)];
    }

    private async Task<int> GetDeadLetterCountAsync()
    {
        try
        {
            var db  = _redis.GetDatabase();
            var val = await db.StringGetAsync("dl:count");
            return val.HasValue && int.TryParse(val, out var n) ? n : 0;
        }
        catch
        {
            // Redis unavailable — return 0, do not crash the snapshot loop
            return 0;
        }
    }

    private (double cpu, double mem) ComputeProcessMetrics()
    {
        try
        {
            var process   = Process.GetCurrentProcess();
            var nowCpu    = process.TotalProcessorTime;
            var nowTime   = DateTime.UtcNow;

            double cpu = 0;
            var elapsed = (nowTime - _lastCpuSample).TotalMilliseconds;
            if (elapsed > 0 && _lastCpuTime != TimeSpan.Zero)
            {
                var cpuUsed   = (nowCpu - _lastCpuTime).TotalMilliseconds;
                var cpuCount  = Environment.ProcessorCount;
                cpu = Math.Round(cpuUsed / (elapsed * cpuCount) * 100.0, 1);
                cpu = Math.Clamp(cpu, 0, 100);
            }

            _lastCpuTime   = nowCpu;
            _lastCpuSample = nowTime;

            // Memory: GC allocated bytes as % of working-set ceiling (1 GB proxy)
            var workingSetBytes = process.WorkingSet64;
            const long ceiling  = 1L * 1024 * 1024 * 1024; // 1 GB reference ceiling
            var mem = Math.Round((double)workingSetBytes / ceiling * 100.0, 1);
            mem = Math.Clamp(mem, 0, 100);

            return (cpu, mem);
        }
        catch
        {
            return (0, 0);
        }
    }
}
