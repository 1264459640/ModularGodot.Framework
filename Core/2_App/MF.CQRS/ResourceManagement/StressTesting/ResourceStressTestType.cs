namespace MF.CQRS.ResourceManagement.StressTesting;

/// <summary>
/// 资源压力测试类型
/// </summary>
public enum ResourceStressTestType
{
    /// <summary>
    /// 内存压力测试 - 大量加载资源直到内存压力
    /// </summary>
    MemoryPressure,
    
    /// <summary>
    /// 缓存清理测试 - 测试自动缓存清理机制
    /// </summary>
    CacheCleanup,
    
    /// <summary>
    /// 并发加载测试 - 多线程并发加载资源
    /// </summary>
    ConcurrentLoad,
    
    /// <summary>
    /// 内存泄漏测试 - 长时间运行检测内存泄漏
    /// </summary>
    MemoryLeak,
    
    /// <summary>
    /// 性能基准测试 - 测试系统性能基准
    /// </summary>
    PerformanceBenchmark
}