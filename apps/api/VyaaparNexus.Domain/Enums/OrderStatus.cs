namespace VyaaparNexus.Domain.Enums;

public enum OrderStatus
{
    Submitted,
    InventoryChecking,
    InventoryReserved,
    InventoryFailed,
    PaymentProcessing,
    PaymentProcessed,
    PaymentFailed,
    ShippingDispatching,
    ShippingDispatched,
    NotificationSending,
    OrderCompleted,
    OrderCancelled
}
