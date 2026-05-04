using System;
using System.Threading;
using System.Threading.Tasks;

namespace VyaaparNexus.Application.Interfaces;

public interface IInventoryService
{
    Task<bool> ReserveInventoryAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
    Task RestoreInventoryAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
}
