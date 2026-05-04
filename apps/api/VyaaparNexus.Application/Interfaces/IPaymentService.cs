using System;
using System.Threading;
using System.Threading.Tasks;

namespace VyaaparNexus.Application.Interfaces;

public interface IPaymentService
{
    Task<string> ProcessPaymentAsync(Guid orderId, decimal amount, string paymentMethod, string? forceFailure = null, CancellationToken cancellationToken = default);
}
