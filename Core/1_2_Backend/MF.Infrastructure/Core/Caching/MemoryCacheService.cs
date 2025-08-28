using System.Collections.Concurrent;
using System.Text.Json;
using MF.Data.Configuration.Resources;
using MF.Infrastructure.Abstractions.Core.Caching;
using MF.Infrastructure.Abstractions.Core.Logging;
using MF.Infrastructure.Bases;
using Microsoft.Extensions.Caching.Memory;

namespace MF.Infrastructure.Core.Caching;

/// <summary>
/// 内存缓存服务实现
/// </summary>
public class MemoryCacheService : BaseInfrastructure, ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IGameLogger<MemoryCacheService> _logger;
    private readonly CacheConfig _config;
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();
    
    // 统计信息
    private long _hits;
    private long _misses;
    private long _sets;
    private long _evictions;
    private long _errors;
    private readonly ConcurrentDictionary<string, long> _evictionReasons = new();
    
    public MemoryCacheService(
        IMemoryCache memoryCache,
        IGameLogger<MemoryCacheService> logger,
        CacheConfig? config = null)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _config = config ?? new CacheConfig();
        
        _logger.LogInformation("MemoryCacheService initialized with config: {Config}", _config);
    }
    
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        CheckDisposed();
        
        try
        {
            if (_memoryCache.TryGetValue(key, out var value))
            {
                Interlocked.Increment(ref _hits);
                _logger.LogDebug("Cache hit: {Key}", key);
                
                // 更新访问时间
                if (_entries.TryGetValue(key, out var entry))
                {
                    entry.LastAccessed = DateTime.UtcNow;
                }
                
                return Task.FromResult(value as T);
            }
            
            Interlocked.Increment(ref _misses);
            _logger.LogDebug("Cache miss: {Key}", key);
            return Task.FromResult<T?>(null);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _errors);
            _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
    }
    
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        CheckDisposed();
        
        try
        {
            var options = new MemoryCacheEntryOptions();
            
            // 设置过期时间
            var actualExpiration = expiration ?? _config.DefaultExpiration;
            if (actualExpiration != TimeSpan.Zero)
            {
                options.AbsoluteExpirationRelativeToNow = actualExpiration;
            }
            
            // 设置缓存大小限制
            options.Size = CalculateSize(value);
            
            // 设置缓存优先级
            options.Priority = CacheItemPriority.Normal;
            
            // 设置过期回调
            options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = OnCacheEvicted,
                State = key
            });
            
            _memoryCache.Set(key, value, options);
            
            // 记录缓存条目信息
            var entry = new CacheEntry
            {
                Key = key,
                Size = options.Size ?? 1,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                ExpiresAt = actualExpiration != TimeSpan.Zero ? DateTime.UtcNow.Add(actualExpiration) : null
            };
            
            _entries.AddOrUpdate(key, entry, (k, v) => entry);
            
            Interlocked.Increment(ref _sets);
            _logger.LogDebug("Cache set: {Key}, Size: {Size}, Expiration: {Expiration}", key, options.Size, actualExpiration);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _errors);
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            throw;
        }
    }
    
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        
        return Task.FromResult(_memoryCache.TryGetValue(key, out _));
    }
    
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        
        try
        {
            _memoryCache.Remove(key);
            _entries.TryRemove(key, out _);
            _logger.LogDebug("Cache removed: {Key}", key);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _errors);
            _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
            throw;
        }
    }
    
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        
        try
        {
            if (_memoryCache is MemoryCache mc)
            {
                mc.Compact(1.0); // 清空所有缓存
            }
            
            _entries.Clear();
            _logger.LogInformation("Cache cleared");
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _errors);
            _logger.LogError(ex, "Error clearing cache");
            throw;
        }
    }
    

    

    
    private void OnCacheEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        Interlocked.Increment(ref _evictions);
        _evictionReasons.AddOrUpdate(reason.ToString(), 1, (k, v) => v + 1);
        
        if (state is string keyString)
        {
            _entries.TryRemove(keyString, out _);
        }
        
        _logger.LogDebug("Cache evicted: {Key}, Reason: {Reason}", key, reason);
    }
    
    private long CalculateSize<T>(T value) where T : class
    {
        try
        {
            // 简单的大小估算
            if (value is string str)
            {
                return str.Length * 2; // Unicode字符
            }
            
            if (value is byte[] bytes)
            {
                return bytes.Length;
            }
            
            // 对于其他对象，使用JSON序列化来估算大小
            var json = JsonSerializer.Serialize(value);
            return json.Length * 2;
        }
        catch
        {
            return 1024; // 默认1KB
        }
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger.LogInformation("Disposing MemoryCacheService");
            
            // 清理缓存
            if (_memoryCache is MemoryCache mc)
            {
                mc.Compact(1.0); // 清空所有缓存
            }
            
            _entries.Clear();
            _evictionReasons.Clear();
            
            _logger.LogInformation("MemoryCacheService disposed");
        }
        
        base.Dispose(disposing);
    }
}

/// <summary>
/// 缓存条目信息
/// </summary>
internal class CacheEntry
{
    public string Key { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessed { get; set; }
    public DateTime? ExpiresAt { get; set; }
}