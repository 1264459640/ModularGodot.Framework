using MF.Infrastructure.Abstractions.Core.Caching;
using MF.Infrastructure.Abstractions.Core.EventBus;
using MF.Infrastructure.Abstractions.Core.Monitoring;
using MF.Services.Abstractions.Core.ResourceManagement;
using MF.Data.Transient.Infrastructure.Monitoring;
using MF.Events.ResourceManagement;
using MF.Services.Bases;
using System.Collections.Concurrent;
using System.Diagnostics;
using MF.Commons.Core.Enums.Infrastructure;
using MemoryPressureLevel = MF.Events.ResourceManagement.MemoryPressureLevel;

namespace MF.Infrastructure.Core.ResourceManagement;

/// <summary>
/// 资源管理器 - 系统核心协调组件
/// </summary>
public class ResourceManager : BaseService, IResourceCacheService, IResourceMonitorService
{
    private readonly ICacheService _cacheService;
    private readonly IMemoryMonitor _memoryMonitor;
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly IEventBus _eventBus;
    private readonly ResourceSystemConfig _config;
    
    private readonly Timer? _cleanupTimer;
    private readonly object _lockObject = new();
    
    // 统计数据
    private int _hitCount;
    private int _missCount;
    private int _totalRequests;
    private int _errorCount;
    private readonly ConcurrentQueue<TimeSpan> _responseTimes = new();
    private readonly ConcurrentDictionary<string, DateTime> _cacheItems = new();
    
    public ResourceManager(
        ICacheService cacheService,
        IMemoryMonitor memoryMonitor,
        IPerformanceMonitor performanceMonitor,
        IEventBus eventBus,
        ResourceSystemConfig config)
    {
        _cacheService = cacheService;
        _memoryMonitor = memoryMonitor;
        _performanceMonitor = performanceMonitor;
        _eventBus = eventBus;
        _config = config;
        
        // 订阅内存监控事件
        _memoryMonitor.MemoryPressureDetected += OnMemoryPressureDetected;
        
        // 启动定时清理
        if (_config.EnableAutoCleanup)
        {
            _cleanupTimer = new Timer(OnCleanupTimer, null, _config.CleanupInterval, _config.CleanupInterval);
        }
        
        // 启动内存监控
        _memoryMonitor.StartMonitoring();
    }
    
    #region IResourceCacheService Implementation
    
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await _cacheService.GetAsync<T>(key, cancellationToken);
            
            lock (_lockObject)
            {
                _totalRequests++;
                if (result != null)
                {
                    _hitCount++;
                }
                else
                {
                    _missCount++;
                }
            }
            
            _responseTimes.Enqueue(stopwatch.Elapsed);
            
            // 限制响应时间队列大小
            if (_responseTimes.Count > 1000)
            {
                _responseTimes.TryDequeue(out _);
            }
            
            // 记录性能指标
            if (_config.EnablePerformanceMonitoring)
            {
                _performanceMonitor.RecordTimer("resource_cache_get", stopwatch.Elapsed, 
                    new Dictionary<string, string> { { "hit", (result != null).ToString() } });
            }
            
            // 发布资源加载事件
            await _eventBus.PublishAsync(new ResourceLoadEvent(
                key, 
                typeof(T).Name, 
                result != null ? ResourceLoadResult.CacheHit : ResourceLoadResult.CacheMiss, 
                stopwatch.Elapsed, 
                0, 
                result != null), cancellationToken);
            
            return result;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _errorCount);
            
            await _eventBus.PublishAsync(new ResourceLoadEvent(
                key, 
                typeof(T).Name, 
                ResourceLoadResult.Failed, 
                stopwatch.Elapsed, 
                0, 
                false, 
                ex.Message), cancellationToken);
            
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }
    
    public async Task SetAsync<T>(string key, T resource, ResourceCacheStrategy cacheStrategy = ResourceCacheStrategy.Default, CancellationToken cancellationToken = default) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var expiration = GetExpirationFromStrategy(cacheStrategy);
            await _cacheService.SetAsync(key, resource, expiration, cancellationToken);
            
            // 记录缓存项，即便是永久缓存（LongLived）也要追踪
            var expiryTime = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : DateTime.MaxValue;
            _cacheItems.TryAdd(key, expiryTime);
            
            // 记录性能指标
            if (_config.EnablePerformanceMonitoring)
            {
                _performanceMonitor.RecordTimer("resource_cache_set", stopwatch.Elapsed);
                _performanceMonitor.RecordCounter("resource_cache_items_added");
            }
        }
        finally
        {
            stopwatch.Stop();
        }
    }
    
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cacheService.RemoveAsync(key, cancellationToken);
        _cacheItems.TryRemove(key, out _);
        
        if (_config.EnablePerformanceMonitoring)
        {
            _performanceMonitor.RecordCounter("resource_cache_items_removed");
        }
    }
    
    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        await PerformCleanup(CacheCleanupReason.Manual);
    }
    
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _cacheService.ExistsAsync(key, cancellationToken);
    }
    
    #endregion
    
    #region IResourceMonitorService Implementation
    
    public async Task<CacheStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredCount = _cacheItems.Values.Count(expiry => expiry < now);
        
        lock (_lockObject)
        {
            return new CacheStatistics
            {
                HitCount = _hitCount,
                MissCount = _missCount,
                TotalItems = _cacheItems.Count,
                TotalSize = 0, // 需要从缓存服务获取实际大小
                ExpiredItems = expiredCount,
                LastUpdated = DateTime.UtcNow
            };
        }
    }
    
    public async Task<MemoryUsage> GetMemoryUsageAsync(CancellationToken cancellationToken = default)
    {
        var currentUsage = _memoryMonitor.GetCurrentMemoryUsage();
        var maxUsage = _config.MaxMemorySize;
        
        return new MemoryUsage
        {
            CurrentUsage = currentUsage,
            MaxUsage = maxUsage,
            UsagePercentage = maxUsage > 0 ? (double)currentUsage / maxUsage : 0,
            AvailableMemory = Math.Max(0, maxUsage - currentUsage),
            GCCollectionCount = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2),
            LastChecked = DateTime.UtcNow
        };
    }
    
    public async Task<PerformanceReport> GetPerformanceReportAsync(TimeSpan period, CancellationToken cancellationToken = default)
    {
        var cacheStats = await GetCacheStatisticsAsync(cancellationToken);
        var memoryStats = await GetMemoryUsageAsync(cancellationToken);
        
        var responseTimes = _responseTimes.ToArray();
        
        var avgResponseTime = responseTimes.Length > 0 
            ? TimeSpan.FromTicks((long)responseTimes.Average(t => t.Ticks))
            : TimeSpan.Zero;
            
        var fastestTime = responseTimes.Length > 0 
            ? responseTimes.Min()
            : TimeSpan.Zero;
            
        var slowestTime = responseTimes.Length > 0 
            ? responseTimes.Max()
            : TimeSpan.Zero;
        
        lock (_lockObject)
        {
            return new PerformanceReport
            {
                Period = period,
                CacheStats = cacheStats,
                MemoryStats = memoryStats,
                TotalRequests = _totalRequests,
                AverageResponseTime = avgResponseTime,
                FastestResponseTime = fastestTime,
                SlowestResponseTime = slowestTime,
                ErrorCount = _errorCount,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }
    
    public Task<ResourceSystemConfig> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_config);
    }
    
    public Task UpdateConfigurationAsync(ResourceSystemConfig config, CancellationToken cancellationToken = default)
    {
        // 更新配置逻辑
        _config.MaxMemorySize = config.MaxMemorySize;
        _config.DefaultExpiration = config.DefaultExpiration;
        _config.MemoryPressureThreshold = config.MemoryPressureThreshold;
        _config.CleanupInterval = config.CleanupInterval;
        _config.EnableAutoCleanup = config.EnableAutoCleanup;
        _config.EnablePerformanceMonitoring = config.EnablePerformanceMonitoring;
        _config.MaxCacheItems = config.MaxCacheItems;
        
        // 更新内存监控器配置
        _memoryMonitor.MemoryPressureThreshold = config.MemoryPressureThreshold;
        
        return Task.CompletedTask;
    }
    
    #endregion
    
    #region Event Handlers
    
    private async void OnMemoryPressureDetected(long currentUsage)
    {
        var usagePercentage = _config.MaxMemorySize > 0 ? (double)currentUsage / _config.MaxMemorySize : 0;
        var pressureLevel = GetPressureLevel(usagePercentage);
        
        // 发布内存压力事件
        await _eventBus.PublishAsync(new MemoryPressureEvent(currentUsage, usagePercentage, pressureLevel));
        
        // 根据压力级别执行清理
        if (pressureLevel >= MemoryPressureLevel.Warning)
        {
            await PerformCleanup(CacheCleanupReason.MemoryPressure);
        }
    }
    
    private async void OnCleanupTimer(object? state)
    {
        await PerformCleanup(CacheCleanupReason.Scheduled);
    }
    
    #endregion
    
    #region Private Methods
    
    private async Task PerformCleanup(CacheCleanupReason reason)
    {
        try
        {
            var itemsBeforeCleanup = _cacheItems.Count;
            var now = DateTime.UtcNow;
            
            // 清理过期项
            var expiredKeys = _cacheItems
                .Where(kvp => kvp.Value < now)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in expiredKeys)
            {
                await _cacheService.RemoveAsync(key);
                _cacheItems.TryRemove(key, out _);
            }
            
            var itemsAfterCleanup = _cacheItems.Count;
            var memoryFreed = (itemsBeforeCleanup - itemsAfterCleanup) * 1024; // 估算释放的内存
            
            // 发布清理事件
            await _eventBus.PublishAsync(new CacheCleanupEvent(reason, itemsBeforeCleanup, itemsAfterCleanup, memoryFreed));
            
            if (_config.EnablePerformanceMonitoring)
            {
                _performanceMonitor.RecordCounter("cache_cleanup_performed", 1, 
                    new Dictionary<string, string> { { "reason", reason.ToString() } });
                _performanceMonitor.RecordCounter("cache_items_cleaned", expiredKeys.Count);
            }
        }
        catch (Exception)
        {
            // 记录错误
            if (_config.EnablePerformanceMonitoring)
            {
                _performanceMonitor.RecordCounter("cache_cleanup_errors");
            }
            
            // 可以考虑记录日志或发布错误事件
        }
    }
    
    private static MemoryPressureLevel GetPressureLevel(double usagePercentage)
    {
        return usagePercentage switch
        {
            >= 0.9 => MemoryPressureLevel.Critical,
            >= 0.8 => MemoryPressureLevel.Warning,
            _ => MemoryPressureLevel.Normal
        };
    }
    
    private TimeSpan? GetExpirationFromStrategy(ResourceCacheStrategy cacheStrategy)
    {
        return cacheStrategy switch
        {
            ResourceCacheStrategy.NoCache => null,
            ResourceCacheStrategy.Permanent => null, // 永久缓存
            ResourceCacheStrategy.Default => _config.DefaultExpiration,
            ResourceCacheStrategy.ForceCache => _config.DefaultExpiration,
            ResourceCacheStrategy.WeakReference => _config.DefaultExpiration,
            ResourceCacheStrategy.Temporary => TimeSpan.FromMinutes(5), // 临时缓存5分钟
            _ => _config.DefaultExpiration
        };
    }
    
    #endregion
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cleanupTimer?.Dispose();
            _memoryMonitor.MemoryPressureDetected -= OnMemoryPressureDetected;
            _memoryMonitor.StopMonitoring();
        }
        base.Dispose(disposing);
    }
}