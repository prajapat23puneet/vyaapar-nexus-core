using System.Collections.Concurrent;
using System.Collections.Generic;
using VyaaparNexus.Domain.Enums;

namespace VyaaparNexus.Infrastructure.Observability;

public class CircuitBreakerStateMonitor
{
    private readonly ConcurrentDictionary<string, CircuitState> _states = new();

    public CircuitBreakerStateMonitor()
    {
        _states["payment"] = CircuitState.Closed;
        _states["inventory"] = CircuitState.Closed;
        _states["shipping"] = CircuitState.Closed;
        _states["notification"] = CircuitState.Closed;
    }

    public void SetState(string service, CircuitState state)
    {
        _states[service] = state;
    }

    public IReadOnlyDictionary<string, CircuitState> GetAll()
    {
        return _states;
    }
}
