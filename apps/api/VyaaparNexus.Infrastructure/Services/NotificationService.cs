using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VyaaparNexus.Application.Interfaces;

namespace VyaaparNexus.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(Guid orderId, string channel, string? email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stub: Sending notification for order {OrderId} via {Channel} to {Email}", orderId, channel, email);
        return Task.CompletedTask;
    }
}
