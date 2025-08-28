namespace MF.Infrastructure.Abstractions.Core.Monitoring;

/// <summary>
/// 性能监控接口
/// </summary>
public interface IPerformanceMonitor
{
    /// <summary>
    /// 记录指标
    /// </summary>
    /// <param name="name">指标名称</param>
    /// <param name="value">指标值</param>
    /// <param name="tags">标签</param>
    void RecordMetric(string name, double value, Dictionary<string, string>? tags = null);
    
    /// <summary>
    /// 记录计数器
    /// </summary>
    /// <param name="name">计数器名称</param>
    /// <param name="value">计数值</param>
    /// <param name="tags">标签</param>
    void RecordCounter(string name, long value = 1, Dictionary<string, string>? tags = null);
    
    /// <summary>
    /// 记录计时器
    /// </summary>
    /// <param name="name">计时器名称</param>
    /// <param name="duration">持续时间</param>
    /// <param name="tags">标签</param>
    void RecordTimer(string name, TimeSpan duration, Dictionary<string, string>? tags = null);
    
    /// <summary>
    /// 开始计时
    /// </summary>
    /// <param name="name">计时器名称</param>
    /// <param name="tags">标签</param>
    /// <returns>计时器句柄</returns>
    IDisposable StartTimer(string name, Dictionary<string, string>? tags = null);
    

}