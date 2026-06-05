using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using VyaaparNexus.Infrastructure.HostedServices;
using VyaaparNexus.Infrastructure.Persistence;
using VyaaparNexus.Infrastructure.Persistence.Seed;

namespace VyaaparNexus.Tests.Infrastructure;

public class VyaaparNexusFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management-alpine")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        // Wrap entire body so factory failures surface as a single clear error
        // rather than a cascade of confusing assertion failures downstream.
        try
        {
            await _postgres.StartAsync();
            await _rabbit.StartAsync();
            await _redis.StartAsync();

            CreateClient();

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Bug 1 fix: MigrateAsync is intentionally NOT called here.
            // DatabaseSeeder.SeedAsync owns migration as its very first step.
            // Calling it twice here is redundant and could mask migration timing issues.

            // Correct: go up 6 levels from bin/Release/net8.0/ to repo root's apps/api/
            var basePath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..",         // → apps/api/
                "VyaaparNexus.Infrastructure"         // → apps/api/VyaaparNexus.Infrastructure
            ));

            if (!Directory.Exists(basePath))
                throw new DirectoryNotFoundException(
                    $"[VyaaparNexusFactory] Infrastructure project not found at: {basePath}");

            await DatabaseSeeder.SeedAsync(db, basePath);

            // Verify seeding actually populated data; fail fast with a precise message
            // instead of letting every test report "Expected >= 7 but was 0".
            var categoryCount = await db.Categories.CountAsync();
            if (categoryCount < 7)
                throw new InvalidOperationException(
                    $"[VyaaparNexusFactory] Seeding failed — categories count is {categoryCount}, expected >= 7. " +
                    $"Seed path resolved to: {basePath}");

            await Task.Delay(5000);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(
                $"[VyaaparNexusFactory] INIT FAILED: {ex.GetType().Name} — {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_URL"] = _postgres.GetConnectionString(),
                ["DATABASE_MIGRATION_URL"] = _postgres.GetConnectionString(),
                ["RABBITMQ_URL"] = _rabbit.GetConnectionString(),
                ["Redis:ConnectionString"] = _redis.GetConnectionString(),
                ["ASPNETCORE_ENVIRONMENT"] = "Testing",
                ["SEED_API_KEY"] = "vyaaparnexus-demo-key-2026",
                ["FrontendUrl"] = "http://localhost:5173"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // ROOT CAUSE FIX for saga timeouts:
            //
            // The original code removed ALL IHostedService registrations. This killed:
            //   1. MassTransit's bus host → consumers never bind → no messages delivered
            //   2. OutboxPublisherService → OrderCreated never leaves the outbox table
            //      → OrderCreatedConsumer never fires → saga stuck at "Submitted" forever
            //
            // OutboxPublisherService MUST remain running in tests because
            // CreateOrderCommandHandler writes to the outbox table (not IBus.Publish directly).
            // The outbox-to-RabbitMQ relay is what triggers the entire consumer chain.
            //
            // Only MetricsSnapshotService is safe to remove — it has no role in saga flow
            // and only produces background noise in test logs.
            services.Remove(services.Single(d =>
                d.ImplementationType == typeof(MetricsSnapshotService)));
        });
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbit.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }
}
