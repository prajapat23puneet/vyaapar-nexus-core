using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using VyaaparNexus.Application.DTOs;
using VyaaparNexus.Infrastructure.Persistence;

namespace VyaaparNexus.Tests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<VyaaparNexusFactory>, IDisposable
{
    protected readonly HttpClient Client;
    public VyaaparNexusFactory Factory { get; }

    protected IntegrationTestBase(VyaaparNexusFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        Client.DefaultRequestHeaders.Add("X-Api-Key", "vyaaparnexus-demo-key-2026");
    }

    protected AppDbContext GetDb()
    {
        // Creates a new scope each time this is called to avoid concurrency and stale data issues
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    protected async Task<SagaStateDto> PollSagaUntilTerminal(
        Guid orderId,
        int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        var start = DateTime.UtcNow;
        var lastPrint = DateTime.UtcNow;
        SagaStateDto? saga = null;

        while (DateTime.UtcNow < deadline)
        {
            var response = await Client.GetAsync($"/api/v1/orders/{orderId}/saga");
            if (response.IsSuccessStatusCode)
            {
                saga = await response.Content.ReadFromJsonAsync<SagaStateDto>();
                if (saga?.CurrentState is "OrderCompleted" or "OrderCancelled")
                    return saga;
            }

            // Change 8 — print progress every 5 seconds so the inline log shows how far the saga got
            var now = DateTime.UtcNow;
            if ((now - lastPrint).TotalSeconds >= 5)
            {
                var elapsed = now - start;
                Console.WriteLine(
                    $"[PollSaga] orderId={orderId} elapsed={elapsed.TotalSeconds:F0}s state={saga?.CurrentState ?? "pending"}");
                lastPrint = now;
            }

            await Task.Delay(500);
        }
        throw new TimeoutException($"Saga for order {orderId} did not reach terminal state within {timeoutSeconds}s");
    }

    public void Dispose()
    {
        Client.Dispose();
    }
}
