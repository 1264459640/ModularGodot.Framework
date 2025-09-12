using MediatR;

namespace MF.CQRS.ResourceManagement.StressTesting;

/// <summary>
/// 运行资源压力测试命令
/// </summary>
public record RunResourceStressTestCommand : IRequest<RunResourceStressTestResult>
{
    /// <summary>
    /// 测试类型
    /// </summary>
    public ResourceStressTestType TestType { get; init; }
    
    /// <summary>
    /// 测试持续时间（秒）
    /// </summary>
    public int DurationSeconds { get; init; } = 30;
    
    /// <summary>
    /// 并发线程数
    /// </summary>
    public int ConcurrentThreads { get; init; } = 4;
    
    /// <summary>
    /// 每秒请求数
    /// </summary>
    public int RequestsPerSecond { get; init; } = 10;
    
    /// <summary>
    /// 最大内存使用量（MB）
    /// </summary>
    public long MaxMemoryMB { get; init; } = 500;
    
    /// <summary>
    /// 是否启用内存监控
    /// </summary>
    public bool EnableMemoryMonitoring { get; init; } = true;
    
    /// <summary>
    /// 是否强制垃圾回收
    /// </summary>
    public bool ForceGarbageCollection { get; init; } = true;
    
    /// <summary>
    /// 命令ID
    /// </summary>
    public string CommandId { get; init; } = Guid.NewGuid().ToString();
}