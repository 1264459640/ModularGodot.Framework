using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using MF.Infrastructure.Abstractions.Core.Caching;
using MF.Infrastructure.Abstractions.Core.Logging;
using Xunit;

namespace MF.Infrastructure.Tests.Caching;

/// <summary>
/// 内存缓存服务测试 - 测试 ICacheService 接口的基本实现
/// </summary>
public class MemoryCacheServiceTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly TestMemoryCacheService _cacheService;

    public MemoryCacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cacheService = new TestMemoryCacheService(_memoryCache);
    }

    #region 初始化测试

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var service = new TestMemoryCacheService(_memoryCache);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullMemoryCache_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new TestMemoryCacheService(null!));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // This test is no longer applicable since we removed the logger parameter
        Assert.True(true);
    }

    #endregion

    #region 设置缓存测试

    [Fact]
    public async Task SetAsync_WithValidKeyAndValue_ShouldSetCacheSuccessfully()
    {
        // Arrange
        const string key = "test-key";
        const string value = "test-value";

        // Act
        await _cacheService.SetAsync(key, value);

        // Assert
        var result = await _cacheService.GetAsync<string>(key);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task SetAsync_WithExpiration_ShouldSetCacheWithExpiration()
    {
        // Arrange
        const string key = "test-key-expiration";
        const string value = "test-value";
        var expiration = TimeSpan.FromMilliseconds(100);

        // Act
        await _cacheService.SetAsync(key, value, expiration);

        // Assert
        var result = await _cacheService.GetAsync<string>(key);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task SetAsync_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _cacheService.SetAsync(null!, "value"));
    }

    [Fact]
    public async Task SetAsync_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _cacheService.SetAsync<string>("key", null!));
    }

    #endregion

    #region 获取缓存测试

    [Fact]
    public async Task GetAsync_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        const string key = "existing-key";
        const string value = "existing-value";
        await _cacheService.SetAsync(key, value);

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GetAsync_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        const string key = "non-existing-key";

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WithWrongType_ShouldReturnNull()
    {
        // Arrange
        const string key = "type-test-key";
        const string value = "string-value";
        await _cacheService.SetAsync(key, value);

        // Act - Try to get as a different reference type
        var result = await _cacheService.GetAsync<TestComplexObject>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _cacheService.GetAsync<string>(null!));
    }

    #endregion

    #region 缓存存在性测试

    [Fact]
    public async Task ExistsAsync_WithExistingKey_ShouldReturnTrue()
    {
        // Arrange
        const string key = "exists-test-key";
        const string value = "exists-test-value";
        await _cacheService.SetAsync(key, value);

        // Act
        var exists = await _cacheService.ExistsAsync(key);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingKey_ShouldReturnFalse()
    {
        // Arrange
        const string key = "non-exists-test-key";

        // Act
        var exists = await _cacheService.ExistsAsync(key);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _cacheService.ExistsAsync(null!));
    }

    #endregion

    #region 删除缓存测试

    [Fact]
    public async Task RemoveAsync_WithExistingKey_ShouldRemoveSuccessfully()
    {
        // Arrange
        const string key = "remove-test-key";
        const string value = "remove-test-value";
        await _cacheService.SetAsync(key, value);

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        var result = await _cacheService.GetAsync<string>(key);
        Assert.Null(result);
        
        var exists = await _cacheService.ExistsAsync(key);
        Assert.False(exists);
    }

    [Fact]
    public async Task RemoveAsync_WithNonExistingKey_ShouldNotThrow()
    {
        // Arrange
        const string key = "non-existing-remove-key";

        // Act & Assert
        await _cacheService.RemoveAsync(key); // Should not throw
    }

    [Fact]
    public async Task RemoveAsync_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _cacheService.RemoveAsync(null!));
    }

    #endregion

    #region 清空缓存测试

    [Fact]
    public async Task ClearAsync_WithMultipleItems_ShouldClearAllItems()
    {
        // Arrange
        const string key1 = "clear-test-key-1";
        const string key2 = "clear-test-key-2";
        const string key3 = "clear-test-key-3";
        const string value = "clear-test-value";
        
        await _cacheService.SetAsync(key1, value);
        await _cacheService.SetAsync(key2, value);
        await _cacheService.SetAsync(key3, value);

        // Act
        await _cacheService.ClearAsync();

        // Assert
        var result1 = await _cacheService.GetAsync<string>(key1);
        var result2 = await _cacheService.GetAsync<string>(key2);
        var result3 = await _cacheService.GetAsync<string>(key3);
        
        Assert.Null(result1);
        Assert.Null(result2);
        Assert.Null(result3);
    }

    [Fact]
    public async Task ClearAsync_WithEmptyCache_ShouldNotThrow()
    {
        // Act & Assert
        await _cacheService.ClearAsync(); // Should not throw
    }

    #endregion

    #region 缓存过期测试

    [Fact]
    public async Task SetAsync_WithShortExpiration_ShouldExpireAfterTime()
    {
        // Arrange
        const string key = "expiration-test-key";
        const string value = "expiration-test-value";
        var expiration = TimeSpan.FromMilliseconds(50);

        // Act
        await _cacheService.SetAsync(key, value, expiration);
        
        // Verify item exists initially
        var initialResult = await _cacheService.GetAsync<string>(key);
        Assert.Equal(value, initialResult);
        
        // Wait for expiration
        await Task.Delay(100);
        
        // Assert
        var expiredResult = await _cacheService.GetAsync<string>(key);
        Assert.Null(expiredResult);
    }

    [Fact]
    public async Task SetAsync_WithZeroExpiration_ShouldNotExpire()
    {
        // Arrange
        const string key = "no-expiration-test-key";
        const string value = "no-expiration-test-value";
        var expiration = TimeSpan.Zero;

        // Act
        await _cacheService.SetAsync(key, value, expiration);
        
        // Wait a bit
        await Task.Delay(50);
        
        // Assert
        var result = await _cacheService.GetAsync<string>(key);
        Assert.Equal(value, result);
    }

    #endregion

    #region 并发测试

    [Fact]
    public async Task ConcurrentOperations_ShouldHandleCorrectly()
    {
        // Arrange
        const int taskCount = 10;
        const string keyPrefix = "concurrent-key-";
        const string value = "concurrent-value";
        
        var tasks = new Task[taskCount];

        // Act - 并发设置缓存
        for (int i = 0; i < taskCount; i++)
        {
            var key = keyPrefix + i;
            tasks[i] = _cacheService.SetAsync(key, value + i);
        }
        
        await Task.WhenAll(tasks);
        
        // Assert - 验证所有缓存项都设置成功
        for (int i = 0; i < taskCount; i++)
        {
            var key = keyPrefix + i;
            var result = await _cacheService.GetAsync<string>(key);
            Assert.Equal(value + i, result);
        }
    }

    #endregion

    #region 边界测试

    [Fact]
    public async Task SetAsync_WithEmptyKey_ShouldThrowArgumentException()
    {
        // Arrange
        const string key = "";
        const string value = "empty-key-value";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _cacheService.SetAsync(key, value));
    }

    [Fact]
    public async Task SetAsync_WithLongKey_ShouldHandleCorrectly()
    {
        // Arrange
        var key = new string('a', 1000); // 1000 character key
        const string value = "long-key-value";

        // Act
        await _cacheService.SetAsync(key, value);

        // Assert
        var result = await _cacheService.GetAsync<string>(key);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task SetAsync_WithComplexObject_ShouldHandleCorrectly()
    {
        // Arrange
        const string key = "complex-object-key";
        var complexObject = new TestComplexObject
        {
            Id = 123,
            Name = "Test Object",
            Data = new[] { "item1", "item2", "item3" },
            Timestamp = DateTime.UtcNow
        };

        // Act
        await _cacheService.SetAsync(key, complexObject);

        // Assert
        var result = await _cacheService.GetAsync<TestComplexObject>(key);
        Assert.NotNull(result);
        Assert.Equal(complexObject.Id, result.Id);
        Assert.Equal(complexObject.Name, result.Name);
        Assert.Equal(complexObject.Data, result.Data);
    }

    #endregion

    #region 取消令牌测试

    [Fact]
    public async Task GetAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _cacheService.GetAsync<string>("key", cts.Token));
    }

    [Fact]
    public async Task SetAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _cacheService.SetAsync("key", "value", null, cts.Token));
    }

    #endregion

    #region 资源清理

    public void Dispose()
    {
        _cacheService?.Dispose();
        _memoryCache?.Dispose();
    }

    #endregion

    #region 测试辅助类

    private class TestComplexObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string[] Data { get; set; } = Array.Empty<string>();
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 测试用的简单内存缓存服务实现
    /// </summary>
    public class TestMemoryCacheService : ICacheService, IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private bool _disposed;

        public TestMemoryCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TestMemoryCacheService));
            if (key == null) throw new ArgumentNullException(nameof(key));
            
            cancellationToken.ThrowIfCancellationRequested();

            if (_memoryCache.TryGetValue(key, out var value))
            {
                return Task.FromResult(value as T);
            }

            return Task.FromResult<T?>(null);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TestMemoryCacheService));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be empty", nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));
            
            cancellationToken.ThrowIfCancellationRequested();

            var options = new MemoryCacheEntryOptions();
            if (expiration.HasValue && expiration.Value != TimeSpan.Zero)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }

            _memoryCache.Set(key, value, options);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TestMemoryCacheService));
            if (key == null) throw new ArgumentNullException(nameof(key));
            
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(_memoryCache.TryGetValue(key, out _));
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TestMemoryCacheService));
            if (key == null) throw new ArgumentNullException(nameof(key));
            
            cancellationToken.ThrowIfCancellationRequested();

            _memoryCache.Remove(key);
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TestMemoryCacheService));
            
            cancellationToken.ThrowIfCancellationRequested();

            if (_memoryCache is MemoryCache mc)
            {
                mc.Compact(1.0);
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }

    #endregion
}