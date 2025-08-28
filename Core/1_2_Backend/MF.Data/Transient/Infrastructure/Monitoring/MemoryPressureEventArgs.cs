using MF.Commons.Core.Enums.Infrastructure;

namespace MF.Data.Transient.Infrastructure.Monitoring;

/// <summary>
/// 内存压力事件参数
/// </summary>
public class MemoryPressureEventArgs : EventArgs
{
    /// <summary>
    /// 当前内存使用量
    /// </summary>
    public long CurrentUsage { get; set; }
    
    /// <summary>
    /// 之前的内存使用量
    /// </summary>
    public long PreviousUsage { get; set; }
    
    /// <summary>
    /// 内存阈值
    /// </summary>
    public long Threshold { get; set; }
    
    /// <summary>
    /// 压力级别
    /// </summary>
    public MemoryPressureLevel PressureLevel { get; set; }
    
    /// <summary>
    /// 检测时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}