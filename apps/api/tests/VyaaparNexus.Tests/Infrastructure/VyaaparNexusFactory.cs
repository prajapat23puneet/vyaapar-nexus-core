using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        await _postgres.StartAsync();
        await _rabbit.StartAsync();
        await _redis.StartAsync();

        // Ensure App is built so services are available to run migrations
        _ = Server;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        
        // Use DatabaseSeeder logic here. It's normally a static class in this setup (from Program.cs view)
        // Wait, from Program.cs: `await DatabaseSeeder.SeedAsync(context, basePath);`
        // We can just call it here.
        var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "VyaaparNexus.Infrastructure"));
        // Check if we need a valid basePath for seed data. In tests, we might need to point to where seed files are.
        // The VyaaparNexusFactory runs in tests bin folder.
        // Let's copy SeedData to tests or point correctly. 
        // A better approach is to rely on the seeder if it creates data without files or find the correct path.
        if (!Directory.Exists(basePath))
        {
            basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "VyaaparNexus.Infrastructure"));
        }
        await DatabaseSeeder.SeedAsync(db, basePath);
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
                ["REDIS_URL"] = _redis.GetConnectionString(),
                ["ASPNETCORE_ENVIRONMENT"] = "Testing",
                ["SEED_API_KEY"] = "vyaaparnexus-demo-key-2026",
                ["FrontendUrl"] = "http://localhost:5173"
            });
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
