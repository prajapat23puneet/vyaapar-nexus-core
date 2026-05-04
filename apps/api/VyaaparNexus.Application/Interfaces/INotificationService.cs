using System;
using System.Threading;
using System.Threading.Tasks;

namespace VyaaparNexus.Application.Interfaces;

public interface INotificationService
{
    Task SendAsync(Guid orderId, string channel, string? email, CancellationToken cancellationToken = default);
}
