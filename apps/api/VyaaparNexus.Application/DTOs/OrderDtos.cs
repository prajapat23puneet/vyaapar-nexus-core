using VyaaparNexus.Domain.Enums;

namespace VyaaparNexus.Application.DTOs;

public class CreateOrderRequest
{
    public Guid CustomerId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public ShippingAddressDto ShippingAddress { get; set; } = new();
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class ShippingAddressDto
{
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string Country { get; set; } = "India";
}

public class CreateOrderResponse
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string TraceUrl { get; set; } = string.Empty;
    public string SagaUrl { get; set; } = string.Empty;
}

public class OrderListItemDto
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class OrderCustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class OrderDetailItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}

public class OrderDetailDto
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public OrderCustomerDto Customer { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentReference { get; set; }
    public ShippingAddressDto ShippingAddress { get; set; } = new();
    public List<OrderDetailItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}

public class SagaStateDto
{
    public Guid OrderId { get; set; }
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public bool InventoryReserved { get; set; }
    public bool PaymentProcessed { get; set; }
    public bool ShippingDispatched { get; set; }
    public bool NotificationSent { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int? DurationMs { get; set; }
    public string? LastError { get; set; }
}

public class SagaTraceEventDto
{
    public string EventType { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? PreviousState { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public string? Message { get; set; }
    public int? DurationMs { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? Metadata { get; set; }
}

public class SagaTraceDto
{
    public Guid OrderId { get; set; }
    public Guid CorrelationId { get; set; }
    public List<SagaTraceEventDto> Events { get; set; } = new();
}
