

using MF.Commons.Core.Enums.Infrastructure;

namespace MF.Infrastructure.Abstractions.Core.ResourceLoading;

/// <summary>
/// 资源加载器接口 - Standard级别
/// </summary>
public interface IResourceLoader
{
    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源实例</returns>
    Task<T?> LoadAsync<T>(string path, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// 异步加载资源（带缓存策略）
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <param name="cacheStrategy">缓存策略</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源实例</returns>
    Task<T?> LoadAsync<T>(string path, ResourceCacheStrategy cacheStrategy, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// 异步加载资源（带进度回调和最小加载时间）
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <param name="progressCallback">进度回调函数</param>
    /// <param name="minLoadTime">最小加载时间</param>
    /// <param name="cacheStrategy">缓存策略</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源实例</returns>
    Task<T?> LoadAsync<T>(string path, Action<float>? progressCallback = null, TimeSpan? minLoadTime = null, ResourceCacheStrategy cacheStrategy = ResourceCacheStrategy.Default, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// 同步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <returns>资源实例</returns>
    T? Load<T>(string path) where T : class;
    
    /// <summary>
    /// 同步加载资源（带缓存策略）
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <param name="cacheStrategy">缓存策略</param>
    /// <returns>资源实例</returns>
    T? Load<T>(string path, ResourceCacheStrategy cacheStrategy) where T : class;
    

    
    /// <summary>
    /// 预加载资源
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>预加载任务</returns>
    Task PreloadAsync(string path, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 批量预加载资源
    /// </summary>
    /// <param name="paths">资源路径列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>预加载任务</returns>
    Task PreloadBatchAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default);
}