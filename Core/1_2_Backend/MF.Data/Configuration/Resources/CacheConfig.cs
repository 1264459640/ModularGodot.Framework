namespace MF.Data.Configuration.Resources;

/// <summary>
/// 缓存配置
/// </summary>
public class CacheConfig
{
    /// <summary>
    /// 默认过期时间
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);
    
    /// <summary>
    /// 最大缓存大小（字节）
    /// </summary>
    public long MaxCacheSize { get; set; } = 100 * 1024 * 1024; // 100MB
    
    /// <summary>
    /// 是否启用统计信息
    /// </summary>
    public bool EnableStatistics { get; set; } = true;
    
    /// <summary>
    /// 缓存压缩阈值
    /// </summary>
    public double CompactionPercentage { get; set; } = 0.8;
    
    /// <summary>
    /// 是否使用分布式缓存
    /// </summary>
    public bool UseDistributedCache { get; set; } = false;
    
    /// <summary>
    /// Redis连接字符串（当使用分布式缓存时）
    /// </summary>
    public string? RedisConnectionString { get; set; }
    
    /// <summary>
    /// 内存缓存大小限制
    /// </summary>
    public long? MemoryCacheSizeLimit { get; set; }
    
    public override string ToString()
    {
        return $"DefaultExpiration: {DefaultExpiration}, MaxCacheSize: {MaxCacheSize}, EnableStatistics: {EnableStatistics}, CompactionPercentage: {CompactionPercentage}";
    }
}