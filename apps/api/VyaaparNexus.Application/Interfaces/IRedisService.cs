using System.Threading.Tasks;

namespace VyaaparNexus.Application.Interfaces;

public interface IRedisService
{
    System.Threading.Tasks.Task<T?> GetAsync<T>(string key, System.Threading.CancellationToken _ = default);
    System.Threading.Tasks.Task SetAsync<T>(string key, T value, System.TimeSpan? expiry = null, System.Threading.CancellationToken _ = default);
    System.Threading.Tasks.Task DeleteAsync(string key, System.Threading.CancellationToken _ = default);
    System.Threading.Tasks.Task<bool> KeyExistsAsync(string key, System.Threading.CancellationToken _ = default);
    System.Threading.Tasks.Task<long> IncrementAsync(string key, long value = 1, System.Threading.CancellationToken _ = default);
    System.Threading.Tasks.Task<string?> GetRawAsync(string key, System.Threading.CancellationToken _ = default);
}
