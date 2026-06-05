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
        // Change 7 — wrap entire body so factory failures surface as a single clear error
        try
        {
            await _postgres.StartAsync();
            await _rabbit.StartAsync();
            await _redis.StartAsync();

            CreateClient();

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();

            var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "VyaaparNexus.Infrastructure"));
            if (!Directory.Exists(basePath))
            {
                basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "VyaaparNexus.Infrastructure"));
            }
            await DatabaseSeeder.SeedAsync(db, basePath);

            // Change 7 — verify seeding actually populated data; fail fast with a clear message
            var categoryCount = await db.Categories.CountAsync();
            if (categoryCount < 7)
                throw new InvalidOperationException(
                    $"[VyaaparNexusFactory] Seeding failed — categories count is {categoryCount}, expected >= 7. " +
                    $"Seed path resolved to: {basePath}");

            await Task.Delay(3000);
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
            var descriptors = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }
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

