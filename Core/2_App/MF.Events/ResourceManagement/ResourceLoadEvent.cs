using MF.Infrastructure.Abstractions.Core.EventBus;

namespace MF.Events.ResourceManagement;

/// <summary>
/// 资源加载事件
/// </summary>
public class ResourceLoadEvent : EventBase
{
    /// <summary>
    /// 资源路径
    /// </summary>
    public string ResourcePath { get; }
    
    /// <summary>
    /// 资源类型
    /// </summary>
    public string ResourceType { get; }
    
    /// <summary>
    /// 加载结果
    /// </summary>
    public ResourceLoadResult Result { get; }
    
    /// <summary>
    /// 加载耗时
    /// </summary>
    public TimeSpan LoadTime { get; }
    
    /// <summary>
    /// 资源大小（字节）
    /// </summary>
    public long ResourceSize { get; }
    
    /// <summary>
    /// 是否来自缓存
    /// </summary>
    public bool FromCache { get; }
    
    /// <summary>
    /// 错误信息（如果加载失败）
    /// </summary>
    public string? ErrorMessage { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="resourcePath">资源路径</param>
    /// <param name="resourceType">资源类型</param>
    /// <param name="result">加载结果</param>
    /// <param name="loadTime">加载耗时</param>
    /// <param name="resourceSize">资源大小</param>
    /// <param name="fromCache">是否来自缓存</param>
    /// <param name="errorMessage">错误信息</param>
    public ResourceLoadEvent(
        string resourcePath, 
        string resourceType, 
        ResourceLoadResult result, 
        TimeSpan loadTime, 
        long resourceSize = 0, 
        bool fromCache = false, 
        string? errorMessage = null)
        : base("ResourceLoader")
    {
        ResourcePath = resourcePath;
        ResourceType = resourceType;
        Result = result;
        LoadTime = loadTime;
        ResourceSize = resourceSize;
        FromCache = fromCache;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// 资源加载结果
/// </summary>
public enum ResourceLoadResult
{
    /// <summary>
    /// 成功
    /// </summary>
    Success,
    
    /// <summary>
    /// 失败
    /// </summary>
    Failed,
    
    /// <summary>
    /// 缓存命中
    /// </summary>
    CacheHit,
    
    /// <summary>
    /// 缓存未命中
    /// </summary>
    CacheMiss
}