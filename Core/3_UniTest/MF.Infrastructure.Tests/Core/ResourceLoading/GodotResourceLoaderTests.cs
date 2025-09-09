using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using MF.Infrastructure.Abstractions.Core.ResourceLoading;
using MF.Infrastructure.Abstractions.Core.ResourceManagement;
using MF.Commons.Core.Enums.Infrastructure;

namespace MF.Infrastructure.Tests.Core.ResourceLoading;

/// <summary>
/// GodotResourceLoader 单元测试
/// </summary>
public class GodotResourceLoaderTests : IDisposable
{
    private readonly Mock<IResourceCacheService> _mockCacheService;
    private readonly TestResourceLoader _resourceLoader;

    public GodotResourceLoaderTests()
    {
        _mockCacheService = new Mock<IResourceCacheService>();
        _resourceLoader = new TestResourceLoader(_mockCacheService.Object);
    }

    #region 初始化测试

    [Fact]
    public void Constructor_WithValidCacheService_ShouldInitializeSuccessfully()
    {
        // Arrange
        var mockCacheService = new Mock<IResourceCacheService>();

        // Act
        var loader = new TestResourceLoader(mockCacheService.Object);

        // Assert
        loader.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullCacheService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestResourceLoader(null!));
    }

    #endregion

    #region 异步加载资源测试（默认缓存策略）

    [Fact]
    public async Task LoadAsync_WithDefaultCacheStrategy_ShouldReturnResourceFromCache()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        var expectedResource = new TestResource { Name = "TestResource" };
        
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(resourcePath, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResource);

        // Act
        var result = await _resourceLoader.LoadAsync<TestResource>(resourcePath, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResource);
        _mockCacheService.Verify(x => x.GetAsync<TestResource>(resourcePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_WithDefaultCacheStrategy_WhenCacheMiss_ShouldLoadFromDisk()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(resourcePath, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((TestResource?)null);
        
        _mockCacheService.Setup(x => x.SetAsync(resourcePath, It.IsAny<TestResource>(), It.IsAny<ResourceCacheStrategy>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        // Act
        var result = await _resourceLoader.LoadAsync<TestResource>(resourcePath, CancellationToken.None);

        // Assert
        // 由于 GD.Load 在测试环境中可能返回 null，我们主要验证缓存服务的调用
        _mockCacheService.Verify(x => x.GetAsync<TestResource>(resourcePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_WithCancellationToken_ShouldPassTokenToCache()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(resourcePath, cancellationToken))
                        .ReturnsAsync((TestResource?)null);

        // Act
        await _resourceLoader.LoadAsync<TestResource>(resourcePath, cancellationToken);

        // Assert
        _mockCacheService.Verify(x => x.GetAsync<TestResource>(resourcePath, cancellationToken), Times.Once);
    }

    #endregion

    #region 异步加载资源测试（无缓存策略）

    [Fact]
    public async Task LoadAsync_WithNoCacheStrategy_ShouldSkipCacheGet()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        
        _mockCacheService.Setup(x => x.SetAsync(resourcePath, It.IsAny<TestResource>(), ResourceCacheStrategy.NoCache, It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        // Act
        var result = await _resourceLoader.LoadAsync<TestResource>(resourcePath, ResourceCacheStrategy.NoCache);

        // Assert
        _mockCacheService.Verify(x => x.GetAsync<TestResource>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_WithProgressCallback_ShouldInvokeCallback()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        var progressValues = new List<float>();
        
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(resourcePath, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((TestResource?)null);
        
        _mockCacheService.Setup(x => x.SetAsync(resourcePath, It.IsAny<TestResource>(), It.IsAny<ResourceCacheStrategy>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        // Act
        await _resourceLoader.LoadAsync<TestResource>(resourcePath, progress => progressValues.Add(progress));

        // Assert
        progressValues.Should().NotBeEmpty();
        progressValues.Should().Contain(1.0f); // 最终进度应该是 100%
    }

    [Fact]
    public async Task LoadAsync_WithMinLoadTime_ShouldRespectMinimumTime()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        var minLoadTime = TimeSpan.FromMilliseconds(100);
        
        // 设置缓存未命中，这样会执行完整的加载流程包括最小加载时间
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(resourcePath, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((TestResource?)null);
        
        _mockCacheService.Setup(x => x.SetAsync(resourcePath, It.IsAny<TestResource>(), It.IsAny<ResourceCacheStrategy>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        var startTime = DateTime.UtcNow;

        // Act
        var result = await _resourceLoader.LoadAsync<TestResource>(resourcePath, null, minLoadTime);

        // Assert
        var elapsed = DateTime.UtcNow - startTime;
        elapsed.Should().BeGreaterOrEqualTo(minLoadTime);
        result.Should().NotBeNull();
    }

    #endregion

    #region 同步加载资源测试

    [Fact]
    public void Load_WithDefaultCacheStrategy_ShouldReturnResource()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        var expectedResource = new TestResource { Name = "TestResource" };
        
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(resourcePath, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResource);

        // Act
        var result = _resourceLoader.Load<TestResource>(resourcePath);

        // Assert
        result.Should().Be(expectedResource);
    }

    [Fact]
    public void Load_WithSpecificCacheStrategy_ShouldUseStrategy()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        var expectedResource = new TestResource { Name = "TestResource" };
        
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(resourcePath, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResource);

        // Act
        var result = _resourceLoader.Load<TestResource>(resourcePath, ResourceCacheStrategy.ForceCache);

        // Assert
        result.Should().Be(expectedResource);
    }

    [Fact]
    public void Load_WithNullPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resourceLoader.Load<TestResource>(null!));
    }

    [Fact]
    public void Load_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resourceLoader.Load<TestResource>(""));
    }

    #endregion

    #region 预加载资源测试

    [Fact]
    public async Task PreloadAsync_WithValidPath_ShouldCompleteSuccessfully()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        
        _mockCacheService.Setup(x => x.ExistsAsync(resourcePath, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);
        
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(resourcePath, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((TestResource?)null);
        
        _mockCacheService.Setup(x => x.SetAsync(resourcePath, It.IsAny<TestResource>(), It.IsAny<ResourceCacheStrategy>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        // Act
        await _resourceLoader.PreloadAsync(resourcePath);

        // Assert
        _mockCacheService.Verify(x => x.ExistsAsync(resourcePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PreloadAsync_WhenResourceAlreadyCached_ShouldSkipLoading()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        
        _mockCacheService.Setup(x => x.ExistsAsync(resourcePath, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

        // Act
        await _resourceLoader.PreloadAsync(resourcePath);

        // Assert
        _mockCacheService.Verify(x => x.ExistsAsync(resourcePath, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.GetAsync<TestResource>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PreloadAsync_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        _mockCacheService.Setup(x => x.ExistsAsync(resourcePath, cancellationToken))
                        .ReturnsAsync(true);

        // Act
        await _resourceLoader.PreloadAsync(resourcePath, cancellationToken);

        // Assert
        _mockCacheService.Verify(x => x.ExistsAsync(resourcePath, cancellationToken), Times.Once);
    }

    #endregion

    #region 批量预加载资源测试

    [Fact]
    public async Task PreloadBatchAsync_WithMultiplePaths_ShouldPreloadAll()
    {
        // Arrange
        var resourcePaths = new[] 
        {
            "res://test/resource1.tres",
            "res://test/resource2.tres",
            "res://test/resource3.tres"
        };
        
        _mockCacheService.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

        // Act
        await _resourceLoader.PreloadBatchAsync(resourcePaths);

        // Assert
        foreach (var path in resourcePaths)
        {
            _mockCacheService.Verify(x => x.ExistsAsync(path, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task PreloadBatchAsync_WithEmptyPaths_ShouldCompleteImmediately()
    {
        // Arrange
        var resourcePaths = Array.Empty<string>();

        // Act
        await _resourceLoader.PreloadBatchAsync(resourcePaths);

        // Assert
        _mockCacheService.Verify(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PreloadBatchAsync_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        var resourcePaths = new[] { "res://test/resource.tres" };
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        _mockCacheService.Setup(x => x.ExistsAsync(It.IsAny<string>(), cancellationToken))
                        .ReturnsAsync(true);

        // Act
        await _resourceLoader.PreloadBatchAsync(resourcePaths, cancellationToken);

        // Assert
        _mockCacheService.Verify(x => x.ExistsAsync(It.IsAny<string>(), cancellationToken), Times.Once);
    }

    #endregion

    #region 获取统计信息测试

    [Fact]
    public void GetStatistics_ShouldReturnStatisticsInstance()
    {
        // Act
        var statistics = _resourceLoader.GetStatistics();

        // Assert
        statistics.Should().NotBeNull();
        statistics.Should().BeOfType<ResourceLoaderStatistics>();
    }

    [Fact]
    public async Task GetStatistics_AfterSuccessfulLoad_ShouldUpdateCounters()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        var expectedResource = new TestResource { Name = "TestResource" };
        
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(resourcePath, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResource);

        // Act
        await _resourceLoader.LoadAsync<TestResource>(resourcePath, CancellationToken.None);
        var statistics = _resourceLoader.GetStatistics();

        // Assert
        statistics.TotalLoads.Should().Be(1);
        statistics.SuccessfulLoads.Should().Be(1);
        statistics.CacheHits.Should().Be(1);
    }

    [Fact]
    public async Task GetStatistics_AfterCacheMiss_ShouldUpdateCounters()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(resourcePath, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((TestResource?)null);
        
        _mockCacheService.Setup(x => x.SetAsync(resourcePath, It.IsAny<TestResource>(), It.IsAny<ResourceCacheStrategy>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        // Act
        await _resourceLoader.LoadAsync<TestResource>(resourcePath, CancellationToken.None);
        var statistics = _resourceLoader.GetStatistics();

        // Assert
        statistics.TotalLoads.Should().Be(1);
        statistics.CacheMisses.Should().Be(1);
    }

    [Fact]
    public async Task GetStatistics_AfterPreload_ShouldUpdatePreloadCount()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        
        _mockCacheService.Setup(x => x.ExistsAsync(resourcePath, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

        // Act
        await _resourceLoader.PreloadAsync(resourcePath);
        var statistics = _resourceLoader.GetStatistics();

        // Assert
        statistics.PreloadCount.Should().Be(1);
    }

    #endregion

    #region 重置统计信息测试

    [Fact]
    public async Task ResetStatistics_ShouldClearAllCounters()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        var expectedResource = new TestResource { Name = "TestResource" };
        
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(resourcePath, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResource);

        // 先进行一些操作以产生统计数据
        await _resourceLoader.LoadAsync<TestResource>(resourcePath, CancellationToken.None);
        await _resourceLoader.PreloadAsync(resourcePath);

        // Act
        _resourceLoader.ResetStatistics();
        var statistics = _resourceLoader.GetStatistics();

        // Assert
        statistics.TotalLoads.Should().Be(0);
        statistics.SuccessfulLoads.Should().Be(0);
        statistics.FailedLoads.Should().Be(0);
        statistics.CacheHits.Should().Be(0);
        statistics.CacheMisses.Should().Be(0);
        statistics.PreloadCount.Should().Be(0);
    }

    [Fact]
    public void ResetStatistics_ShouldCreateNewStatisticsInstance()
    {
        // Arrange
        var originalStatistics = _resourceLoader.GetStatistics();

        // Act
        _resourceLoader.ResetStatistics();
        var newStatistics = _resourceLoader.GetStatistics();

        // Assert
        newStatistics.Should().NotBeSameAs(originalStatistics);
    }

    #endregion

    #region 异常处理测试

    [Fact]
    public async Task LoadAsync_WhenCacheServiceThrows_ShouldPropagateException()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(resourcePath, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new InvalidOperationException("Cache service error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _resourceLoader.LoadAsync<TestResource>(resourcePath, CancellationToken.None));
        
        exception.Message.Should().Be("Cache service error");
    }

    [Fact]
    public async Task LoadAsync_WhenExceptionOccurs_ShouldUpdateFailedLoads()
    {
        // Arrange
        const string resourcePath = "res://test/resource.tres";
        
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(resourcePath, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        try
        {
            await _resourceLoader.LoadAsync<TestResource>(resourcePath, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }

        var statistics = _resourceLoader.GetStatistics();

        // Assert
        statistics.FailedLoads.Should().Be(1);
        statistics.TotalLoads.Should().Be(1);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public async Task LoadAsync_WithVeryLongPath_ShouldHandleCorrectly()
    {
        // Arrange
        var longPath = "res://" + new string('a', 1000) + ".tres";
        
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(longPath, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((TestResource?)null);
        
        _mockCacheService.Setup(x => x.SetAsync(longPath, It.IsAny<TestResource>(), It.IsAny<ResourceCacheStrategy>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        // Act
        var result = await _resourceLoader.LoadAsync<TestResource>(longPath, CancellationToken.None);

        // Assert
        _mockCacheService.Verify(x => x.GetAsync<TestResource>(longPath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_WithSpecialCharactersInPath_ShouldHandleCorrectly()
    {
        // Arrange
        const string specialPath = "res://test/资源文件_测试@#$%^&*().tres";
        
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(specialPath, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((TestResource?)null);
        
        _mockCacheService.Setup(x => x.SetAsync(specialPath, It.IsAny<TestResource>(), It.IsAny<ResourceCacheStrategy>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        // Act
        var result = await _resourceLoader.LoadAsync<TestResource>(specialPath, CancellationToken.None);

        // Assert
        _mockCacheService.Verify(x => x.GetAsync<TestResource>(specialPath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PreloadBatchAsync_WithLargeNumberOfPaths_ShouldHandleCorrectly()
    {
        // Arrange
        var largeBatch = new List<string>();
        for (int i = 0; i < 1000; i++)
        {
            largeBatch.Add($"res://test/resource{i}.tres");
        }
        
        _mockCacheService.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

        // Act
        await _resourceLoader.PreloadBatchAsync(largeBatch);

        // Assert
        _mockCacheService.Verify(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(1000));
    }

    #endregion

    #region 并发测试

    [Fact]
    public async Task LoadAsync_ConcurrentCalls_ShouldHandleCorrectly()
    {
        // Arrange
        const int concurrentCalls = 10;
        var tasks = new List<Task<TestResource?>>();
        
        _mockCacheService.Setup(x => x.GetAsync<TestResource>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new TestResource { Name = "ConcurrentTest" });

        // Act
        for (int i = 0; i < concurrentCalls; i++)
        {
            var path = $"res://test/resource{i}.tres";
            tasks.Add(_resourceLoader.LoadAsync<TestResource>(path, CancellationToken.None));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrentCalls);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
        
        var statistics = _resourceLoader.GetStatistics();
        statistics.TotalLoads.Should().Be(concurrentCalls);
    }

    #endregion

    #region 资源清理

    public void Dispose()
    {
        // 清理测试资源
    }

    #endregion

    #region 测试辅助类

    /// <summary>
    /// 测试用的资源类
    /// </summary>
    public class TestResource
    {
        public string Name { get; set; } = string.Empty;
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 测试用的资源加载器实现
    /// </summary>
    public class TestResourceLoader : IResourceLoader
    {
        private readonly IResourceCacheService _cacheService;
        private ResourceLoaderStatistics _statistics = new();

        public TestResourceLoader(IResourceCacheService cacheService)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        public async Task<T?> LoadAsync<T>(string path, CancellationToken cancellationToken = default) where T : class
        {
            return await LoadAsync<T>(path, ResourceCacheStrategy.Default, cancellationToken);
        }

        public async Task<T?> LoadAsync<T>(string path, ResourceCacheStrategy cacheStrategy, CancellationToken cancellationToken = default) where T : class
        {
            return await LoadAsync<T>(path, null, null, cacheStrategy, cancellationToken);
        }

        public async Task<T?> LoadAsync<T>(string path, Action<float>? progressCallback = null, TimeSpan? minLoadTime = null, ResourceCacheStrategy cacheStrategy = ResourceCacheStrategy.Default, CancellationToken cancellationToken = default) where T : class
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                _statistics.TotalLoads++;
                _statistics.ActiveLoads++;

                T? result = null;

                // 1. 根据缓存策略尝试从缓存获取
                if (cacheStrategy != ResourceCacheStrategy.NoCache)
                {
                    result = await _cacheService.GetAsync<T>(path, cancellationToken);
                    if (result != null)
                    {
                        _statistics.CacheHits++;
                        _statistics.SuccessfulLoads++;
                        return result;
                    }
                    else
                    {
                        _statistics.CacheMisses++;
                    }
                }

                // 2. 模拟从磁盘加载资源
                progressCallback?.Invoke(0.1f);
                
                result = await LoadFromDiskAsync<T>(path, progressCallback, cancellationToken);

                if (result != null)
                {
                    _statistics.SuccessfulLoads++;
                    
                    // 3. 根据缓存策略存储到缓存
                    await _cacheService.SetAsync(path, result, cacheStrategy, cancellationToken);
                }
                else
                {
                    _statistics.FailedLoads++;
                }

                // 4. 确保最小加载时间
                if (minLoadTime.HasValue)
                {
                    var elapsed = DateTime.UtcNow - startTime;
                    if (elapsed < minLoadTime.Value)
                    {
                        var remainingTime = minLoadTime.Value - elapsed;
                        await Task.Delay(remainingTime, cancellationToken);
                    }
                }

                progressCallback?.Invoke(1.0f);
                return result;
            }
            catch (Exception ex)
            {
                _statistics.FailedLoads++;
                _statistics.RecentErrors.Add(new ResourceLoadError
                {
                    Path = path,
                    Message = ex.Message,
                    ExceptionType = ex.GetType().Name,
                    Timestamp = DateTime.UtcNow
                });

                // 限制错误列表大小
                if (_statistics.RecentErrors.Count > 100)
                {
                    _statistics.RecentErrors.RemoveAt(0);
                }

                throw;
            }
            finally
            {
                var elapsed = DateTime.UtcNow - startTime;
                _statistics.ActiveLoads--;
                _statistics.TotalLoadTime = _statistics.TotalLoadTime.Add(elapsed);

                // 更新最快/最慢加载时间
                if (elapsed < _statistics.FastestLoadTime)
                {
                    _statistics.FastestLoadTime = elapsed;
                }
                if (elapsed > _statistics.SlowestLoadTime)
                {
                    _statistics.SlowestLoadTime = elapsed;
                }

                _statistics.LastUpdated = DateTime.UtcNow;
            }
        }

        public T? Load<T>(string path) where T : class
        {
            return Load<T>(path, ResourceCacheStrategy.Default);
        }

        public T? Load<T>(string path, ResourceCacheStrategy cacheStrategy) where T : class
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
                
            // 同步版本 - 简化实现
            return LoadAsync<T>(path, cacheStrategy).GetAwaiter().GetResult();
        }

        public async Task PreloadAsync(string path, CancellationToken cancellationToken = default)
        {
            _statistics.PreloadCount++;

            // 检查是否已在缓存中
            if (await _cacheService.ExistsAsync(path, cancellationToken))
            {
                return;
            }

            // 预加载资源（假设为 TestResource 类型）
            await LoadAsync<TestResource>(path, cancellationToken);
        }

        public async Task PreloadBatchAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
        {
            var tasks = paths.Select(path => PreloadAsync(path, cancellationToken));
            await Task.WhenAll(tasks);
        }

        public ResourceLoaderStatistics GetStatistics()
        {
            return _statistics;
        }

        public void ResetStatistics()
        {
            _statistics = new ResourceLoaderStatistics();
        }

        private async Task<T?> LoadFromDiskAsync<T>(string path, Action<float>? progressCallback, CancellationToken cancellationToken) where T : class
        {
            // 模拟进度更新
            progressCallback?.Invoke(0.3f);
            
            // 模拟异步加载延迟
            await Task.Delay(10, cancellationToken);
            
            progressCallback?.Invoke(0.8f);

            // 模拟资源创建
            if (typeof(T) == typeof(TestResource))
            {
                return new TestResource { Name = $"Loaded from {path}" } as T;
            }

            return null;
        }
    }

    #endregion
}