namespace MF.Infrastructure.Abstractions.Core.EventBus;

/// <summary>
/// 事件基类
/// </summary>
public abstract class EventBase
{
    /// <summary>
    /// 事件ID
    /// </summary>
    public string EventId { get; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 事件时间戳
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    
    /// <summary>
    /// 事件源
    /// </summary>
    public virtual string Source { get; protected set; } = "Unknown";
    
    /// <summary>
    /// 关联ID
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// 事件基类
    /// </summary>
    /// <param name="source">事件源</param>
    protected EventBase(string? source = null)
    {
        Source = source ?? GetType().Name;
    }
}