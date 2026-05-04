using System;
using System.Collections.Generic;

namespace VyaaparNexus.Infrastructure.Messaging.Contracts;

public record OrderCreated
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString();
    public Guid CorrelationId { get; init; }
    public Guid OrderId { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public Guid CustomerId { get; init; }
    public string PaymentMethod { get; init; } = null!;
    public ShippingAddressContract ShippingAddress { get; init; } = null!;
    public List<OrderItemContract> Items { get; init; } = new();
    public string? ForceFailure { get; init; }
}

public record ShippingAddressContract
{
    public string Line1 { get; init; } = null!;
    public string? Line2 { get; init; }
    public string City { get; init; } = null!;
    public string State { get; init; } = null!;
    public string Pincode { get; init; } = null!;
    public string Country { get; init; } = null!;
}

public record OrderItemContract
{
    public Guid ProductId { get; init; }
    public string Sku { get; init; } = null!;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
