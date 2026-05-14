using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VyaaparNexus.Application.Interfaces;

namespace VyaaparNexus.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    public Task<string> ProcessPaymentAsync(Guid orderId, decimal amount, string paymentMethod, string? forceFailure = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stub: Processing payment for order {OrderId}, amount {Amount}", orderId, amount);
        
        if (string.Equals(forceFailure, "payment", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Stub: Payment forced to fail for order {OrderId}", orderId);
            throw new InvalidOperationException("Simulated payment failure (forced)");
        }
        
        return Task.FromResult($"PAY-{orderId:N}-{DateTime.UtcNow:yyyyMMddHHmmss}");
    }
}
