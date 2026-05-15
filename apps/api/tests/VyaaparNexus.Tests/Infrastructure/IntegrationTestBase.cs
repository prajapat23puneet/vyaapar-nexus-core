using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Infrastructure.Persistence;

namespace VyaaparNexus.Tests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<VyaaparNexusFactory>, IDisposable
{
    protected readonly HttpClient Client;
    protected readonly AppDbContext Db;
    private readonly IServiceScope _scope;

    protected IntegrationTestBase(VyaaparNexusFactory factory)
    {
        Client = factory.CreateClient();
        Client.DefaultRequestHeaders.Add("X-Api-Key", "vyaaparnexus-demo-key-2026");

        _scope = factory.Services.CreateScope();
        Db = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    protected async Task<SagaStateDto> PollSagaUntilTerminal(
        Guid orderId,
        int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var response = await Client.GetAsync($"/api/v1/orders/{orderId}/saga");
            if (response.IsSuccessStatusCode)
            {
                var saga = await response.Content.ReadFromJsonAsync<SagaStateDto>();
                if (saga?.CurrentState is "OrderCompleted" or "OrderCancelled")
                    return saga;
            }
            await Task.Delay(500);
        }
        throw new TimeoutException($"Saga for order {orderId} did not reach terminal state within {timeoutSeconds}s");
    }

    public void Dispose()
    {
        _scope.Dispose();
        Client.Dispose();
    }
}
