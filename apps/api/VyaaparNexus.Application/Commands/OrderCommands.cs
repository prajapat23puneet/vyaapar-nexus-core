// using System.Text.Json;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using VyaaparNexus.Application.DTOs;
// using VyaaparNexus.Application.Interfaces;
// using VyaaparNexus.Domain.Entities;
// using VyaaparNexus.Domain.Enums;
// using VyaaparNexus.Infrastructure.Messaging.Contracts;
// using VyaaparNexus.Infrastructure.Observability;
// using VyaaparNexus.Infrastructure.Persistence;

// namespace VyaaparNexus.Application.Commands;

// public record CreateOrderCommand(CreateOrderRequest Request, string? ForceFailure) : IRequest<CreateOrderResponse>;
// public record CreateDemoOrderCommand(string? ForceFailure) : IRequest<CreateOrderResponse>;

// public class OrderCommandsHandler :
//     IRequestHandler<CreateOrderCommand, CreateOrderResponse>,
//     IRequestHandler<CreateDemoOrderCommand, CreateOrderResponse>
// {
//     private readonly IAppDbContext _context;
//     private readonly IMediator _mediator;

//     public OrderCommandsHandler(IAppDbContext context, IMediator mediator)
//     {
//         _context = context;
//         _mediator = mediator;
//     }

//     public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
//     {
//         var dbContext = _context as AppDbContext
//             ?? throw new InvalidOperationException("AppDbContext transaction support is required.");

//         var input = request.Request;
//         if (input.Items.Count == 0)
//             throw new ArgumentException("At least one order item is required.");

//         var customer = await _context.Customers
//             .FirstOrDefaultAsync(c => c.Id == input.CustomerId, cancellationToken);
//         if (customer == null)
//             throw new ArgumentException("Customer not found.");

//         var groupedItems = input.Items
//             .GroupBy(i => i.ProductId)
//             .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
//             .ToList();

//         if (groupedItems.Any(x => x.Quantity <= 0))
//             throw new ArgumentException("Item quantity must be greater than zero.");

//         var productIds = groupedItems.Select(x => x.ProductId).ToList();
//         var products = await _context.Products
//             .Where(p => productIds.Contains(p.Id))
//             .ToListAsync(cancellationToken);

//         if (products.Count != productIds.Count)
//             throw new ArgumentException("One or more products were not found.");
//         if (products.Any(p => !p.IsActive))
//             throw new InvalidOperationException("One or more products are inactive.");

//         var subtotal = groupedItems.Sum(item =>
//         {
//             var product = products.First(p => p.Id == item.ProductId);
//             return product.UnitPrice * item.Quantity;
//         });
//         var taxAmount = 0m;
//         var shippingAmount = 0m;
//         var totalAmount = subtotal + taxAmount + shippingAmount;

//         var correlationId = Guid.NewGuid();
//         var now = DateTimeOffset.UtcNow;

//         var shippingAddress = new ShippingAddressDto
//         {
//             Line1 = input.ShippingAddress.Line1,
//             Line2 = input.ShippingAddress.Line2,
//             City = input.ShippingAddress.City,
//             State = input.ShippingAddress.State,
//             Pincode = input.ShippingAddress.Pincode,
//             Country = input.ShippingAddress.Country
//         };

//         await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

//         var order = new Order
//         {
//             CorrelationId = correlationId,
//             CustomerId = customer.Id,
//             Status = OrderStatus.Submitted,
//             Subtotal = subtotal,
//             TaxAmount = taxAmount,
//             ShippingAmount = shippingAmount,
//             TotalAmount = totalAmount,
//             ShippingAddress = JsonSerializer.Serialize(shippingAddress),
//             PaymentMethod = input.PaymentMethod,
//             CreatedAt = now,
//             UpdatedAt = now
//         };

//         _context.Orders.Add(order);

//         var orderItems = groupedItems.Select(item =>
//         {
//             var product = products.First(p => p.Id == item.ProductId);
//             return new OrderItem
//             {
//                 OrderId = order.Id,
//                 ProductId = product.Id,
//                 ProductName = product.Name,
//                 Sku = product.Sku,
//                 UnitPrice = product.UnitPrice,
//                 Quantity = item.Quantity,
//                 LineTotal = product.UnitPrice * item.Quantity
//             };
//         }).ToList();

//         _context.OrderItems.AddRange(orderItems);

//         _context.SagaStates.Add(new SagaState
//         {
//             OrderId = order.Id,
//             CorrelationId = correlationId,
//             CurrentState = OrderStatus.Submitted.ToString(),
//             StartedAt = now
//         });

//         _context.SagaEventLogs.Add(new SagaEventLog
//         {
//             CorrelationId = correlationId,
//             OrderId = order.Id,
//             EventType = "OrderSubmitted",
//             ServiceName = "API",
//             PreviousState = null,
//             CurrentState = OrderStatus.Submitted.ToString(),
//             Message = "Order persisted and outbox message written",
//             CreatedAt = now
//         });

//         var orderCreated = new OrderCreated
//         {
//             MessageId = Guid.NewGuid().ToString("N"),
//             CorrelationId = correlationId,
//             OrderId = order.Id,
//             OccurredAt = DateTime.UtcNow,
//             CustomerId = customer.Id,
//             PaymentMethod = input.PaymentMethod.ToString(),
//             ShippingAddress = new ShippingAddressContract
//             {
//                 Line1 = shippingAddress.Line1,
//                 Line2 = shippingAddress.Line2,
//                 City = shippingAddress.City,
//                 State = shippingAddress.State,
//                 Pincode = shippingAddress.Pincode,
//                 Country = shippingAddress.Country
//             },
//             ForceFailure = request.ForceFailure,
//             Items = orderItems.Select(i => new OrderItemContract
//             {
//                 ProductId = i.ProductId,
//                 Sku = i.Sku,
//                 Quantity = i.Quantity,
//                 UnitPrice = i.UnitPrice
//             }).ToList()
//         };

//         _context.OutboxMessages.Add(new OutboxMessage
//         {
//             CorrelationId = correlationId,
//             MessageType = nameof(OrderCreated),
//             Payload = JsonSerializer.Serialize(orderCreated),
//             Exchange = "order.events",
//             RoutingKey = "order.created",
//             CreatedAt = now
//         });

//         await _context.SaveChangesAsync(cancellationToken);
//         await tx.CommitAsync(cancellationToken);

//         MetricsRegistry.OrdersSubmittedTotal.Inc();

//         return new CreateOrderResponse
//         {
//             Id = order.Id,
//             CorrelationId = order.CorrelationId,
//             Status = order.Status.ToString(),
//             Subtotal = order.Subtotal,
//             TaxAmount = order.TaxAmount,
//             ShippingAmount = order.ShippingAmount,
//             TotalAmount = order.TotalAmount,
//             CreatedAt = order.CreatedAt,
//             TraceUrl = $"/api/v1/orders/{order.Id}/trace",
//             SagaUrl = $"/api/v1/orders/{order.Id}/saga"
//         };
//     }

//     public async Task<CreateOrderResponse> Handle(CreateDemoOrderCommand request, CancellationToken cancellationToken)
//     {
//         var customerIds = await _context.Customers
//             .Select(c => c.Id)
//             .ToListAsync(cancellationToken);
//         if (customerIds.Count == 0)
//             throw new InvalidOperationException("No customers available for demo order.");

//         var products = await _context.Products
//             .Where(p => p.IsActive)
//             .ToListAsync(cancellationToken);
//         if (products.Count == 0)
//             throw new InvalidOperationException("No active products available for demo order.");

//         var chosenCustomerId = customerIds[Random.Shared.Next(customerIds.Count)];
//         var chosenCustomer = await _context.Customers.FirstAsync(c => c.Id == chosenCustomerId, cancellationToken);

//         var take = Random.Shared.Next(1, Math.Min(3, products.Count) + 1);
//         var chosenProducts = products
//             .OrderBy(_ => Guid.NewGuid())
//             .Take(take)
//             .ToList();

//         var createRequest = new CreateOrderRequest
//         {
//             CustomerId = chosenCustomer.Id,
//             PaymentMethod = new[] { PaymentMethod.UPI, PaymentMethod.Card, PaymentMethod.Wallet }[Random.Shared.Next(3)],
//             ShippingAddress = new ShippingAddressDto
//             {
//                 Line1 = chosenCustomer.AddressLine1,
//                 Line2 = chosenCustomer.AddressLine2,
//                 City = chosenCustomer.City,
//                 State = chosenCustomer.State,
//                 Pincode = chosenCustomer.Pincode,
//                 Country = chosenCustomer.Country
//             },
//             Items = chosenProducts.Select(p => new CreateOrderItemRequest
//             {
//                 ProductId = p.Id,
//                 Quantity = Random.Shared.Next(1, 4)
//             }).ToList()
//         };

//         return await _mediator.Send(new CreateOrderCommand(createRequest, request.ForceFailure), cancellationToken);
//     }
// }

using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Application.Interfaces;
using VyaaparNexus.Domain.Entities;
using VyaaparNexus.Domain.Enums;

namespace VyaaparNexus.Application.Commands;

public record CreateOrderCommand(CreateOrderRequest Request, string? ForceFailure) : IRequest<CreateOrderResponse>;
public record CreateDemoOrderCommand(string? ForceFailure) : IRequest<CreateOrderResponse>;

public class OrderCommandsHandler :
    IRequestHandler<CreateOrderCommand, CreateOrderResponse>,
    IRequestHandler<CreateDemoOrderCommand, CreateOrderResponse>
{
    private readonly IAppDbContext _context;
    private readonly IMediator _mediator;

    public OrderCommandsHandler(IAppDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var input = request.Request;
        if (input.Items.Count == 0)
            throw new ArgumentException("At least one order item is required.");

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == input.CustomerId, cancellationToken);
        if (customer == null)
            throw new ArgumentException("Customer not found.");

        var groupedItems = input.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        if (groupedItems.Any(x => x.Quantity <= 0))
            throw new ArgumentException("Item quantity must be greater than zero.");

        var productIds = groupedItems.Select(x => x.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Count || products.Any(p => !p.IsActive))
            throw new ArgumentException("One or more products were not found or are inactive.");

        var subtotal = groupedItems.Sum(item =>
        {
            var product = products.First(p => p.Id == item.ProductId);
            return product.UnitPrice * item.Quantity;
        });
        var taxAmount = 0m;
        var shippingAmount = 0m;
        var totalAmount = subtotal + taxAmount + shippingAmount;

        var correlationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var shippingAddress = new ShippingAddressDto
        {
            Line1 = input.ShippingAddress.Line1,
            Line2 = input.ShippingAddress.Line2,
            City = input.ShippingAddress.City,
            State = input.ShippingAddress.State,
            Pincode = input.ShippingAddress.Pincode,
            Country = input.ShippingAddress.Country
        };

        var order = new Order
        {
            CorrelationId = correlationId,
            CustomerId = customer.Id,
            Status = OrderStatus.Submitted,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            ShippingAmount = shippingAmount,
            TotalAmount = totalAmount,
            ShippingAddress = JsonSerializer.Serialize(shippingAddress),
            PaymentMethod = input.PaymentMethod,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Orders.Add(order);

        var orderItems = groupedItems.Select(item =>
        {
            var product = products.First(p => p.Id == item.ProductId);
            return new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                Sku = product.Sku,
                UnitPrice = product.UnitPrice,
                Quantity = item.Quantity,
                LineTotal = product.UnitPrice * item.Quantity
            };
        }).ToList();

        _context.OrderItems.AddRange(orderItems);

        _context.SagaStates.Add(new SagaState
        {
            OrderId = order.Id,
            CorrelationId = correlationId,
            CurrentState = OrderStatus.Submitted.ToString(),
            StartedAt = now
        });

        _context.SagaEventLogs.Add(new SagaEventLog
        {
            CorrelationId = correlationId,
            OrderId = order.Id,
            EventType = "OrderSubmitted",
            ServiceName = "API",
            CurrentState = OrderStatus.Submitted.ToString(),
            Message = "Order persisted and outbox message written",
            CreatedAt = now
        });

        // Use anonymous object matching MassTransit contract shape to avoid Infrastructure reference
        var orderCreatedMessage = new
        {
            MessageId = Guid.NewGuid().ToString("N"),
            CorrelationId = correlationId,
            OrderId = order.Id,
            OccurredAt = DateTime.UtcNow,
            CustomerId = customer.Id,
            PaymentMethod = input.PaymentMethod.ToString(),
            ShippingAddress = new 
            {
                Line1 = shippingAddress.Line1,
                Line2 = shippingAddress.Line2,
                City = shippingAddress.City,
                State = shippingAddress.State,
                Pincode = shippingAddress.Pincode,
                Country = shippingAddress.Country
            },
            ForceFailure = request.ForceFailure,
            Items = orderItems.Select(i => new 
            {
                ProductId = i.ProductId,
                Sku = i.Sku,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        _context.OutboxMessages.Add(new OutboxMessage
        {
            CorrelationId = correlationId,
            MessageType = "OrderCreated",
            Payload = JsonSerializer.Serialize(orderCreatedMessage),
            Exchange = "order.events",
            RoutingKey = "order.created",
            CreatedAt = now
        });

        // EF automatically wraps this single call in a database transaction
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateOrderResponse
        {
            Id = order.Id,
            CorrelationId = order.CorrelationId,
            Status = order.Status.ToString(),
            Subtotal = order.Subtotal,
            TaxAmount = order.TaxAmount,
            ShippingAmount = order.ShippingAmount,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            TraceUrl = $"/api/v1/orders/{order.Id}/trace",
            SagaUrl = $"/api/v1/orders/{order.Id}/saga"
        };
    }

    public async Task<CreateOrderResponse> Handle(CreateDemoOrderCommand request, CancellationToken cancellationToken)
    {
        var customerIds = await _context.Customers
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        if (customerIds.Count == 0)
            throw new InvalidOperationException("No customers available for demo order.");

        var products = await _context.Products
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
        if (products.Count == 0)
            throw new InvalidOperationException("No active products available for demo order.");

        var chosenCustomerId = customerIds[Random.Shared.Next(customerIds.Count)];
        var chosenCustomer = await _context.Customers.FirstAsync(c => c.Id == chosenCustomerId, cancellationToken);

        var take = Random.Shared.Next(1, Math.Min(3, products.Count) + 1);
        var chosenProducts = products
            .OrderBy(_ => Guid.NewGuid())
            .Take(take)
            .ToList();

        var createRequest = new CreateOrderRequest
        {
            CustomerId = chosenCustomer.Id,
            PaymentMethod = new[] { PaymentMethod.UPI, PaymentMethod.Card, PaymentMethod.Wallet }[Random.Shared.Next(3)],
            ShippingAddress = new ShippingAddressDto
            {
                Line1 = chosenCustomer.AddressLine1,
                Line2 = chosenCustomer.AddressLine2,
                City = chosenCustomer.City,
                State = chosenCustomer.State,
                Pincode = chosenCustomer.Pincode,
                Country = chosenCustomer.Country
            },
            Items = chosenProducts.Select(p => new CreateOrderItemRequest
            {
                ProductId = p.Id,
                Quantity = Random.Shared.Next(1, 4)
            }).ToList()
        };

        return await _mediator.Send(new CreateOrderCommand(createRequest, request.ForceFailure), cancellationToken);
    }
}