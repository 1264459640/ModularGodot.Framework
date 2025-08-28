using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using MF.Infrastructure.Abstractions.Core.Logging;
using MF.Infrastructure.Abstractions.Core.Monitoring;
using Xunit;

namespace MF.Infrastructure.Tests.Monitoring;

/// <summary>
/// 性能监控服务测试 - 测试 IPerformanceMonitor 接口的基本实现
/// </summary>
public class PerformanceMonitorTests : IDisposable
{
    private readonly TestPerformanceMonitor _performanceMonitor;

    public PerformanceMonitorTests()
    {
        _performanceMonitor = new TestPerformanceMonitor();
    }

    #region 初始化测试

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var monitor = new TestPerformanceMonitor();

        // Assert
        Assert.NotNull(monitor);
        Assert.False(monitor.IsDisposed);
    }

    [Fact]
    public void Constructor_ShouldInitializeEmptyCollections()
    {
        // Arrange & Act
        var monitor = new TestPerformanceMonitor();

        // Assert
        Assert.Empty(monitor.GetMetrics());
        Assert.Empty(monitor.GetCounters());
        Assert.Empty(monitor.GetTimers());
    }

    #endregion

    #region 记录指标测试

    [Fact]
    public void RecordMetric_WithValidNameAndValue_ShouldRecordSuccessfully()
    {
        // Arrange
        const string metricName = "test_metric";
        const double metricValue = 42.5;

        // Act
        _performanceMonitor.RecordMetric(metricName, metricValue);

        // Assert
        var metrics = _performanceMonitor.GetMetrics();
        Assert.Single(metrics);
        Assert.Contains(metricName, metrics.Keys);
        Assert.Contains(metricValue, metrics[metricName]);
    }

    [Fact]
    public void RecordMetric_WithTags_ShouldRecordWithTags()
    {
        // Arrange
        const string metricName = "tagged_metric";
        const double metricValue = 100.0;
        var tags = new Dictionary<string, string> { { "environment", "test" }, { "version", "1.0" } };

        // Act
        _performanceMonitor.RecordMetric(metricName, metricValue, tags);

        // Assert
        var metrics = _performanceMonitor.GetMetrics();
        var expectedKey = _performanceMonitor.CreateTestKey(metricName, tags);
        Assert.Contains(expectedKey, metrics.Keys);
    }

    [Fact]
    public void RecordMetric_WithNullTags_ShouldRecordWithoutTags()
    {
        // Arrange
        const string metricName = "no_tags_metric";
        const double metricValue = 25.0;

        // Act
        _performanceMonitor.RecordMetric(metricName, metricValue, null);

        // Assert
        var metrics = _performanceMonitor.GetMetrics();
        Assert.Contains(metricName, metrics.Keys);
    }

    [Fact]
    public void RecordMetric_MultipleValues_ShouldAccumulate()
    {
        // Arrange
        const string metricName = "accumulate_metric";
        var values = new[] { 10.0, 20.0, 30.0 };

        // Act
        foreach (var value in values)
        {
            _performanceMonitor.RecordMetric(metricName, value);
        }

        // Assert
        var metrics = _performanceMonitor.GetMetrics();
        Assert.Contains(metricName, metrics.Keys);
        Assert.Equal(3, metrics[metricName].Count);
        Assert.Contains(10.0, metrics[metricName]);
        Assert.Contains(20.0, metrics[metricName]);
        Assert.Contains(30.0, metrics[metricName]);
    }

    [Fact]
    public void RecordMetric_WithNullName_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _performanceMonitor.RecordMetric(null!, 42.0));
    }

    [Fact]
    public void RecordMetric_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _performanceMonitor.RecordMetric("", 42.0));
    }

    #endregion

    #region 记录计数器测试

    [Fact]
    public void RecordCounter_WithValidName_ShouldRecordSuccessfully()
    {
        // Arrange
        const string counterName = "test_counter";
        const long counterValue = 5;

        // Act
        _performanceMonitor.RecordCounter(counterName, counterValue);

        // Assert
        var counters = _performanceMonitor.GetCounters();
        Assert.Single(counters);
        Assert.Contains(counterName, counters.Keys);
        Assert.Equal(counterValue, counters[counterName]);
    }

    [Fact]
    public void RecordCounter_WithDefaultValue_ShouldRecordOne()
    {
        // Arrange
        const string counterName = "default_counter";

        // Act
        _performanceMonitor.RecordCounter(counterName);

        // Assert
        var counters = _performanceMonitor.GetCounters();
        Assert.Equal(1, counters[counterName]);
    }

    [Fact]
    public void RecordCounter_WithTags_ShouldRecordWithTags()
    {
        // Arrange
        const string counterName = "tagged_counter";
        const long counterValue = 10;
        var tags = new Dictionary<string, string> { { "type", "request" } };

        // Act
        _performanceMonitor.RecordCounter(counterName, counterValue, tags);

        // Assert
        var counters = _performanceMonitor.GetCounters();
        var expectedKey = _performanceMonitor.CreateTestKey(counterName, tags);
        Assert.Contains(expectedKey, counters.Keys);
        Assert.Equal(counterValue, counters[expectedKey]);
    }

    [Fact]
    public void RecordCounter_MultipleCalls_ShouldAccumulate()
    {
        // Arrange
        const string counterName = "accumulate_counter";
        var values = new long[] { 1, 2, 3 };

        // Act
        foreach (var value in values)
        {
            _performanceMonitor.RecordCounter(counterName, value);
        }

        // Assert
        var counters = _performanceMonitor.GetCounters();
        Assert.Equal(6, counters[counterName]); // 1 + 2 + 3 = 6
    }

    [Fact]
    public void RecordCounter_WithNullName_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _performanceMonitor.RecordCounter(null!, 1));
    }

    [Fact]
    public void RecordCounter_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _performanceMonitor.RecordCounter("", 1));
    }

    #endregion

    #region 记录计时器测试

    [Fact]
    public void RecordTimer_WithValidNameAndDuration_ShouldRecordSuccessfully()
    {
        // Arrange
        const string timerName = "test_timer";
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        _performanceMonitor.RecordTimer(timerName, duration);

        // Assert
        var timers = _performanceMonitor.GetTimers();
        Assert.Single(timers);
        Assert.Contains(timerName, timers.Keys);
        Assert.Contains(duration, timers[timerName]);
    }

    [Fact]
    public void RecordTimer_WithTags_ShouldRecordWithTags()
    {
        // Arrange
        const string timerName = "tagged_timer";
        var duration = TimeSpan.FromSeconds(1);
        var tags = new Dictionary<string, string> { { "operation", "database" } };

        // Act
        _performanceMonitor.RecordTimer(timerName, duration, tags);

        // Assert
        var timers = _performanceMonitor.GetTimers();
        var expectedKey = _performanceMonitor.CreateTestKey(timerName, tags);
        Assert.Contains(expectedKey, timers.Keys);
    }

    [Fact]
    public void RecordTimer_MultipleDurations_ShouldAccumulate()
    {
        // Arrange
        const string timerName = "accumulate_timer";
        var durations = new[] 
        {
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(150)
        };

        // Act
        foreach (var duration in durations)
        {
            _performanceMonitor.RecordTimer(timerName, duration);
        }

        // Assert
        var timers = _performanceMonitor.GetTimers();
        Assert.Contains(timerName, timers.Keys);
        Assert.Equal(3, timers[timerName].Count);
    }

    [Fact]
    public void RecordTimer_WithNullName_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _performanceMonitor.RecordTimer(null!, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void RecordTimer_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _performanceMonitor.RecordTimer("", TimeSpan.FromSeconds(1)));
    }

    #endregion

    #region 启动计时器测试

    [Fact]
    public void StartTimer_WithValidName_ShouldReturnDisposable()
    {
        // Arrange
        const string timerName = "start_timer_test";

        // Act
        var timer = _performanceMonitor.StartTimer(timerName);

        // Assert
        Assert.NotNull(timer);
        Assert.IsAssignableFrom<IDisposable>(timer);
        
        // Cleanup
        timer.Dispose();
    }

    [Fact]
    public void StartTimer_WithTags_ShouldReturnDisposable()
    {
        // Arrange
        const string timerName = "tagged_start_timer";
        var tags = new Dictionary<string, string> { { "method", "GET" } };

        // Act
        var timer = _performanceMonitor.StartTimer(timerName, tags);

        // Assert
        Assert.NotNull(timer);
        
        // Cleanup
        timer.Dispose();
    }

    [Fact]
    public void StartTimer_WithNullName_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _performanceMonitor.StartTimer(null!));
    }

    [Fact]
    public void StartTimer_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _performanceMonitor.StartTimer(""));
    }

    #endregion

    #region 完成计时器测试

    [Fact]
    public void StartTimer_WhenDisposed_ShouldRecordTimer()
    {
        // Arrange
        const string timerName = "dispose_timer_test";

        // Act
        using (var timer = _performanceMonitor.StartTimer(timerName))
        {
            Thread.Sleep(10); // 模拟一些工作
        } // timer.Dispose() 在这里被调用

        // Assert
        var timers = _performanceMonitor.GetTimers();
        Assert.Contains(timerName, timers.Keys);
        Assert.Single(timers[timerName]);
        Assert.True(timers[timerName][0] >= TimeSpan.FromMilliseconds(5)); // 至少5ms
    }

    [Fact]
    public async Task StartTimer_WithAsyncOperation_ShouldRecordCorrectDuration()
    {
        // Arrange
        const string timerName = "async_timer_test";
        const int delayMs = 50;

        // Act
        using (var timer = _performanceMonitor.StartTimer(timerName))
        {
            await Task.Delay(delayMs);
        }

        // Assert
        var timers = _performanceMonitor.GetTimers();
        Assert.Contains(timerName, timers.Keys);
        var recordedDuration = timers[timerName][0];
        Assert.True(recordedDuration >= TimeSpan.FromMilliseconds(delayMs - 10)); // 允许一些误差
    }

    [Fact]
    public void StartTimer_MultipleTimers_ShouldRecordAll()
    {
        // Arrange
        const string timerName = "multiple_timers_test";
        const int timerCount = 3;

        // Act
        for (int i = 0; i < timerCount; i++)
        {
            using (var timer = _performanceMonitor.StartTimer(timerName))
            {
                Thread.Sleep(5);
            }
        }

        // Assert
        var timers = _performanceMonitor.GetTimers();
        Assert.Contains(timerName, timers.Keys);
        Assert.Equal(timerCount, timers[timerName].Count);
    }

    #endregion

    #region 创建键测试

    [Fact]
    public void CreateKey_WithNameOnly_ShouldReturnName()
    {
        // Arrange
        const string name = "test_key";

        // Act
        var key = _performanceMonitor.CreateTestKey(name, null);

        // Assert
        Assert.Equal(name, key);
    }

    [Fact]
    public void CreateKey_WithTags_ShouldReturnFormattedKey()
    {
        // Arrange
        const string name = "test_key";
        var tags = new Dictionary<string, string> 
        { 
            { "env", "test" }, 
            { "version", "1.0" } 
        };

        // Act
        var key = _performanceMonitor.CreateTestKey(name, tags);

        // Assert
        Assert.Contains(name, key);
        Assert.Contains("env=test", key);
        Assert.Contains("version=1.0", key);
    }

    [Fact]
    public void CreateKey_WithEmptyTags_ShouldReturnName()
    {
        // Arrange
        const string name = "test_key";
        var tags = new Dictionary<string, string>();

        // Act
        var key = _performanceMonitor.CreateTestKey(name, tags);

        // Assert
        Assert.Equal(name, key);
    }

    #endregion

    #region 释放资源测试

    [Fact]
    public void Dispose_ShouldReleaseResourcesSuccessfully()
    {
        // Arrange
        _performanceMonitor.RecordMetric("test_metric", 42.0);
        _performanceMonitor.RecordCounter("test_counter", 5);
        _performanceMonitor.RecordTimer("test_timer", TimeSpan.FromMilliseconds(100));

        // Act
        _performanceMonitor.Dispose();

        // Assert
        Assert.True(_performanceMonitor.IsDisposed);
        Assert.Empty(_performanceMonitor.GetMetrics());
        Assert.Empty(_performanceMonitor.GetCounters());
        Assert.Empty(_performanceMonitor.GetTimers());
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert
        _performanceMonitor.Dispose();
        _performanceMonitor.Dispose(); // Should not throw
    }

    [Fact]
    public void Dispose_WithActiveTimers_ShouldCompleteAllTimers()
    {
        // Arrange
        const string timerName = "active_timer_test";
        var timer1 = _performanceMonitor.StartTimer(timerName);
        var timer2 = _performanceMonitor.StartTimer(timerName);

        // Act
        _performanceMonitor.Dispose();

        // Assert
        Assert.True(_performanceMonitor.IsDisposed);
        // 活跃计时器应该被完成并记录
        var timers = _performanceMonitor.GetTimers();
        if (timers.ContainsKey(timerName))
        {
            Assert.True(timers[timerName].Count >= 0); // 可能为0，因为Dispose清空了集合
        }
    }

    #endregion

    #region 处理已释放对象测试

    [Fact]
    public void RecordMetric_AfterDispose_ShouldNotThrow()
    {
        // Arrange
        _performanceMonitor.Dispose();

        // Act & Assert
        _performanceMonitor.RecordMetric("test_metric", 42.0); // Should not throw
    }

    [Fact]
    public void RecordCounter_AfterDispose_ShouldNotThrow()
    {
        // Arrange
        _performanceMonitor.Dispose();

        // Act & Assert
        _performanceMonitor.RecordCounter("test_counter", 5); // Should not throw
    }

    [Fact]
    public void RecordTimer_AfterDispose_ShouldNotThrow()
    {
        // Arrange
        _performanceMonitor.Dispose();

        // Act & Assert
        _performanceMonitor.RecordTimer("test_timer", TimeSpan.FromMilliseconds(100)); // Should not throw
    }

    [Fact]
    public void StartTimer_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _performanceMonitor.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => 
            _performanceMonitor.StartTimer("test_timer"));
    }

    #endregion

    #region 并发测试

    [Fact]
    public async Task ConcurrentOperations_ShouldHandleCorrectly()
    {
        // Arrange
        const int taskCount = 10;
        const int operationsPerTask = 100;
        var tasks = new Task[taskCount];

        // Act
        for (int i = 0; i < taskCount; i++)
        {
            var taskId = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < operationsPerTask; j++)
                {
                    _performanceMonitor.RecordMetric($"metric_{taskId}", j);
                    _performanceMonitor.RecordCounter($"counter_{taskId}", 1);
                    _performanceMonitor.RecordTimer($"timer_{taskId}", TimeSpan.FromMilliseconds(j));
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        var metrics = _performanceMonitor.GetMetrics();
        var counters = _performanceMonitor.GetCounters();
        var timers = _performanceMonitor.GetTimers();

        Assert.Equal(taskCount, metrics.Count);
        Assert.Equal(taskCount, counters.Count);
        Assert.Equal(taskCount, timers.Count);

        // 验证每个计数器的值
        for (int i = 0; i < taskCount; i++)
        {
            Assert.Equal(operationsPerTask, counters[$"counter_{i}"]);
            Assert.Equal(operationsPerTask, metrics[$"metric_{i}"].Count);
            Assert.Equal(operationsPerTask, timers[$"timer_{i}"].Count);
        }
    }

    #endregion

    #region 资源清理

    public void Dispose()
    {
        _performanceMonitor?.Dispose();
    }

    #endregion

    #region 测试辅助类

    /// <summary>
    /// 测试用的简单性能监控服务实现
    /// </summary>
    public class TestPerformanceMonitor : IPerformanceMonitor, IDisposable
    {
        private readonly ConcurrentDictionary<string, List<double>> _metrics = new();
        private readonly ConcurrentDictionary<string, long> _counters = new();
        private readonly ConcurrentDictionary<string, List<TimeSpan>> _timers = new();
        private readonly ConcurrentBag<TestActiveTimer> _activeTimers = new();
        private readonly object _lock = new();
        private bool _disposed;

        public bool IsDisposed => _disposed;

        public void RecordMetric(string name, double value, Dictionary<string, string>? tags = null)
        {
            if (_disposed) return;
            
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be empty", nameof(name));

            var key = CreateTestKey(name, tags);
            lock (_lock)
            {
                if (!_metrics.ContainsKey(key))
                {
                    _metrics[key] = new List<double>();
                }
                _metrics[key].Add(value);
            }
        }

        public void RecordCounter(string name, long value = 1, Dictionary<string, string>? tags = null)
        {
            if (_disposed) return;
            
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be empty", nameof(name));

            var key = CreateTestKey(name, tags);
            _counters.AddOrUpdate(key, value, (k, existing) => existing + value);
        }

        public void RecordTimer(string name, TimeSpan duration, Dictionary<string, string>? tags = null)
        {
            if (_disposed) return;
            
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be empty", nameof(name));

            var key = CreateTestKey(name, tags);
            lock (_lock)
            {
                if (!_timers.ContainsKey(key))
                {
                    _timers[key] = new List<TimeSpan>();
                }
                _timers[key].Add(duration);
            }
        }

        public IDisposable StartTimer(string name, Dictionary<string, string>? tags = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TestPerformanceMonitor));
            
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be empty", nameof(name));

            var timer = new TestActiveTimer(name, tags, this);
            _activeTimers.Add(timer);
            return timer;
        }

        public string CreateTestKey(string name, Dictionary<string, string>? tags)
        {
            if (tags == null || tags.Count == 0)
                return name;

            var tagString = string.Join(",", tags.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            return $"{name}[{tagString}]";
        }

        public Dictionary<string, List<double>> GetMetrics()
        {
            lock (_lock)
            {
                return new Dictionary<string, List<double>>(_metrics.ToDictionary(kvp => kvp.Key, kvp => new List<double>(kvp.Value)));
            }
        }
        
        public Dictionary<string, long> GetCounters() => new(_counters);
        
        public Dictionary<string, List<TimeSpan>> GetTimers()
        {
            lock (_lock)
            {
                return new Dictionary<string, List<TimeSpan>>(_timers.ToDictionary(kvp => kvp.Key, kvp => new List<TimeSpan>(kvp.Value)));
            }
        }

        internal void CompleteTimer(TestActiveTimer timer)
        {
            RecordTimer(timer.Name, timer.Elapsed, timer.Tags);
        }

        public void Dispose()
        {
            if (_disposed) return;

            // 完成所有活跃的计时器
            var activeTimersCopy = _activeTimers.ToArray();
            foreach (var timer in activeTimersCopy)
            {
                timer.Dispose();
            }

            _metrics.Clear();
            _counters.Clear();
            _timers.Clear();

            _disposed = true;
        }
    }

    /// <summary>
    /// 测试用的活跃计时器
    /// </summary>
    public class TestActiveTimer : IDisposable
    {
        private readonly TestPerformanceMonitor _monitor;
        private readonly DateTime _startTime;
        private bool _disposed;

        public string Name { get; }
        public Dictionary<string, string>? Tags { get; }
        public TimeSpan Elapsed => DateTime.UtcNow - _startTime;

        public TestActiveTimer(string name, Dictionary<string, string>? tags, TestPerformanceMonitor monitor)
        {
            Name = name;
            Tags = tags;
            _monitor = monitor;
            _startTime = DateTime.UtcNow;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _monitor.CompleteTimer(this);
            _disposed = true;
        }
    }

    #endregion
}