using System;
using System.Collections.Generic;

namespace VyaaparNexus.Application.DTOs;

public class AnalyticsSummaryDto
{
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int ActiveSagas { get; set; }
    public int OutboxPending { get; set; }
    public int DeadLetterCount { get; set; }
    public decimal OrdersPerMinute { get; set; }
    public decimal SagaSuccessRate { get; set; }
    public int P95LatencyMs { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class OrderOverTimeDto
{
    public string Date { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}

public class SagaSuccessRateDto
{
    public string Date { get; set; } = string.Empty;
    public decimal SuccessRate { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
}

public class TopProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}
