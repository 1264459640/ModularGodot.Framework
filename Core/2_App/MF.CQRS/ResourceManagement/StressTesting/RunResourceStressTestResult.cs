namespace MF.CQRS.ResourceManagement.StressTesting;

/// <summary>
/// 运行资源压力测试结果
/// </summary>
public record RunResourceStressTestResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// 测试消息
    /// </summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// 命令ID
    /// </summary>
    public string CommandId { get; init; } = string.Empty;
    
    /// <summary>
    /// 测试类型
    /// </summary>
    public ResourceStressTestType TestType { get; init; }
    
    /// <summary>
    /// 测试持续时间
    /// </summary>
    public TimeSpan Duration { get; init; }
    
    /// <summary>
    /// 总请求数
    /// </summary>
    public long TotalRequests { get; init; }
    
    /// <summary>
    /// 成功请求数
    /// </summary>
    public long SuccessfulRequests { get; init; }
    
    /// <summary>
    /// 失败请求数
    /// </summary>
    public long FailedRequests { get; init; }
    
    /// <summary>
    /// 平均响应时间（毫秒）
    /// </summary>
    public double AverageResponseTimeMs { get; init; }
    
    /// <summary>
    /// 最大内存使用量（字节）
    /// </summary>
    public long PeakMemoryUsage { get; init; }
    
    /// <summary>
    /// 初始内存使用量（字节）
    /// </summary>
    public long InitialMemoryUsage { get; init; }
    
    /// <summary>
    /// 最终内存使用量（字节）
    /// </summary>
    public long FinalMemoryUsage { get; init; }
    
    /// <summary>
    /// 内存释放量（字节）
    /// </summary>
    public long MemoryReleased => Math.Max(0, PeakMemoryUsage - FinalMemoryUsage);
    
    /// <summary>
    /// 内存释放率
    /// </summary>
    public double MemoryReleaseRate => PeakMemoryUsage > InitialMemoryUsage ? 
        (double)MemoryReleased / (PeakMemoryUsage - InitialMemoryUsage) : 0;
    
    /// <summary>
    /// 垃圾回收次数
    /// </summary>
    public int GCCollectionCount { get; init; }
    
    /// <summary>
    /// 缓存命中率
    /// </summary>
    public double CacheHitRate { get; init; }
    
    /// <summary>
    /// 错误信息列表
    /// </summary>
    public List<string> Errors { get; init; } = new();
    
    /// <summary>
    /// 性能指标
    /// </summary>
    public Dictionary<string, object> PerformanceMetrics { get; init; } = new();
    
    /// <summary>
    /// 处理时间戳
    /// </summary>
    public DateTime ProcessedAt { get; init; } = DateTime.Now;
    
    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static RunResourceStressTestResult Success(
        string message, 
        string commandId, 
        ResourceStressTestType testType,
        TimeSpan duration,
        long totalRequests,
        long successfulRequests,
        double avgResponseTime,
        long peakMemory,
        long initialMemory,
        long finalMemory,
        int gcCount,
        double cacheHitRate,
        Dictionary<string, object>? metrics = null)
    {
        return new RunResourceStressTestResult
        {
            IsSuccess = true,
            Message = message,
            CommandId = commandId,
            TestType = testType,
            Duration = duration,
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            FailedRequests = totalRequests - successfulRequests,
            AverageResponseTimeMs = avgResponseTime,
            PeakMemoryUsage = peakMemory,
            InitialMemoryUsage = initialMemory,
            FinalMemoryUsage = finalMemory,
            GCCollectionCount = gcCount,
            CacheHitRate = cacheHitRate,
            PerformanceMetrics = metrics ?? new Dictionary<string, object>(),
            ProcessedAt = DateTime.Now
        };
    }
    
    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static RunResourceStressTestResult Failure(
        string message, 
        string commandId, 
        ResourceStressTestType testType,
        List<string>? errors = null)
    {
        return new RunResourceStressTestResult
        {
            IsSuccess = false,
            Message = message,
            CommandId = commandId,
            TestType = testType,
            Errors = errors ?? new List<string>(),
            ProcessedAt = DateTime.Now
        };
    }
}