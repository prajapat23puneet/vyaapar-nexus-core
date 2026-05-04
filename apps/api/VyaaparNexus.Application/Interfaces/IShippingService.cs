using System;
using System.Threading;
using System.Threading.Tasks;

namespace VyaaparNexus.Application.Interfaces;

public interface IShippingService
{
    Task<string> DispatchAsync(Guid orderId, object address, CancellationToken cancellationToken = default);
}
