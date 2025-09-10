namespace MF.Infrastructure.Abstractions.Core.ResourceLoading;

/// <summary>
/// 资源加载器统计信息
/// </summary>
public class ResourceLoaderStatistics
{
    /// <summary>
    /// 总加载次数
    /// </summary>
    public long TotalLoads { get; set; }
    
    /// <summary>
    /// 成功加载次数
    /// </summary>
    public long SuccessfulLoads { get; set; }
    
    /// <summary>
    /// 失败加载次数
    /// </summary>
    public long FailedLoads { get; set; }
    
    /// <summary>
    /// 缓存命中次数
    /// </summary>
    public long CacheHits { get; set; }
    
    /// <summary>
    /// 缓存未命中次数
    /// </summary>
    public long CacheMisses { get; set; }
    
    /// <summary>
    /// 预加载次数
    /// </summary>
    public long PreloadCount { get; set; }
    
    /// <summary>
    /// 总加载时间
    /// </summary>
    public TimeSpan TotalLoadTime { get; set; }
    
    /// <summary>
    /// 平均加载时间
    /// </summary>
    public TimeSpan AverageLoadTime => TotalLoads > 0 ? TimeSpan.FromTicks(TotalLoadTime.Ticks / TotalLoads) : TimeSpan.Zero;
    
    /// <summary>
    /// 最快加载时间
    /// </summary>
    public TimeSpan FastestLoadTime { get; set; } = TimeSpan.MaxValue;
    
    /// <summary>
    /// 最慢加载时间
    /// </summary>
    public TimeSpan SlowestLoadTime { get; set; }
    
    /// <summary>
    /// 总加载字节数
    /// </summary>
    public long TotalBytesLoaded { get; set; }
    
    /// <summary>
    /// 当前活跃加载数
    /// </summary>
    public int ActiveLoads { get; set; }
    
    /// <summary>
    /// 缓存命中率
    /// </summary>
    public double CacheHitRate => (CacheHits + CacheMisses) > 0 ? (double)CacheHits / (CacheHits + CacheMisses) : 0.0;
    
    /// <summary>
    /// 成功率
    /// </summary>
    public double SuccessRate => TotalLoads > 0 ? (double)SuccessfulLoads / TotalLoads : 0.0;
    
    /// <summary>
    /// 按资源类型分组的统计
    /// </summary>
    public Dictionary<string, ResourceTypeStatistics> TypeStatistics { get; set; } = new();
    
    /// <summary>
    /// 最近的错误信息
    /// </summary>
    public List<ResourceLoadError> RecentErrors { get; set; } = new();
    
    /// <summary>
    /// 统计开始时间
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    public override string ToString()
    {
        return $"ResourceLoaderStatistics(Loads: {TotalLoads}, Success: {SuccessfulLoads}, CacheHitRate: {CacheHitRate:P2}, AvgTime: {AverageLoadTime.TotalMilliseconds:F2}ms)";
    }
}

/// <summary>
/// 按资源类型的统计信息
/// </summary>
public class ResourceTypeStatistics
{
    /// <summary>
    /// 资源类型名称
    /// </summary>
    public string TypeName { get; set; } = string.Empty;
    
    /// <summary>
    /// 加载次数
    /// </summary>
    public long LoadCount { get; set; }
    
    /// <summary>
    /// 总加载时间
    /// </summary>
    public TimeSpan TotalLoadTime { get; set; }
    
    /// <summary>
    /// 平均加载时间
    /// </summary>
    public TimeSpan AverageLoadTime => LoadCount > 0 ? TimeSpan.FromTicks(TotalLoadTime.Ticks / LoadCount) : TimeSpan.Zero;
    
    /// <summary>
    /// 总字节数
    /// </summary>
    public long TotalBytes { get; set; }
    
    /// <summary>
    /// 平均文件大小
    /// </summary>
    public long AverageSize => LoadCount > 0 ? TotalBytes / LoadCount : 0;
}

/// <summary>
/// 资源加载错误信息
/// </summary>
public class ResourceLoadError
{
    /// <summary>
    /// 资源路径
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// 错误消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 异常类型
    /// </summary>
    public string ExceptionType { get; set; } = string.Empty;
    
    /// <summary>
    /// 发生时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; }
}