using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Application.Interfaces;
using VyaaparNexus.Domain.Enums;

namespace VyaaparNexus.Application.Queries;

public record GetOrdersQuery(int Page = 1, int Size = 20, OrderStatus? Status = null, Guid? CustomerId = null)
    : IRequest<PaginatedList<OrderListItemDto>>;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDetailDto?>;
public record GetOrderSagaQuery(Guid Id) : IRequest<SagaStateDto?>;
public record GetOrderTraceQuery(Guid Id) : IRequest<SagaTraceDto?>;

public class OrderQueriesHandler :
    IRequestHandler<GetOrdersQuery, PaginatedList<OrderListItemDto>>,
    IRequestHandler<GetOrderByIdQuery, OrderDetailDto?>,
    IRequestHandler<GetOrderSagaQuery, SagaStateDto?>,
    IRequestHandler<GetOrderTraceQuery, SagaTraceDto?>
{
    private readonly IAppDbContext _context;

    public OrderQueriesHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<OrderListItemDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);
        if (request.CustomerId.HasValue)
            query = query.Where(o => o.CustomerId == request.CustomerId.Value);

        var total = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(total / (double)request.Size);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.Size)
            .Take(request.Size)
            .Select(o => new OrderListItemDto
            {
                Id = o.Id,
                CorrelationId = o.CorrelationId,
                CustomerName = o.Customer != null ? o.Customer.Name : string.Empty,
                ItemCount = o.Items.Count,
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString(),
                FailureReason = o.FailureReason,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedList<OrderListItemDto>
        {
            Items = items,
            Page = request.Page,
            Size = request.Size,
            Total = total,
            TotalPages = totalPages
        };
    }

    public async Task<OrderDetailDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order == null)
            return null;

        var shippingAddress = JsonSerializer.Deserialize<ShippingAddressDto>(order.ShippingAddress) ?? new ShippingAddressDto();

        return new OrderDetailDto
        {
            Id = order.Id,
            CorrelationId = order.CorrelationId,
            Customer = new OrderCustomerDto
            {
                Id = order.CustomerId,
                Name = order.Customer != null ? order.Customer.Name : string.Empty,
                Email = order.Customer != null ? order.Customer.Email : string.Empty
            },
            Status = order.Status.ToString(),
            PaymentMethod = order.PaymentMethod.ToString(),
            PaymentReference = order.PaymentReference,
            ShippingAddress = shippingAddress,
            Items = order.Items
                .Select(i => new OrderDetailItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Sku = i.Sku,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    LineTotal = i.LineTotal
                }).ToList(),
            Subtotal = order.Subtotal,
            TaxAmount = order.TaxAmount,
            ShippingAmount = order.ShippingAmount,
            TotalAmount = order.TotalAmount,
            FailureReason = order.FailureReason,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            CompletedAt = order.CompletedAt,
            CancelledAt = order.CancelledAt
        };
    }

    public async Task<SagaStateDto?> Handle(GetOrderSagaQuery request, CancellationToken cancellationToken)
    {
        var saga = await _context.SagaStates
            .FirstOrDefaultAsync(s => s.OrderId == request.Id, cancellationToken);

        if (saga == null)
            return null;

        return new SagaStateDto
        {
            OrderId = saga.OrderId,
            CorrelationId = saga.CorrelationId,
            CurrentState = saga.CurrentState,
            InventoryReserved = saga.InventoryReserved,
            PaymentProcessed = saga.PaymentProcessed,
            ShippingDispatched = saga.ShippingDispatched,
            NotificationSent = saga.NotificationSent,
            StartedAt = saga.StartedAt,
            CompletedAt = saga.CompletedAt,
            DurationMs = saga.DurationMs,
            LastError = saga.LastError
        };
    }

    public async Task<SagaTraceDto?> Handle(GetOrderTraceQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Where(o => o.Id == request.Id)
            .Select(o => new { o.Id, o.CorrelationId })
            .FirstOrDefaultAsync(cancellationToken);

        if (order == null)
            return null;

        var events = await _context.SagaEventLogs
            .Where(e => e.OrderId == request.Id)
            .OrderBy(e => e.CreatedAt)
            .Select(e => new SagaTraceEventDto
            {
                EventType = e.EventType,
                ServiceName = e.ServiceName,
                PreviousState = e.PreviousState,
                CurrentState = e.CurrentState,
                Message = e.Message,
                DurationMs = e.DurationMs,
                CreatedAt = e.CreatedAt,
                Metadata = e.Metadata
            })
            .ToListAsync(cancellationToken);

        return new SagaTraceDto
        {
            OrderId = order.Id,
            CorrelationId = order.CorrelationId,
            Events = events
        };
    }
}
