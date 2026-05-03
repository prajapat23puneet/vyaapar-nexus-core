namespace VyaaparNexus.Domain.Enums;

public enum SagaEventType
{
    OrderSubmitted,
    InventoryReserved,
    InventoryFailed,
    PaymentProcessed,
    PaymentFailed,
    InventoryReleased,
    ShippingDispatched,
    NotificationSent,
    OrderCompleted,
    OrderCancelled
}
