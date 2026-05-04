using Prometheus;

namespace VyaaparNexus.Infrastructure.Observability;

/// <summary>
/// Central registry for all Prometheus metrics used in this application.
/// Declared as static so any layer can increment without DI overhead.
///
/// Registered metrics (prd-agent-v2.txt Section 13.4):
///   Counters  : orders_submitted_total, orders_completed_total, orders_cancelled_total
///   Histogram : saga_duration_ms (buckets: 100, 250, 500, 1000, 2000, 5000)
///   Gauges    : outbox_pending_gauge, active_sagas_gauge, dead_letter_count_gauge
/// </summary>
public static class MetricsRegistry
{
    // ── Counters ─────────────────────────────────────────────────────────────

    public static readonly Counter OrdersSubmittedTotal =
        Metrics.CreateCounter(
            "orders_submitted_total",
            "Total number of orders submitted via POST /api/orders");

    public static readonly Counter OrdersCompletedTotal =
        Metrics.CreateCounter(
            "orders_completed_total",
            "Total number of orders that reached OrderCompleted");

    public static readonly Counter OrdersCancelledTotal =
        Metrics.CreateCounter(
            "orders_cancelled_total",
            "Total number of orders that reached OrderCancelled");

    // ── Histogram ────────────────────────────────────────────────────────────

    public static readonly Histogram SagaDurationMs =
        Metrics.CreateHistogram(
            "saga_duration_ms",
            "End-to-end saga duration in milliseconds from Submitted to terminal state",
            new HistogramConfiguration
            {
                Buckets = new double[] { 100, 250, 500, 1000, 2000, 5000 },
            });

    // ── Gauges ───────────────────────────────────────────────────────────────

    public static readonly Gauge OutboxPendingGauge =
        Metrics.CreateGauge(
            "outbox_pending_gauge",
            "Current count of outbox_messages rows where published_at IS NULL");

    public static readonly Gauge ActiveSagasGauge =
        Metrics.CreateGauge(
            "active_sagas_gauge",
            "Current count of saga_states rows in non-terminal states");

    public static readonly Gauge DeadLetterCountGauge =
        Metrics.CreateGauge(
            "dead_letter_count_gauge",
            "Current dead-letter queue depth (read from Redis key dl:count)");
}
