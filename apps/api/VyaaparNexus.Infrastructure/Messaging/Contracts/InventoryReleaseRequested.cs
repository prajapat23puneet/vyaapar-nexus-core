using System;
using System.Collections.Generic;

namespace VyaaparNexus.Infrastructure.Messaging.Contracts;

public record InventoryReleaseRequested
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString();
    public Guid CorrelationId { get; init; }
    public Guid OrderId { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public List<InventoryReleaseItemContract> Items { get; init; } = new();
}

public record InventoryReleaseItemContract
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}
