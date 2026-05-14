using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Domain.Enums;
using VyaaparNexus.Application.Interfaces;

namespace VyaaparNexus.Application.Queries;

public record GetAnalyticsSummaryQuery : IRequest<AnalyticsSummaryDto>;
public record GetOrdersOverTimeQuery(int Days = 30) : IRequest<List<OrderOverTimeDto>>;
public record GetSagaSuccessRateQuery(int Days = 7) : IRequest<List<SagaSuccessRateDto>>;
public record GetTopProductsQuery(int Limit = 10) : IRequest<List<TopProductDto>>;

public class AnalyticsQueriesHandler : 
    IRequestHandler<GetAnalyticsSummaryQuery, AnalyticsSummaryDto>,
    IRequestHandler<GetOrdersOverTimeQuery, List<OrderOverTimeDto>>,
    IRequestHandler<GetSagaSuccessRateQuery, List<SagaSuccessRateDto>>,
    IRequestHandler<GetTopProductsQuery, List<TopProductDto>>
{
    private readonly IAppDbContext _context;
    private readonly IRedisService _redisService;

    public AnalyticsQueriesHandler(IAppDbContext context, IRedisService redisService)
    {
        _context = context;
        _redisService = redisService;
    }

    public async Task<AnalyticsSummaryDto> Handle(GetAnalyticsSummaryQuery request, CancellationToken cancellationToken)
    {
        var terminalStates = new[] { OrderStatus.OrderCompleted.ToString(), OrderStatus.OrderCancelled.ToString() };

        var totalOrders = await _context.Orders.CountAsync(cancellationToken);
        var completedOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.OrderCompleted, cancellationToken);
        var cancelledOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.OrderCancelled, cancellationToken);
        
        var activeSagas = await _context.SagaStates
            .CountAsync(s => !terminalStates.Contains(s.CurrentState), cancellationToken);
            
        var outboxPending = await _context.OutboxMessages
            .CountAsync(o => o.PublishedAt == null, cancellationToken);
            
        var totalRevenue = await _context.Orders
            .Where(o => o.Status == OrderStatus.OrderCompleted)
            .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;

        // Gap E: Read actual deadLetterCount from Redis
        int deadLetterCount = 0;
        try
        {
            var deadLetterRaw = await _redisService.GetRawAsync("dl:count", cancellationToken);
            if (int.TryParse(deadLetterRaw, out var parsed))
            {
                deadLetterCount = parsed;
            }
        }
        catch
        {
            // Fallback to 0 on Redis failure to avoid crashing the analytics dashboard
            deadLetterCount = 0;
        }
        
        // Orders per minute over trailing 60s
        var lastMinute = DateTimeOffset.UtcNow.AddSeconds(-60);
        var ordersPerMinuteCount = await _context.Orders.CountAsync(o => o.CreatedAt > lastMinute, cancellationToken);
        
        // Saga success rate (completed / completed + cancelled) over trailing window
        var totalCompletedAndCancelled = completedOrders + cancelledOrders;
        decimal sagaSuccessRate = totalCompletedAndCancelled > 0 
            ? (decimal)completedOrders / totalCompletedAndCancelled 
            : 0m;

        // P95 latency placeholder for read path (computed from completed saga duration_ms percentile in memory or DB)
        // Since percentile functions are hard in EF core across providers, we can do it in memory for a limited set or estimate
        var completedDurations = await _context.SagaStates
            .Where(s => s.CurrentState == OrderStatus.OrderCompleted.ToString() && s.DurationMs.HasValue)
            .Select(s => s.DurationMs!.Value)
            .OrderBy(d => d)
            .Take(1000)
            .ToListAsync(cancellationToken);
            
        int p95LatencyMs = 0;
        if (completedDurations.Any())
        {
            int index = (int)Math.Ceiling(0.95 * completedDurations.Count) - 1;
            p95LatencyMs = completedDurations[Math.Max(0, index)];
        }

        return new AnalyticsSummaryDto
        {
            TotalOrders = totalOrders,
            CompletedOrders = completedOrders,
            CancelledOrders = cancelledOrders,
            ActiveSagas = activeSagas,
            OutboxPending = outboxPending,
            DeadLetterCount = deadLetterCount,
            OrdersPerMinute = ordersPerMinuteCount,
            SagaSuccessRate = sagaSuccessRate,
            P95LatencyMs = p95LatencyMs,
            TotalRevenue = totalRevenue
        };
    }

    public async Task<List<OrderOverTimeDto>> Handle(GetOrdersOverTimeQuery request, CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-request.Days);
        
        // SQLite/Postgres grouping difference can be tricky in EF Core, 
        // fallback to memory if needed, but Date is usually supported
        var orders = await _context.Orders
            .Where(o => o.CreatedAt >= since)
            .Select(o => new { o.CreatedAt, o.TotalAmount })
            .ToListAsync(cancellationToken);

        return orders
            .GroupBy(o => o.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new OrderOverTimeDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                OrderCount = g.Count(),
                Revenue = g.Sum(x => x.TotalAmount)
            })
            .ToList();
    }

    public async Task<List<SagaSuccessRateDto>> Handle(GetSagaSuccessRateQuery request, CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.Date.AddDays(-request.Days);

        var orders = await _context.Orders
            .Where(o => o.CreatedAt >= since && 
                        (o.Status == OrderStatus.OrderCompleted || o.Status == OrderStatus.OrderCancelled))
            .Select(o => new { o.CreatedAt, o.Status })
            .ToListAsync(cancellationToken);

        return orders
            .GroupBy(o => o.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => {
                var completed = g.Count(x => x.Status == OrderStatus.OrderCompleted);
                var cancelled = g.Count(x => x.Status == OrderStatus.OrderCancelled);
                var total = completed + cancelled;
                return new SagaSuccessRateDto
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    CompletedCount = completed,
                    CancelledCount = cancelled,
                    SuccessRate = total > 0 ? (decimal)completed / total : 0
                };
            })
            .ToList();
    }

    public async Task<List<TopProductDto>> Handle(GetTopProductsQuery request, CancellationToken cancellationToken)
    {
        return await _context.OrderItems
            .GroupBy(oi => new { oi.ProductId, oi.ProductName })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.LineTotal)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);
    }
}
