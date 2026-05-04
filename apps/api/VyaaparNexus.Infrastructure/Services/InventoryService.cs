using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VyaaparNexus.Application.Interfaces;

namespace VyaaparNexus.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(ILogger<InventoryService> logger)
    {
        _logger = logger;
    }

    public Task<bool> ReserveInventoryAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stub: Reserving {Quantity} of {ProductId}", quantity, productId);
        return Task.FromResult(true);
    }

    public Task RestoreInventoryAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stub: Restoring {Quantity} of {ProductId}", quantity, productId);
        return Task.CompletedTask;
    }
}
