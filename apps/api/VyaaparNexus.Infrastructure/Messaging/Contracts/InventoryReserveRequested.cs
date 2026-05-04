using System;
using System.Collections.Generic;

namespace VyaaparNexus.Infrastructure.Messaging.Contracts;

public record InventoryReserveRequested
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString();
    public Guid CorrelationId { get; init; }
    public Guid OrderId { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public List<InventoryItemContract> Items { get; init; } = new();
}

public record InventoryItemContract
{
    public Guid ProductId { get; init; }
    public string Sku { get; init; } = null!;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
