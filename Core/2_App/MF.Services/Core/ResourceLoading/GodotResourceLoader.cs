using Godot;
using MF.Commons.Core.Enums.Infrastructure;
using MF.Services.Abstractions.Core.ResourceLoading;
using MF.Services.Abstractions.Core.ResourceManagement;
using MF.Services.Bases;
using System.Diagnostics;

namespace MF.Services.ResourceLoading;

/// <summary>
/// Godot 资源加载器实现 - 集成资源缓存服务
/// </summary>
public class GodotResourceLoader : BaseService, IResourceLoader
{
    private readonly IResourceCacheService _cacheService;
    private ResourceLoaderStatistics _statistics = new();
    
    public GodotResourceLoader(IResourceCacheService cacheService)
    {
        _cacheService = cacheService;
    }
    
    public async Task<T?> LoadAsync<T>(string path, CancellationToken cancellationToken = default) where T : class
    {
        return await LoadAsync<T>(path, ResourceCacheStrategy.Default, cancellationToken);
    }
    
    public async Task<T?> LoadAsync<T>(string path, ResourceCacheStrategy cacheStrategy, CancellationToken cancellationToken = default) where T : class
    {
        return await LoadAsync<T>(path, null, null, cacheStrategy, cancellationToken);
    }
    
    public async Task<T?> LoadAsync<T>(string path, Action<float>? progressCallback = null, TimeSpan? minLoadTime = null, ResourceCacheStrategy cacheStrategy = ResourceCacheStrategy.Default, CancellationToken cancellationToken = default) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _statistics.TotalLoads++;
            _statistics.ActiveLoads++;
            
            T? result = null;
            
            // 1. 根据缓存策略尝试从缓存获取
            if (cacheStrategy != ResourceCacheStrategy.NoCache)
            {
                result = await _cacheService.GetAsync<T>(path, cancellationToken);
                if (result != null)
                {
                    _statistics.CacheHits++;
                    _statistics.SuccessfulLoads++;
                    return result;
                }
                else
                {
                    _statistics.CacheMisses++;
                }
            }
            
            // 2. 从磁盘加载资源
            progressCallback?.Invoke(0.1f);
            
            result = await LoadFromDiskAsync<T>(path, progressCallback, cancellationToken);
            
            if (result != null)
            {
                _statistics.SuccessfulLoads++;
                
                // 3. 根据缓存策略存储到缓存
                await _cacheService.SetAsync(path, result, cacheStrategy, cancellationToken: cancellationToken);
            }
            else
            {
                _statistics.FailedLoads++;
            }
            
            // 4. 确保最小加载时间
            if (minLoadTime.HasValue)
            {
                var elapsed = stopwatch.Elapsed;
                if (elapsed < minLoadTime.Value)
                {
                    var remainingTime = minLoadTime.Value - elapsed;
                    await Task.Delay(remainingTime, cancellationToken);
                }
            }
            
            progressCallback?.Invoke(1.0f);
            return result;
        }
        catch (Exception ex)
        {
            _statistics.FailedLoads++;
            _statistics.RecentErrors.Add(new ResourceLoadError
            {
                Path = path,
                Message = ex.Message,
                ExceptionType = ex.GetType().Name,
                Timestamp = DateTime.UtcNow
            });
            
            // 限制错误列表大小
            if (_statistics.RecentErrors.Count > 100)
            {
                _statistics.RecentErrors.RemoveAt(0);
            }
            
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _statistics.ActiveLoads--;
            _statistics.TotalLoadTime = _statistics.TotalLoadTime.Add(stopwatch.Elapsed);
            
            // 更新最快/最慢加载时间
            if (stopwatch.Elapsed < _statistics.FastestLoadTime)
            {
                _statistics.FastestLoadTime = stopwatch.Elapsed;
            }
            if (stopwatch.Elapsed > _statistics.SlowestLoadTime)
            {
                _statistics.SlowestLoadTime = stopwatch.Elapsed;
            }
            
            _statistics.LastUpdated = DateTime.UtcNow;
        }
    }
    
    public T? Load<T>(string path) where T : class
    {
        return Load<T>(path, ResourceCacheStrategy.Default);
    }
    
    public T? Load<T>(string path, ResourceCacheStrategy cacheStrategy) where T : class
    {
        // 同步版本 - 简化实现
        return LoadAsync<T>(path, cacheStrategy).GetAwaiter().GetResult();
    }
    
    public async Task PreloadAsync(string path, CancellationToken cancellationToken = default)
    {
        _statistics.PreloadCount++;
        
        // 检查是否已在缓存中
        if (await _cacheService.ExistsAsync(path, cancellationToken))
        {
            return;
        }
        
        // 预加载资源（假设为 Resource 类型）
        await LoadAsync<Resource>(path, cancellationToken);
    }
    
    public async Task PreloadBatchAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        var tasks = paths.Select(path => PreloadAsync(path, cancellationToken));
        await Task.WhenAll(tasks);
    }
    
    /// <summary>
    /// 获取加载器统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public ResourceLoaderStatistics GetStatistics()
    {
        return _statistics;
    }
    
    /// <summary>
    /// 重置统计信息
    /// </summary>
    public void ResetStatistics()
    {
        _statistics = new ResourceLoaderStatistics();
    }
    
    #region Private Methods
    
    private Task<T?> LoadFromDiskAsync<T>(string path, Action<float>? progressCallback, CancellationToken cancellationToken) where T : class
    {
        // 模拟进度更新
        progressCallback?.Invoke(0.3f);
        
        // 使用 Godot 的资源加载
        var resource = GD.Load(path);
        
        progressCallback?.Invoke(0.8f);
        
        // 类型转换
        if (resource is T typedResource)
        {
            return Task.FromResult<T?>(typedResource);
        }
        
        return Task.FromResult<T?>(null);
    }
    
    #endregion
}