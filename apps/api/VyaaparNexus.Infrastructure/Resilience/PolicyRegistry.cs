using System;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using VyaaparNexus.Domain.Enums;
using VyaaparNexus.Infrastructure.Observability;

namespace VyaaparNexus.Infrastructure.Resilience;

public static class PolicyRegistrySetup
{
    public static IServiceCollection AddResiliencePolicies(this IServiceCollection services)
    {
        services.AddSingleton<CircuitBreakerStateMonitor>();

        services.AddSingleton<IPolicyRegistry<string>>(sp =>
        {
            var registry = new PolicyRegistry();
            var monitor = sp.GetRequiredService<CircuitBreakerStateMonitor>();

            var defaultRetry = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1))
                );

            var paymentCircuitBreaker = Policy<string>
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (outcome, breakDelay) => monitor.SetState("payment", CircuitState.Open),
                    onReset: () => monitor.SetState("payment", CircuitState.Closed),
                    onHalfOpen: () => monitor.SetState("payment", CircuitState.HalfOpen)
                );

            monitor.SetState("payment", CircuitState.Closed);
            monitor.SetState("inventory", CircuitState.Closed);
            monitor.SetState("shipping", CircuitState.Closed);
            monitor.SetState("notification", CircuitState.Closed);

            registry.Add("DefaultRetry", defaultRetry);
            registry.Add("PaymentCircuitBreaker", paymentCircuitBreaker);

            return registry;
        });

        return services;
    }
}
