using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace VyaaparNexus.Infrastructure.Caching;

public class LockService
{
    private readonly IDatabase _db;
    private readonly ILogger<LockService> _logger;

    // Value stored in the lock key so only the owner can release it.
    private static readonly RedisValue LockToken = Environment.MachineName;

    public LockService(IConnectionMultiplexer mux, ILogger<LockService> logger)
    {
        _db = mux.GetDatabase();
        _logger = logger;
    }

    /// <summary>
    /// Acquires a distributed lock using Redis SET NX with expiry.
    /// Retries every 50 ms until <paramref name="timeout"/> elapses.
    /// </summary>
    public async Task<bool> AcquireAsync(string key, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        // Give the lock a generous TTL (the timeout itself + 5 s safety margin).
        var lockExpiry = timeout.Add(TimeSpan.FromSeconds(5));

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var acquired = await _db.StringSetAsync(key, LockToken, lockExpiry, When.NotExists);
            if (acquired)
            {
                _logger.LogDebug("Acquired lock {Key}", key);
                return true;
            }

            await Task.Delay(50, cancellationToken);
        }

        _logger.LogWarning("Failed to acquire lock {Key} within {Timeout}", key, timeout);
        return false;
    }

    /// <summary>
    /// Releases the lock only if the current process owns it (compare-and-delete via Lua).
    /// </summary>
    public async Task ReleaseAsync(string key, CancellationToken _ = default)
    {
        // Lua script ensures atomic check-and-delete.
        const string script = @"
if redis.call('GET', KEYS[1]) == ARGV[1] then
    return redis.call('DEL', KEYS[1])
else
    return 0
end";

        var result = await _db.ScriptEvaluateAsync(script, new RedisKey[] { key }, new RedisValue[] { LockToken });
        if ((long)result == 0)
            _logger.LogWarning("Lock {Key} was not held by this instance or had already expired", key);
        else
            _logger.LogDebug("Released lock {Key}", key);
    }
}
