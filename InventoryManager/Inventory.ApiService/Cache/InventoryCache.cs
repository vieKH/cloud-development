using System.Text.Json;
using Inventory.ApiService.Entity;
using Inventory.ApiService.Generation;
using Microsoft.Extensions.Caching.Distributed;

namespace Inventory.ApiService.Cache;

public class InventoryCache
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDistributedCache _cache;
    private readonly Generator _generator;
    private readonly ILogger<InventoryCache> _logger;

    public InventoryCache(IDistributedCache cache, Generator generator, ILogger<InventoryCache> logger)
    {
        _cache = cache;
        _generator = generator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Product>> GetAsync(int count, int? seed, CancellationToken ct)
    {
        var cacheKey = $"inventory:count={count}:seed={(seed?.ToString() ?? "null")}";

        var cached = await _cache.GetStringAsync(cacheKey, ct);

        if (cached is not null)
        {
            _logger.LogInformation("Inventory cache HIT {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<List<Product>>(cached, _jsonOptions) ?? new List<Product>();
        }

        _logger.LogInformation("Inventory cache MISS {CacheKey}. Generating {Count} items.", cacheKey, count);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var data = Generator.Generate(count, seed);

        sw.Stop();

        _logger.LogInformation("Generated inventory {Count} items in {ElapsedMs}ms", data.Count, sw.ElapsedMilliseconds);

        var json = JsonSerializer.Serialize(data, _jsonOptions);

        await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }, ct);

        return data;
    }
}