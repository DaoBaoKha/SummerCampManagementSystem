using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SummerCampManagementSystem.BLL.Services;

/// <summary>
/// Idempotency service to prevent duplicate webhook processing
/// Caches processed request IDs to ensure each recognition request is only processed once
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Check if a request has already been processed
    /// </summary>
    Task<bool> IsProcessedAsync(string requestId);

    /// <summary>
    /// Mark a request as processed and cache the result
    /// </summary>
    Task MarkAsProcessedAsync(string requestId, object result, TimeSpan ttl);

    /// <summary>
    /// Get cached result for a processed request
    /// </summary>
    Task<T?> GetCachedResultAsync<T>(string requestId) where T : class;

    /// <summary>
    /// Clear cached entry for a specific request
    /// </summary>
    Task ClearAsync(string requestId);
}

public class IdempotencyService : IIdempotencyService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<IdempotencyService> _logger;
    private const string CacheKeyPrefix = "idempotency:";

    public IdempotencyService(IMemoryCache cache, ILogger<IdempotencyService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<bool> IsProcessedAsync(string requestId)
    {
        var cacheKey = $"{CacheKeyPrefix}{requestId}";
        var exists = _cache.TryGetValue(cacheKey, out _);

        _logger.LogDebug(
            "[{RequestId}] Idempotency check: {Status}",
            requestId,
            exists ? "DUPLICATE" : "NEW"
        );

        return Task.FromResult(exists);
    }

    public Task MarkAsProcessedAsync(string requestId, object result, TimeSpan ttl)
    {
        var cacheKey = $"{CacheKeyPrefix}{requestId}";

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, result, cacheOptions);

        _logger.LogInformation(
            "[{RequestId}] Marked as processed, cached for {TTL}",
            requestId,
            ttl
        );

        return Task.CompletedTask;
    }

    public Task<T?> GetCachedResultAsync<T>(string requestId) where T : class
    {
        var cacheKey = $"{CacheKeyPrefix}{requestId}";

        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            _logger.LogDebug("[{RequestId}] Returning cached result", requestId);
            return Task.FromResult(cached as T);
        }

        return Task.FromResult<T?>(null);
    }

    public Task ClearAsync(string requestId)
    {
        var cacheKey = $"{CacheKeyPrefix}{requestId}";
        _cache.Remove(cacheKey);

        _logger.LogDebug("[{RequestId}] Cleared idempotency cache", requestId);

        return Task.CompletedTask;
    }
}
