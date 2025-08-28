namespace MF.Commons.Core.Enums.Infrastructure;

/// <summary>
/// 缓存策略
/// </summary>
public enum CachePolicy
{
    /// <summary>
    /// 普通缓存
    /// </summary>
    Normal,
    
    /// <summary>
    /// 高优先级缓存
    /// </summary>
    High,
    
    /// <summary>
    /// 低优先级缓存
    /// </summary>
    Low,
    
    /// <summary>
    /// 永不过期
    /// </summary>
    NeverExpire
}

/// <summary>
/// 资源缓存策略
/// </summary>
public enum ResourceCacheStrategy
{
    /// <summary>
    /// 默认策略 - 遵循系统配置的标准缓存行为
    /// </summary>
    Default,
    
    /// <summary>
    /// 不缓存 - 明确禁用缓存的即时加载
    /// </summary>
    NoCache,
    
    /// <summary>
    /// 强制缓存 - 强制执行缓存的持久化存储
    /// </summary>
    ForceCache,
    
    /// <summary>
    /// 弱引用缓存 - 允许垃圾回收的弱引用缓存
    /// </summary>
    WeakReference,
    
    /// <summary>
    /// 临时缓存 - 具有明确生命周期的临时缓存
    /// </summary>
    Temporary,
    
    /// <summary>
    /// 永久缓存 - 需要显式清理的永久缓存
    /// </summary>
    Permanent
}