using MassTransit;
using VyaaparNexus.Contracts;

namespace VyaaparNexus.Worker;

public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderCreated> context)
    {
        _logger.LogInformation("Order Received: {OrderId}", context.Message.OrderId);
        return Task.CompletedTask;
    }
}
