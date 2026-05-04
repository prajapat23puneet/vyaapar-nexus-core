using System;
using System.Collections.Generic;

namespace VyaaparNexus.Infrastructure.Messaging.Contracts;

public record InventoryFailed
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString();
    public Guid CorrelationId { get; init; }
    public Guid OrderId { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public string Reason { get; init; } = null!;
    public List<FailedItemContract> FailedItems { get; init; } = new();
}

public record FailedItemContract
{
    public Guid ProductId { get; init; }
    public int RequestedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
}
