using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace VyaaparNexus.Infrastructure.Caching;

public class RedisService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisService> _logger;

    public RedisService(IConnectionMultiplexer mux, ILogger<RedisService> logger)
    {
        _db = mux.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken _ = default)
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty)
            return default;

        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken _ = default)
    {
        var json = JsonSerializer.Serialize(value);
        if (expiry.HasValue)
            await _db.StringSetAsync(key, json, expiry.Value);
        else
            await _db.StringSetAsync(key, json);
        _logger.LogDebug("Redis SET {Key} (ttl={Expiry})", key, expiry);
    }

    public Task DeleteAsync(string key, CancellationToken _ = default)
        => _db.KeyDeleteAsync(key).ContinueWith(_ => { });

    public Task<bool> KeyExistsAsync(string key, CancellationToken _ = default)
        => _db.KeyExistsAsync(key);

    public Task<long> IncrementAsync(string key, long value = 1, CancellationToken _ = default)
        => _db.StringIncrementAsync(key, value);

    public Task<string?> GetRawAsync(string key, CancellationToken _ = default)
        => _db.StringGetAsync(key).ContinueWith(t => (string?)t.Result);
}
