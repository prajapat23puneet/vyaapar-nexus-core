using System;
using System.Collections.Generic;
using VyaaparNexus.Domain.Enums;

namespace VyaaparNexus.Domain.Entities;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CorrelationId { get; set; }
    public Guid CustomerId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; } = 0;
    public decimal ShippingAmount { get; set; } = 0;
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty; // stored as JSONB
    public PaymentMethod PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    
    // Navigation properties
    public Customer? Customer { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
