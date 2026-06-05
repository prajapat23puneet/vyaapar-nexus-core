using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VyaaparNexus.Application.Interfaces;
using VyaaparNexus.Infrastructure.Persistence;

namespace VyaaparNexus.API.Middlewares;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private const string ApiKeyHeaderName = "X-Api-Key";

    // Bug 4 fix: cache validated key hashes in Redis so that every authenticated
    // request no longer hits the DB. TTL of 5 minutes balances freshness (a revoked
    // key stops working within 5 minutes) against DB load elimination.
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "apikey:valid:";

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth for health and metrics
        var path = context.Request.Path.Value;
        if (path != null && (path.StartsWith("/health") || path.StartsWith("/metrics") || path.StartsWith("/swagger")))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key was not provided.");
            return;
        }

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(extractedApiKey.ToString()));
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var cacheKey = $"{CacheKeyPrefix}{hashString}";
        var redisService = context.RequestServices.GetRequiredService<IRedisService>();

        // Check Redis first — cache hit means no DB round-trip at all
        var cached = await redisService.GetAsync<bool?>(cacheKey);
        if (cached == true)
        {
            await _next(context);
            return;
        }

        // Cache miss: validate against DB, then cache the result
        var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
        var apiKey = await dbContext.ApiKeys
            .Where(k => k.KeyHash == hashString && k.IsActive)
            .FirstOrDefaultAsync();

        if (apiKey == null)
        {
            _logger.LogWarning("Unauthorized access attempt with invalid API Key.");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized client.");
            return;
        }

        // Cache valid status so subsequent requests skip the DB entirely
        await redisService.SetAsync(cacheKey, true, CacheTtl);

        apiKey.LastUsed = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        await _next(context);
    }
}
