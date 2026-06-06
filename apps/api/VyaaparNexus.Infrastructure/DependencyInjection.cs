using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using VyaaparNexus.Application.Interfaces;
using VyaaparNexus.Infrastructure.Caching;
using VyaaparNexus.Infrastructure.Messaging.Consumers;
using VyaaparNexus.Infrastructure.Observability;
using VyaaparNexus.Infrastructure.Persistence;
using VyaaparNexus.Infrastructure.Resilience;
using VyaaparNexus.Infrastructure.Services;

namespace VyaaparNexus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Database ─────────────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration["DATABASE_URL"] 
                ?? Environment.GetEnvironmentVariable("DATABASE_URL") 
                ?? throw new InvalidOperationException("Database connection string not found.")));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ── MassTransit + RabbitMQ ────────────────────────────────────────────────
        // No consumers registered yet (Phase 5+). Configuration skeleton only.
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.AddConsumer<OrderCreatedConsumer>();
            x.AddConsumer<PaymentProcessRequestedConsumer>();
            x.AddConsumer<InventoryReleaseRequestedConsumer>();
            x.AddConsumer<ShippingDispatchRequestedConsumer>();
            x.AddConsumer<NotificationSendRequestedConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                var rabbitUrl = configuration["RabbitMQ:Host"] 
                    ?? Environment.GetEnvironmentVariable("RABBITMQ_URL")
                    ?? throw new InvalidOperationException("RabbitMQ connection string not found.");

                cfg.Host(new Uri(rabbitUrl));

                // Consumers will be added in Phase 5/6.
                cfg.ConfigureEndpoints(ctx);
            });
        });

        // ── Redis ─────────────────────────────────────────────────────────────────
        var redisConnectionString = configuration["Redis:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("REDIS_URL")
            ?? throw new InvalidOperationException("Redis connection string not found.");

        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<IRedisService, RedisService>();
        services.AddSingleton<RedisService>(); // Keep explicit registration in case classes still inject it directly
        services.AddSingleton<LockService>();

        // ── Stub Domain Services ──────────────────────────────────────────────────
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IPaymentService,   PaymentService>();
        services.AddScoped<IShippingService,  ShippingService>();
        services.AddScoped<INotificationService, NotificationService>();

        // ── Resilience (Polly) + Circuit-Breaker Monitor ──────────────────────────
        // Registers CircuitBreakerStateMonitor (singleton) and IReadOnlyPolicyRegistry<string>
        services.AddResiliencePolicies();

        // ── Phase 5: Observability singletons ─────────────────────────────────────
        // StreamSnapshotStore is a singleton so the SSE controller and
        // MetricsSnapshotService both share the same instance, and it also
        // implements ILogEventSink so Serilog can be wired to it in Program.cs.
        services.AddSingleton<StreamSnapshotStore>();

        // ── Phase 5: Hosted services ──────────────────────────────────────────────
        services.AddHostedService<HostedServices.OutboxPublisherService>();
        services.AddHostedService<HostedServices.MetricsSnapshotService>();

        return services;
    }
}
