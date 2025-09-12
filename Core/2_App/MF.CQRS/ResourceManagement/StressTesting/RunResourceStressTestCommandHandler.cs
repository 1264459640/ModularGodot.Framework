using MediatR;
using MF.CQRS.ResourceManagement.StressTesting;
using MF.CQRS.ResourceManagement.LoadResource;
using Godot;
using System.Diagnostics;
using System.Collections.Concurrent;
using MF.Infrastructure.Abstractions.Core.ResourceManagement;

namespace MF.CQRS.ResourceManagement.StressTesting;

/// <summary>
/// 运行资源压力测试命令处理器
/// </summary>
public class RunResourceStressTestCommandHandler : IRequestHandler<RunResourceStressTestCommand, RunResourceStressTestResult>
{
    private readonly IMediator _mediator;
    private readonly IResourceCacheService? _resourceCacheService;
    private readonly IResourceMonitorService? _resourceMonitorService;
    private readonly List<string> _testResources = new();
    
    public RunResourceStressTestCommandHandler(
        IMediator mediator,
        IResourceCacheService? resourceCacheService = null,
        IResourceMonitorService? resourceMonitorService = null)
    {
        _mediator = mediator;
        _resourceCacheService = resourceCacheService;
        _resourceMonitorService = resourceMonitorService;
        InitializeTestResources();
    }
    
    /// <summary>
    /// 处理运行资源压力测试命令
    /// </summary>
    public async Task<RunResourceStressTestResult> Handle(RunResourceStressTestCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();
        
        try
        {
            GD.Print($"[StressTest] 开始 {request.TestType} 压力测试，持续时间: {request.DurationSeconds}秒");
            
            var result = request.TestType switch
            {
                ResourceStressTestType.MemoryPressure => await ExecuteMemoryPressureTest(request, cancellationToken),
                ResourceStressTestType.CacheCleanup => await ExecuteCacheCleanupTest(request, cancellationToken),
                ResourceStressTestType.ConcurrentLoad => await ExecuteConcurrentLoadTest(request, cancellationToken),
                ResourceStressTestType.MemoryLeak => await ExecuteMemoryLeakTest(request, cancellationToken),
                ResourceStressTestType.PerformanceBenchmark => await ExecutePerformanceBenchmarkTest(request, cancellationToken),
                _ => RunResourceStressTestResult.Failure($"未知的测试类型: {request.TestType}", request.CommandId, request.TestType)
            };
            
            stopwatch.Stop();
            GD.Print($"[StressTest] {request.TestType} 测试完成，总耗时: {stopwatch.ElapsedMilliseconds}ms");
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            errors.Add($"测试执行异常: {ex.Message}");
            GD.PrintErr($"[StressTest] 压力测试异常: {ex.Message}");
            
            return RunResourceStressTestResult.Failure(
                 $"压力测试失败: {ex.Message}",
                 request.CommandId,
                 request.TestType,
                 errors
             );
        }
    }
    
    /// <summary>
    /// 执行内存压力测试
    /// </summary>
    private async Task<RunResourceStressTestResult> ExecuteMemoryPressureTest(RunResourceStressTestCommand request, CancellationToken cancellationToken)
    {
        var initialMemory = GC.GetTotalMemory(false);
        var peakMemory = initialMemory;
        var totalRequests = 0L;
        var successfulRequests = 0L;
        var responseTimes = new List<double>();
        var initialGCCount = GetTotalGCCount();
        
        GD.Print($"[StressTest] 内存压力测试开始，初始内存: {FormatBytes(initialMemory)}");
        
        var endTime = DateTime.Now.AddSeconds(request.DurationSeconds);
        var resourceIndex = 0;
        
        while (DateTime.Now < endTime && !cancellationToken.IsCancellationRequested)
        {
            var requestStopwatch = Stopwatch.StartNew();
            
            try
            {
                // 循环使用测试资源
                var resourcePath = _testResources[resourceIndex % _testResources.Count];
                var cacheKey = $"stress_test_{totalRequests}_{resourcePath}";
                
                // 使用CQRS指令加载资源
                var loadCommand = new LoadResourceRequest
                {
                    ResourcePath = resourcePath,
                    ResourceType = GetResourceTypeFromPath(resourcePath),
                    IsAsync = true,
                    RequestId = Guid.NewGuid().ToString()
                };
                
                var loadResult = await _mediator.Send(loadCommand, cancellationToken);
                
                if (loadResult.IsSuccess)
                {
                    successfulRequests++;
                    
                    // 如果有缓存服务，将结果存入缓存
                    if (_resourceCacheService != null)
                    {
                        // 注意：这里我们不能直接缓存LoadResult，需要重新加载实际资源
                        // 在实际应用中，LoadResourceRequest应该返回实际的资源对象
                        var resource = GD.Load(resourcePath);
                        if (resource != null)
                        {
                            await _resourceCacheService.SetAsync(cacheKey, resource, cancellationToken: cancellationToken);
                        }
                    }
                }
                else
                {
                    GD.PrintErr($"[StressTest] 资源加载失败: {loadResult.Message}");
                }
                
                resourceIndex++;
                totalRequests++;
                
                requestStopwatch.Stop();
                responseTimes.Add(requestStopwatch.Elapsed.TotalMilliseconds);
                
                // 监控内存使用
                var currentMemory = GC.GetTotalMemory(false);
                if (currentMemory > peakMemory)
                {
                    peakMemory = currentMemory;
                }
                
                // 检查内存限制
                if (currentMemory > request.MaxMemoryMB * 1024 * 1024)
                {
                    GD.Print($"[StressTest] 达到内存限制 {request.MaxMemoryMB}MB，当前内存: {FormatBytes(currentMemory)}");
                    break;
                }
                
                // 控制请求频率
                if (request.RequestsPerSecond > 0)
                {
                    await Task.Delay(1000 / request.RequestsPerSecond, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[StressTest] 请求失败: {ex.Message}");
            }
        }
        
        // 强制垃圾回收
        if (request.ForceGarbageCollection)
        {
            GD.Print($"[StressTest] 执行垃圾回收前内存: {FormatBytes(GC.GetTotalMemory(false))}");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Delay(1000); // 等待GC完成
        }
        
        var finalMemory = GC.GetTotalMemory(false);
        var finalGCCount = GetTotalGCCount();
        var avgResponseTime = responseTimes.Count > 0 ? responseTimes.Average() : 0;
        
        GD.Print($"[StressTest] 内存压力测试完成:");
        GD.Print($"  - 总请求数: {totalRequests}");
        GD.Print($"  - 成功请求: {successfulRequests}");
        GD.Print($"  - 初始内存: {FormatBytes(initialMemory)}");
        GD.Print($"  - 峰值内存: {FormatBytes(peakMemory)}");
        GD.Print($"  - 最终内存: {FormatBytes(finalMemory)}");
        GD.Print($"  - 内存释放: {FormatBytes(Math.Max(0, peakMemory - finalMemory))}");
        GD.Print($"  - GC次数: {finalGCCount - initialGCCount}");
        
        var metrics = new Dictionary<string, object>
        {
            ["initial_memory_mb"] = initialMemory / (1024.0 * 1024.0),
            ["peak_memory_mb"] = peakMemory / (1024.0 * 1024.0),
            ["final_memory_mb"] = finalMemory / (1024.0 * 1024.0),
            ["memory_growth_mb"] = (peakMemory - initialMemory) / (1024.0 * 1024.0),
            ["memory_released_mb"] = Math.Max(0, peakMemory - finalMemory) / (1024.0 * 1024.0)
        };
        
        return RunResourceStressTestResult.Success(
            $"内存压力测试完成，处理 {totalRequests} 个请求，内存释放 {FormatBytes(Math.Max(0, peakMemory - finalMemory))}",
            request.CommandId,
            request.TestType,
            TimeSpan.FromMilliseconds(responseTimes.Sum()),
            totalRequests,
            successfulRequests,
            avgResponseTime,
            peakMemory,
            initialMemory,
            finalMemory,
            finalGCCount - initialGCCount,
            totalRequests > 0 ? (double)successfulRequests / totalRequests : 0,
            metrics
        );
    }
    
    /// <summary>
    /// 执行缓存清理测试
    /// </summary>
    private async Task<RunResourceStressTestResult> ExecuteCacheCleanupTest(RunResourceStressTestCommand request, CancellationToken cancellationToken)
    {
        if (_resourceCacheService == null)
        {
            return RunResourceStressTestResult.Failure("ResourceCacheService 不可用", request.CommandId, request.TestType);
        }
        
        var initialMemory = GC.GetTotalMemory(false);
        var totalRequests = 0L;
        var successfulRequests = 0L;
        var responseTimes = new List<double>();
        
        GD.Print($"[StressTest] 缓存清理测试开始");
        
        // 第一阶段：填充缓存
        GD.Print($"[StressTest] 阶段1: 填充缓存");
        for (int i = 0; i < 100 && !cancellationToken.IsCancellationRequested; i++)
        {
            var resourcePath = _testResources[i % _testResources.Count];
            var cacheKey = $"cache_test_{i}_{resourcePath}";
            
            try
            {
                // 使用CQRS指令加载资源
                var loadCommand = new LoadResourceRequest
                {
                    ResourcePath = resourcePath,
                    ResourceType = GetResourceTypeFromPath(resourcePath),
                    IsAsync = true,
                    RequestId = Guid.NewGuid().ToString()
                };
                
                var loadResult = await _mediator.Send(loadCommand, cancellationToken);
                
                if (loadResult.IsSuccess)
                {
                    // 重新加载实际资源对象用于缓存
                    var resource = GD.Load(resourcePath);
                    if (resource != null)
                    {
                        await _resourceCacheService.SetAsync(cacheKey, resource, cancellationToken: cancellationToken);
                        successfulRequests++;
                    }
                }
                totalRequests++;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[StressTest] 缓存填充失败: {ex.Message}");
            }
        }
        
        var afterFillMemory = GC.GetTotalMemory(false);
        GD.Print($"[StressTest] 缓存填充完成，内存使用: {FormatBytes(afterFillMemory)}");
        
        // 第二阶段：等待自动清理或手动触发清理
        GD.Print($"[StressTest] 阶段2: 等待缓存清理");
        await Task.Delay(5000, cancellationToken); // 等待5秒
        
        // 手动触发清理
        if (_resourceCacheService != null)
        {
            await _resourceCacheService.CleanupAsync(cancellationToken);
        }
        
        // 强制垃圾回收
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        await Task.Delay(1000);
        
        var finalMemory = GC.GetTotalMemory(false);
        
        GD.Print($"[StressTest] 缓存清理测试完成:");
        GD.Print($"  - 初始内存: {FormatBytes(initialMemory)}");
        GD.Print($"  - 填充后内存: {FormatBytes(afterFillMemory)}");
        GD.Print($"  - 清理后内存: {FormatBytes(finalMemory)}");
        GD.Print($"  - 内存释放: {FormatBytes(Math.Max(0, afterFillMemory - finalMemory))}");
        
        return RunResourceStressTestResult.Success(
            $"缓存清理测试完成，内存释放 {FormatBytes(Math.Max(0, afterFillMemory - finalMemory))}",
            request.CommandId,
            request.TestType,
            TimeSpan.FromSeconds(request.DurationSeconds),
            totalRequests,
            successfulRequests,
            0,
            afterFillMemory,
            initialMemory,
            finalMemory,
            0,
            1.0
        );
    }
    
    /// <summary>
    /// 执行并发加载测试
    /// </summary>
    private async Task<RunResourceStressTestResult> ExecuteConcurrentLoadTest(RunResourceStressTestCommand request, CancellationToken cancellationToken)
    {
        var initialMemory = GC.GetTotalMemory(false);
        var totalRequests = 0L;
        var successfulRequests = 0L;
        var responseTimes = new ConcurrentBag<double>();
        var tasks = new List<Task>();
        
        GD.Print($"[StressTest] 并发加载测试开始，线程数: {request.ConcurrentThreads}");
        
        for (int threadId = 0; threadId < request.ConcurrentThreads; threadId++)
        {
            var task = Task.Run(async () =>
            {
                var endTime = DateTime.Now.AddSeconds(request.DurationSeconds);
                var localRequests = 0;
                var localSuccessful = 0;
                
                while (DateTime.Now < endTime && !cancellationToken.IsCancellationRequested)
                {
                    var stopwatch = Stopwatch.StartNew();
                    
                    try
                    {
                        var resourcePath = _testResources[(int)(localRequests % _testResources.Count)];
                        
                        // 使用CQRS指令加载资源
                        var loadCommand = new LoadResourceRequest
                        {
                            ResourcePath = resourcePath,
                            ResourceType = GetResourceTypeFromPath(resourcePath),
                            IsAsync = true,
                            RequestId = Guid.NewGuid().ToString()
                        };
                        
                        var loadResult = await _mediator.Send(loadCommand, cancellationToken);
                        
                        if (loadResult.IsSuccess)
                        {
                            localSuccessful++;
                        }
                        
                        localRequests++;
                        
                        stopwatch.Stop();
                        responseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                        
                        if (request.RequestsPerSecond > 0)
                        {
                            await Task.Delay(1000 / request.RequestsPerSecond, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"[StressTest] 并发请求失败: {ex.Message}");
                    }
                }
                
                Interlocked.Add(ref totalRequests, localRequests);
                Interlocked.Add(ref successfulRequests, localSuccessful);
            }, cancellationToken);
            
            tasks.Add(task);
        }
        
        await Task.WhenAll(tasks);
        
        var finalMemory = GC.GetTotalMemory(false);
        var avgResponseTime = responseTimes.Count > 0 ? responseTimes.Average() : 0;
        
        GD.Print($"[StressTest] 并发加载测试完成:");
        GD.Print($"  - 总请求数: {totalRequests}");
        GD.Print($"  - 成功请求: {successfulRequests}");
        GD.Print($"  - 平均响应时间: {avgResponseTime:F2}ms");
        GD.Print($"  - 内存使用: {FormatBytes(finalMemory)}");
        
        return RunResourceStressTestResult.Success(
            $"并发加载测试完成，{request.ConcurrentThreads} 个线程处理 {totalRequests} 个请求",
            request.CommandId,
            request.TestType,
            TimeSpan.FromSeconds(request.DurationSeconds),
            totalRequests,
            successfulRequests,
            avgResponseTime,
            finalMemory,
            initialMemory,
            finalMemory,
            0,
            totalRequests > 0 ? (double)successfulRequests / totalRequests : 0
        );
    }
    
    /// <summary>
    /// 执行内存泄漏测试
    /// </summary>
    private async Task<RunResourceStressTestResult> ExecuteMemoryLeakTest(RunResourceStressTestCommand request, CancellationToken cancellationToken)
    {
        var memorySnapshots = new List<(DateTime Time, long Memory)>();
        var initialMemory = GC.GetTotalMemory(true); // 强制GC后的内存
        memorySnapshots.Add((DateTime.Now, initialMemory));
        
        GD.Print($"[StressTest] 内存泄漏测试开始，初始内存: {FormatBytes(initialMemory)}");
        
        var totalRequests = 0L;
        var successfulRequests = 0L;
        var endTime = DateTime.Now.AddSeconds(request.DurationSeconds);
        
        while (DateTime.Now < endTime && !cancellationToken.IsCancellationRequested)
        {
            // 执行一轮资源加载和释放
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var resourcePath = _testResources[(int)(i % _testResources.Count)];
                    
                    // 使用CQRS指令加载资源
                    var loadCommand = new LoadResourceRequest
                    {
                        ResourcePath = resourcePath,
                        ResourceType = GetResourceTypeFromPath(resourcePath),
                        IsAsync = true,
                        RequestId = Guid.NewGuid().ToString()
                    };
                    
                    var loadResult = await _mediator.Send(loadCommand, cancellationToken);
                    
                    if (loadResult.IsSuccess)
                    {
                        successfulRequests++;
                        // 注意：CQRS指令已经处理了资源加载，这里不需要额外的引用管理
                    }
                    
                    totalRequests++;
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[StressTest] 内存泄漏测试请求失败: {ex.Message}");
                }
            }
            
            // 定期记录内存快照
            if (totalRequests % 100 == 0)
            {
                GC.Collect();
                await Task.Delay(100);
                var currentMemory = GC.GetTotalMemory(false);
                memorySnapshots.Add((DateTime.Now, currentMemory));
                
                GD.Print($"[StressTest] 内存快照 - 请求: {totalRequests}, 内存: {FormatBytes(currentMemory)}");
            }
            
            await Task.Delay(100, cancellationToken);
        }
        
        // 最终内存检查
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        await Task.Delay(1000);
        
        var finalMemory = GC.GetTotalMemory(false);
        memorySnapshots.Add((DateTime.Now, finalMemory));
        
        // 分析内存趋势
        var memoryGrowth = finalMemory - initialMemory;
        var isMemoryLeak = memoryGrowth > (initialMemory * 0.1); // 如果内存增长超过10%认为可能有泄漏
        
        GD.Print($"[StressTest] 内存泄漏测试完成:");
        GD.Print($"  - 总请求数: {totalRequests}");
        GD.Print($"  - 初始内存: {FormatBytes(initialMemory)}");
        GD.Print($"  - 最终内存: {FormatBytes(finalMemory)}");
        GD.Print($"  - 内存增长: {FormatBytes(memoryGrowth)}");
        GD.Print($"  - 疑似内存泄漏: {(isMemoryLeak ? "是" : "否")}");
        
        var metrics = new Dictionary<string, object>
        {
            ["memory_growth_bytes"] = memoryGrowth,
            ["memory_growth_percentage"] = initialMemory > 0 ? (double)memoryGrowth / initialMemory * 100 : 0,
            ["suspected_memory_leak"] = isMemoryLeak,
            ["memory_snapshots_count"] = memorySnapshots.Count
        };
        
        return RunResourceStressTestResult.Success(
            $"内存泄漏测试完成，内存增长 {FormatBytes(memoryGrowth)}，{(isMemoryLeak ? "疑似" : "无")}内存泄漏",
            request.CommandId,
            request.TestType,
            TimeSpan.FromSeconds(request.DurationSeconds),
            totalRequests,
            successfulRequests,
            0,
            memorySnapshots.Max(s => s.Memory),
            initialMemory,
            finalMemory,
            0,
            1.0,
            metrics
        );
    }
    
    /// <summary>
    /// 执行性能基准测试
    /// </summary>
    private async Task<RunResourceStressTestResult> ExecutePerformanceBenchmarkTest(RunResourceStressTestCommand request, CancellationToken cancellationToken)
    {
        var initialMemory = GC.GetTotalMemory(false);
        var totalRequests = 0L;
        var successfulRequests = 0L;
        var responseTimes = new List<double>();
        var throughputSamples = new List<double>();
        
        GD.Print($"[StressTest] 性能基准测试开始");
        
        var endTime = DateTime.Now.AddSeconds(request.DurationSeconds);
        var lastSampleTime = DateTime.Now;
        var lastRequestCount = 0L;
        
        while (DateTime.Now < endTime && !cancellationToken.IsCancellationRequested)
        {
            var batchStopwatch = Stopwatch.StartNew();
            
            // 批量处理请求
            for (int i = 0; i < 10 && DateTime.Now < endTime; i++)
            {
                var requestStopwatch = Stopwatch.StartNew();
                
                try
                {
                    var resourcePath = _testResources[(int)(totalRequests % _testResources.Count)];
                    
                    // 使用CQRS指令加载资源
                    var loadCommand = new LoadResourceRequest
                    {
                        ResourcePath = resourcePath,
                        ResourceType = GetResourceTypeFromPath(resourcePath),
                        IsAsync = true,
                        RequestId = Guid.NewGuid().ToString()
                    };
                    
                    var loadResult = await _mediator.Send(loadCommand, cancellationToken);
                    
                    if (loadResult.IsSuccess)
                    {
                        successfulRequests++;
                    }
                    
                    totalRequests++;
                    
                    requestStopwatch.Stop();
                    responseTimes.Add(requestStopwatch.Elapsed.TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[StressTest] 基准测试请求失败: {ex.Message}");
                }
            }
            
            // 计算吞吐量
            var now = DateTime.Now;
            if ((now - lastSampleTime).TotalSeconds >= 1.0)
            {
                var requestsInPeriod = totalRequests - lastRequestCount;
                var timeSpan = (now - lastSampleTime).TotalSeconds;
                var throughput = requestsInPeriod / timeSpan;
                
                throughputSamples.Add(throughput);
                
                lastSampleTime = now;
                lastRequestCount = totalRequests;
                
                GD.Print($"[StressTest] 当前吞吐量: {throughput:F2} 请求/秒");
            }
            
            batchStopwatch.Stop();
            
            // 控制请求频率
            if (request.RequestsPerSecond > 0)
            {
                var targetDelay = (10 * 1000) / request.RequestsPerSecond; // 10个请求的目标延迟
                var actualDelay = Math.Max(0, targetDelay - (int)batchStopwatch.ElapsedMilliseconds);
                if (actualDelay > 0)
                {
                    await Task.Delay(actualDelay, cancellationToken);
                }
            }
        }
        
        var finalMemory = GC.GetTotalMemory(false);
        var avgResponseTime = responseTimes.Count > 0 ? responseTimes.Average() : 0;
        var avgThroughput = throughputSamples.Count > 0 ? throughputSamples.Average() : 0;
        var maxThroughput = throughputSamples.Count > 0 ? throughputSamples.Max() : 0;
        
        GD.Print($"[StressTest] 性能基准测试完成:");
        GD.Print($"  - 总请求数: {totalRequests}");
        GD.Print($"  - 成功请求: {successfulRequests}");
        GD.Print($"  - 平均响应时间: {avgResponseTime:F2}ms");
        GD.Print($"  - 平均吞吐量: {avgThroughput:F2} 请求/秒");
        GD.Print($"  - 最大吞吐量: {maxThroughput:F2} 请求/秒");
        
        var metrics = new Dictionary<string, object>
        {
            ["average_throughput_rps"] = avgThroughput,
            ["max_throughput_rps"] = maxThroughput,
            ["min_response_time_ms"] = responseTimes.Count > 0 ? responseTimes.Min() : 0,
            ["max_response_time_ms"] = responseTimes.Count > 0 ? responseTimes.Max() : 0,
            ["p95_response_time_ms"] = responseTimes.Count > 0 ? GetPercentile(responseTimes, 0.95) : 0,
            ["p99_response_time_ms"] = responseTimes.Count > 0 ? GetPercentile(responseTimes, 0.99) : 0
        };
        
        return RunResourceStressTestResult.Success(
            $"性能基准测试完成，平均吞吐量 {avgThroughput:F2} 请求/秒，平均响应时间 {avgResponseTime:F2}ms",
            request.CommandId,
            request.TestType,
            TimeSpan.FromSeconds(request.DurationSeconds),
            totalRequests,
            successfulRequests,
            avgResponseTime,
            finalMemory,
            initialMemory,
            finalMemory,
            0,
            totalRequests > 0 ? (double)successfulRequests / totalRequests : 0,
            metrics
        );
    }
    
    /// <summary>
    /// 初始化测试资源列表
    /// </summary>
    private void InitializeTestResources()
    {
        // 添加一些常见的测试资源路径
        _testResources.AddRange(new[]
        {
            "res://icon.svg",
            "res://Assets/ShaderPatterns/circle.png",
            "res://Assets/ShaderPatterns/dirt.png",
            "res://Assets/ShaderPatterns/pixel.png",
            "res://Assets/ShaderPatterns/radial.png",
            "res://Assets/UI/MainTheme.tres"
        });
        
        // 动态发现更多资源
        try
        {
            DiscoverAdditionalResources("res://Assets");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[StressTest] 资源发现失败: {ex.Message}");
        }
        
        GD.Print($"[StressTest] 初始化完成，发现 {_testResources.Count} 个测试资源");
    }
    
    /// <summary>
    /// 发现额外的测试资源
    /// </summary>
    private void DiscoverAdditionalResources(string basePath)
    {
        var dir = DirAccess.Open(basePath);
        if (dir == null) return;
        
        dir.ListDirBegin();
        string fileName = dir.GetNext();
        
        while (fileName != "")
        {
            var fullPath = $"{basePath}/{fileName}";
            
            if (dir.CurrentIsDir() && !fileName.StartsWith("."))
            {
                DiscoverAdditionalResources(fullPath);
            }
            else if (!fileName.StartsWith(".") && !fileName.EndsWith(".import"))
            {
                var extension = System.IO.Path.GetExtension(fileName).ToLower();
                if (IsTestableResource(extension))
                {
                    _testResources.Add(fullPath);
                }
            }
            
            fileName = dir.GetNext();
        }
        
        dir.ListDirEnd();
    }
    
    /// <summary>
    /// 检查是否为可测试的资源
    /// </summary>
    private static bool IsTestableResource(string extension)
    {
        return extension switch
        {
            ".png" or ".jpg" or ".jpeg" or ".svg" or ".webp" => true,
            ".ogg" or ".wav" or ".mp3" => true,
            ".tres" or ".tscn" => true,
            _ => false
        };
    }
    
    /// <summary>
    /// 获取总GC次数
    /// </summary>
    private static int GetTotalGCCount()
    {
        return GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
    }
    
    /// <summary>
    /// 格式化字节数显示
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
    }
    
    /// <summary>
    /// 根据文件路径获取资源类型
    /// </summary>
    private static ResourceType GetResourceTypeFromPath(string path)
    {
        var extension = System.IO.Path.GetExtension(path).ToLower();
        return extension switch
        {
            ".png" or ".jpg" or ".jpeg" or ".svg" or ".webp" => ResourceType.Image,
            ".ogg" or ".wav" or ".mp3" => ResourceType.Audio,
            ".tscn" or ".scn" => ResourceType.Scene,
            ".tres" => ResourceType.Material,
            _ => ResourceType.Image // 默认为图像类型
        };
    }
    
    /// <summary>
    /// 计算百分位数
    /// </summary>
    private static double GetPercentile(List<double> values, double percentile)
    {
        if (values.Count == 0) return 0;
        
        var sorted = values.OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }
}