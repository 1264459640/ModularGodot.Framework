

using MF.Commons.Core.Enums.Infrastructure;

namespace MF.Data.Transient.Infrastructure.Monitoring;

/// <summary>
/// 内存信息（合并MemoryStatistics和MemorySnapshot）
/// </summary>
public class MemoryInfo
{
    /// <summary>
    /// 当前内存使用量
    /// </summary>
    public long CurrentUsage { get; set; }
    
    /// <summary>
    /// 峰值内存使用量
    /// </summary>
    public long PeakUsage { get; set; }
    
    /// <summary>
    /// 当前压力级别
    /// </summary>
    public MemoryPressureLevel PressureLevel { get; set; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// GC回收次数（简化版）
    /// </summary>
    public int TotalGCCollections { get; set; }
}