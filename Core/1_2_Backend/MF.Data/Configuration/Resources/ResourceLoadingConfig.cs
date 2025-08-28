using MF.Commons.Core.Enums.Infrastructure;

namespace MF.Data.Configuration.Resources;

/// <summary>
/// 资源加载配置
/// </summary>
public class ResourceLoadingConfig
{
    /// <summary>
    /// 最大并发加载数
    /// </summary>
    public int MaxConcurrentLoads { get; set; } = 4;
    
    /// <summary>
    /// 加载超时时间
    /// </summary>
    public TimeSpan LoadTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// 是否启用预加载
    /// </summary>
    public bool EnablePreloading { get; set; } = true;
    
    /// <summary>
    /// 预加载路径
    /// </summary>
    public string[] PreloadPaths { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// 是否启用资源缓存
    /// </summary>
    public bool EnableResourceCache { get; set; } = true;
    
    /// <summary>
    /// 缓存清理间隔
    /// </summary>
    public TimeSpan CacheCleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// 默认缓存策略
    /// </summary>
    public ResourceCacheStrategy DefaultCacheStrategy { get; set; } = ResourceCacheStrategy.Default;
    
    public override string ToString()
    {
        return $"MaxConcurrentLoads: {MaxConcurrentLoads}, LoadTimeout: {LoadTimeout}, EnablePreloading: {EnablePreloading}, EnableResourceCache: {EnableResourceCache}";
    }
}