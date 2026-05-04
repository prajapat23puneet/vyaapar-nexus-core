using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VyaaparNexus.Application.Interfaces;

namespace VyaaparNexus.Infrastructure.Services;

public class ShippingService : IShippingService
{
    private readonly ILogger<ShippingService> _logger;

    public ShippingService(ILogger<ShippingService> logger)
    {
        _logger = logger;
    }

    public Task<string> DispatchAsync(Guid orderId, object address, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stub: Dispatching shipping for order {OrderId}", orderId);
        return Task.FromResult($"SHIP-{Guid.NewGuid().ToString("N").Substring(0, 8)}");
    }
}
