namespace MF.CQRS.ResourceManagement.StressTesting;

/// <summary>
/// 运行资源压力测试命令构建器
/// </summary>
public class RunResourceStressTestCommandBuilder
{
    private ResourceStressTestType _testType = ResourceStressTestType.MemoryPressure;
    private int _durationSeconds = 30;
    private int _concurrentThreads = 4;
    private int _requestsPerSecond = 10;
    private long _maxMemoryMB = 500;
    private bool _enableMemoryMonitoring = true;
    private bool _forceGarbageCollection = true;
    private string? _commandId;

    /// <summary>
    /// 设置测试类型
    /// </summary>
    public RunResourceStressTestCommandBuilder WithTestType(ResourceStressTestType testType)
    {
        _testType = testType;
        return this;
    }

    /// <summary>
    /// 设置测试持续时间（秒）
    /// </summary>
    public RunResourceStressTestCommandBuilder WithDuration(int durationSeconds)
    {
        _durationSeconds = Math.Max(1, durationSeconds);
        return this;
    }

    /// <summary>
    /// 设置并发线程数
    /// </summary>
    public RunResourceStressTestCommandBuilder WithConcurrentThreads(int threads)
    {
        _concurrentThreads = Math.Max(1, threads);
        return this;
    }

    /// <summary>
    /// 设置每秒请求数
    /// </summary>
    public RunResourceStressTestCommandBuilder WithRequestsPerSecond(int rps)
    {
        _requestsPerSecond = Math.Max(1, rps);
        return this;
    }

    /// <summary>
    /// 设置最大内存使用量（MB）
    /// </summary>
    public RunResourceStressTestCommandBuilder WithMaxMemory(long maxMemoryMB)
    {
        _maxMemoryMB = Math.Max(1, maxMemoryMB);
        return this;
    }

    /// <summary>
    /// 设置是否启用内存监控
    /// </summary>
    public RunResourceStressTestCommandBuilder WithMemoryMonitoring(bool enable = true)
    {
        _enableMemoryMonitoring = enable;
        return this;
    }

    /// <summary>
    /// 设置是否强制垃圾回收
    /// </summary>
    public RunResourceStressTestCommandBuilder WithGarbageCollection(bool force = true)
    {
        _forceGarbageCollection = force;
        return this;
    }

    /// <summary>
    /// 设置命令ID
    /// </summary>
    public RunResourceStressTestCommandBuilder WithCommandId(string commandId)
    {
        _commandId = commandId;
        return this;
    }

    /// <summary>
    /// 构建命令
    /// </summary>
    public RunResourceStressTestCommand Build()
    {
        return new RunResourceStressTestCommand
        {
            TestType = _testType,
            DurationSeconds = _durationSeconds,
            ConcurrentThreads = _concurrentThreads,
            RequestsPerSecond = _requestsPerSecond,
            MaxMemoryMB = _maxMemoryMB,
            EnableMemoryMonitoring = _enableMemoryMonitoring,
            ForceGarbageCollection = _forceGarbageCollection,
            CommandId = _commandId ?? Guid.NewGuid().ToString()
        };
    }

    /// <summary>
    /// 创建新的构建器实例
    /// </summary>
    public static RunResourceStressTestCommandBuilder Create() => new();

    /// <summary>
    /// 根据测试类型创建预配置的构建器
    /// </summary>
    public static RunResourceStressTestCommandBuilder ForTestType(ResourceStressTestType testType)
    {
        var builder = new RunResourceStressTestCommandBuilder().WithTestType(testType);
        
        return testType switch
        {
            ResourceStressTestType.MemoryPressure => builder
                .WithDuration(60)
                .WithConcurrentThreads(2)
                .WithRequestsPerSecond(20)
                .WithMaxMemory(1000)
                .WithMemoryMonitoring(true)
                .WithGarbageCollection(true),
                
            ResourceStressTestType.CacheCleanup => builder
                .WithDuration(30)
                .WithConcurrentThreads(1)
                .WithRequestsPerSecond(5)
                .WithMaxMemory(500)
                .WithMemoryMonitoring(true)
                .WithGarbageCollection(true),
                
            ResourceStressTestType.ConcurrentLoad => builder
                .WithDuration(45)
                .WithConcurrentThreads(8)
                .WithRequestsPerSecond(50)
                .WithMaxMemory(800)
                .WithMemoryMonitoring(true)
                .WithGarbageCollection(false),
                
            ResourceStressTestType.MemoryLeak => builder
                .WithDuration(120)
                .WithConcurrentThreads(2)
                .WithRequestsPerSecond(10)
                .WithMaxMemory(2000)
                .WithMemoryMonitoring(true)
                .WithGarbageCollection(false),
                
            ResourceStressTestType.PerformanceBenchmark => builder
                .WithDuration(60)
                .WithConcurrentThreads(4)
                .WithRequestsPerSecond(100)
                .WithMaxMemory(1000)
                .WithMemoryMonitoring(true)
                .WithGarbageCollection(false),
                
            _ => builder
        };
    }
}