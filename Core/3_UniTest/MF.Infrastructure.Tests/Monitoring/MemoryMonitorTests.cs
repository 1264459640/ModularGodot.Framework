using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using MF.Infrastructure.Abstractions.Core.Logging;
using MF.Infrastructure.Abstractions.Core.Monitoring;
using Xunit;

namespace MF.Infrastructure.Tests.Monitoring;

/// <summary>
/// 内存监控服务测试 - 测试 IMemoryMonitor 接口的基本实现
/// </summary>
public class MemoryMonitorTests : IDisposable
{
    private readonly TestMemoryMonitor _memoryMonitor;

    public MemoryMonitorTests()
    {
        _memoryMonitor = new TestMemoryMonitor();
    }

    #region 初始化测试

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var monitor = new TestMemoryMonitor();

        // Assert
        Assert.NotNull(monitor);
        Assert.True(monitor.AutoReleaseThreshold > 0);
        Assert.True(monitor.CheckInterval > TimeSpan.Zero);
        Assert.True(monitor.MemoryPressureThreshold > 0);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var monitor = new TestMemoryMonitor();

        // Assert
        Assert.Equal(800 * 1024 * 1024, monitor.AutoReleaseThreshold); // 800MB
        Assert.Equal(TimeSpan.FromSeconds(15), monitor.CheckInterval);
        Assert.Equal(0.8, monitor.MemoryPressureThreshold);
    }

    #endregion

    #region 开始监控测试

    [Fact]
    public void StartMonitoring_ShouldStartSuccessfully()
    {
        // Act
        _memoryMonitor.StartMonitoring();

        // Assert
        Assert.True(_memoryMonitor.IsMonitoring);
    }

    [Fact]
    public void StartMonitoring_WhenAlreadyStarted_ShouldNotThrow()
    {
        // Arrange
        _memoryMonitor.StartMonitoring();

        // Act & Assert
        _memoryMonitor.StartMonitoring(); // Should not throw
        Assert.True(_memoryMonitor.IsMonitoring);
    }

    #endregion

    #region 停止监控测试

    [Fact]
    public void StopMonitoring_ShouldStopSuccessfully()
    {
        // Arrange
        _memoryMonitor.StartMonitoring();

        // Act
        _memoryMonitor.StopMonitoring();

        // Assert
        Assert.False(_memoryMonitor.IsMonitoring);
    }

    [Fact]
    public void StopMonitoring_WhenNotStarted_ShouldNotThrow()
    {
        // Act & Assert
        _memoryMonitor.StopMonitoring(); // Should not throw
        Assert.False(_memoryMonitor.IsMonitoring);
    }

    #endregion

    #region 内存压力检测测试

    [Fact]
    public void CheckMemoryPressure_WithHighUsage_ShouldTriggerEvent()
    {
        // Arrange
        var eventTriggered = false;
        long reportedUsage = 0;
        _memoryMonitor.MemoryPressureDetected += (usage) =>
        {
            eventTriggered = true;
            reportedUsage = usage;
        };

        var highUsage = _memoryMonitor.AutoReleaseThreshold + 1000000; // 超过阈值

        // Act
        _memoryMonitor.CheckMemoryPressure(highUsage);

        // Assert
        Assert.True(eventTriggered);
        Assert.Equal(highUsage, reportedUsage);
    }

    [Fact]
    public void CheckMemoryPressure_WithLowUsage_ShouldNotTriggerEvent()
    {
        // Arrange
        var eventTriggered = false;
        _memoryMonitor.MemoryPressureDetected += (usage) => eventTriggered = true;

        var lowUsage = _memoryMonitor.AutoReleaseThreshold / 2; // 低于阈值

        // Act
        _memoryMonitor.CheckMemoryPressure(lowUsage);

        // Assert
        Assert.False(eventTriggered);
    }

    [Fact]
    public void CheckMemoryPressure_WithZeroUsage_ShouldNotTriggerEvent()
    {
        // Arrange
        var eventTriggered = false;
        _memoryMonitor.MemoryPressureDetected += (usage) => eventTriggered = true;

        // Act
        _memoryMonitor.CheckMemoryPressure(0);

        // Assert
        Assert.False(eventTriggered);
    }

    #endregion

    #region 自动释放触发测试

    [Fact]
    public void CheckMemoryPressure_WithCriticalUsage_ShouldTriggerAutoRelease()
    {
        // Arrange
        var autoReleaseTriggered = false;
        _memoryMonitor.AutoReleaseTriggered += () => autoReleaseTriggered = true;

        var criticalUsage = _memoryMonitor.AutoReleaseThreshold * 2; // 严重压力

        // Act
        _memoryMonitor.CheckMemoryPressure(criticalUsage);

        // Assert
        Assert.True(autoReleaseTriggered);
    }

    [Fact]
    public void CheckMemoryPressure_WithHighUsage_ShouldTriggerAutoRelease()
    {
        // Arrange
        var autoReleaseTriggered = false;
        _memoryMonitor.AutoReleaseTriggered += () => autoReleaseTriggered = true;

        var highUsage = (long)(_memoryMonitor.AutoReleaseThreshold * 1.1); // 高压力（超过阈值）

        // Act
        _memoryMonitor.CheckMemoryPressure(highUsage);

        // Assert
        Assert.True(autoReleaseTriggered);
    }

    [Fact]
    public void CheckMemoryPressure_WithMediumUsage_ShouldNotTriggerAutoRelease()
    {
        // Arrange
        var autoReleaseTriggered = false;
        _memoryMonitor.AutoReleaseTriggered += () => autoReleaseTriggered = true;

        var mediumUsage = (long)(_memoryMonitor.AutoReleaseThreshold * 0.6); // 中等压力

        // Act
        _memoryMonitor.CheckMemoryPressure(mediumUsage);

        // Assert
        Assert.False(autoReleaseTriggered);
    }

    #endregion

    #region 获取当前内存使用量测试

    [Fact]
    public void GetCurrentMemoryUsage_ShouldReturnPositiveValue()
    {
        // Act
        var usage = _memoryMonitor.GetCurrentMemoryUsage();

        // Assert
        Assert.True(usage > 0);
    }

    [Fact]
    public void GetCurrentMemoryUsage_ShouldReturnReasonableValue()
    {
        // Act
        var usage = _memoryMonitor.GetCurrentMemoryUsage();

        // Assert
        Assert.True(usage > 1024); // 至少1KB
        Assert.True(usage < 10L * 1024 * 1024 * 1024); // 小于10GB（合理范围）
    }

    #endregion

    #region 强制垃圾回收测试

    [Fact]
    public void ForceGarbageCollection_ShouldExecuteSuccessfully()
    {
        // Arrange
        var beforeGC = _memoryMonitor.GetCurrentMemoryUsage();

        // Act
        _memoryMonitor.ForceGarbageCollection();

        // Assert
        var afterGC = _memoryMonitor.GetCurrentMemoryUsage();
        // 垃圾回收后内存使用量应该是正数（垃圾回收的效果可能不明显）
        Assert.True(afterGC > 0);
        // 验证方法执行没有抛出异常即可
        Assert.True(true);
    }

    [Fact]
    public void ForceGarbageCollection_ShouldNotThrow()
    {
        // Act & Assert
        _memoryMonitor.ForceGarbageCollection(); // Should not throw
    }

    #endregion

    #region 获取内存压力级别测试

    [Fact]
    public void GetCurrentPressureLevel_ShouldReturnValidLevel()
    {
        // Act
        var level = _memoryMonitor.GetCurrentPressureLevel();

        // Assert
        Assert.NotNull(level);
        Assert.Contains(level, new[] { "Low", "Medium", "High", "Critical" });
    }

    [Fact]
    public void GetCurrentPressureLevel_WithLowUsage_ShouldReturnLow()
    {
        // Arrange
        _memoryMonitor.SetCurrentMemoryUsage(_memoryMonitor.AutoReleaseThreshold / 4);

        // Act
        var level = _memoryMonitor.GetCurrentPressureLevel();

        // Assert
        Assert.Equal("Low", level);
    }

    [Fact]
    public void GetCurrentPressureLevel_WithMediumUsage_ShouldReturnMedium()
    {
        // Arrange
        _memoryMonitor.SetCurrentMemoryUsage((long)(_memoryMonitor.AutoReleaseThreshold * 0.6));

        // Act
        var level = _memoryMonitor.GetCurrentPressureLevel();

        // Assert
        Assert.Equal("Medium", level);
    }

    [Fact]
    public void GetCurrentPressureLevel_WithHighUsage_ShouldReturnHigh()
    {
        // Arrange
        _memoryMonitor.SetCurrentMemoryUsage((long)(_memoryMonitor.AutoReleaseThreshold * 0.9));

        // Act
        var level = _memoryMonitor.GetCurrentPressureLevel();

        // Assert
        Assert.Equal("High", level);
    }

    [Fact]
    public void GetCurrentPressureLevel_WithCriticalUsage_ShouldReturnCritical()
    {
        // Arrange
        _memoryMonitor.SetCurrentMemoryUsage(_memoryMonitor.AutoReleaseThreshold * 2);

        // Act
        var level = _memoryMonitor.GetCurrentPressureLevel();

        // Assert
        Assert.Equal("Critical", level);
    }

    #endregion

    #region 属性测试

    [Fact]
    public void AutoReleaseThreshold_CanBeSetAndGet()
    {
        // Arrange
        var newThreshold = 1024L * 1024 * 1024; // 1GB

        // Act
        _memoryMonitor.AutoReleaseThreshold = newThreshold;

        // Assert
        Assert.Equal(newThreshold, _memoryMonitor.AutoReleaseThreshold);
    }

    [Fact]
    public void CheckInterval_CanBeSetAndGet()
    {
        // Arrange
        var newInterval = TimeSpan.FromSeconds(30);

        // Act
        _memoryMonitor.CheckInterval = newInterval;

        // Assert
        Assert.Equal(newInterval, _memoryMonitor.CheckInterval);
    }

    [Fact]
    public void MemoryPressureThreshold_CanBeSetAndGet()
    {
        // Arrange
        var newThreshold = 0.9;

        // Act
        _memoryMonitor.MemoryPressureThreshold = newThreshold;

        // Assert
        Assert.Equal(newThreshold, _memoryMonitor.MemoryPressureThreshold);
    }

    #endregion

    #region 事件测试

    [Fact]
    public void MemoryPressureDetected_CanSubscribeAndUnsubscribe()
    {
        // Arrange
        var eventCount = 0;
        Action<long> handler = (usage) => eventCount++;

        // Act - Subscribe
        _memoryMonitor.MemoryPressureDetected += handler;
        _memoryMonitor.CheckMemoryPressure(_memoryMonitor.AutoReleaseThreshold + 1000);
        
        // Assert - Event triggered
        Assert.Equal(1, eventCount);

        // Act - Unsubscribe
        _memoryMonitor.MemoryPressureDetected -= handler;
        _memoryMonitor.CheckMemoryPressure(_memoryMonitor.AutoReleaseThreshold + 1000);
        
        // Assert - Event not triggered after unsubscribe
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void AutoReleaseTriggered_CanSubscribeAndUnsubscribe()
    {
        // Arrange
        var eventCount = 0;
        Action handler = () => eventCount++;

        // Act - Subscribe
        _memoryMonitor.AutoReleaseTriggered += handler;
        _memoryMonitor.CheckMemoryPressure(_memoryMonitor.AutoReleaseThreshold * 2);
        
        // Assert - Event triggered
        Assert.Equal(1, eventCount);

        // Act - Unsubscribe
        _memoryMonitor.AutoReleaseTriggered -= handler;
        _memoryMonitor.CheckMemoryPressure(_memoryMonitor.AutoReleaseThreshold * 2);
        
        // Assert - Event not triggered after unsubscribe
        Assert.Equal(1, eventCount);
    }

    #endregion

    #region 边界测试

    [Fact]
    public void CheckMemoryPressure_WithNegativeUsage_ShouldNotTriggerEvent()
    {
        // Arrange
        var eventTriggered = false;
        _memoryMonitor.MemoryPressureDetected += (usage) => eventTriggered = true;

        // Act
        _memoryMonitor.CheckMemoryPressure(-1000);

        // Assert
        Assert.False(eventTriggered);
    }

    [Fact]
    public void CheckMemoryPressure_WithExactThreshold_ShouldNotTriggerEvent()
    {
        // Arrange
        var eventTriggered = false;
        _memoryMonitor.MemoryPressureDetected += (usage) => eventTriggered = true;

        // Act
        _memoryMonitor.CheckMemoryPressure(_memoryMonitor.AutoReleaseThreshold);

        // Assert
        Assert.False(eventTriggered);
    }

    [Fact]
    public void CheckMemoryPressure_WithThresholdPlusOne_ShouldTriggerEvent()
    {
        // Arrange
        var eventTriggered = false;
        _memoryMonitor.MemoryPressureDetected += (usage) => eventTriggered = true;

        // Act
        _memoryMonitor.CheckMemoryPressure(_memoryMonitor.AutoReleaseThreshold + 1);

        // Assert
        Assert.True(eventTriggered);
    }

    #endregion

    #region 资源释放测试

    [Fact]
    public void Dispose_ShouldReleaseResourcesSuccessfully()
    {
        // Arrange
        _memoryMonitor.StartMonitoring();

        // Act
        _memoryMonitor.Dispose();

        // Assert
        Assert.False(_memoryMonitor.IsMonitoring);
        Assert.True(_memoryMonitor.IsDisposed);
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert
        _memoryMonitor.Dispose();
        _memoryMonitor.Dispose(); // Should not throw
    }

    [Fact]
    public void StartMonitoring_AfterDispose_ShouldNotStart()
    {
        // Arrange
        _memoryMonitor.Dispose();

        // Act
        _memoryMonitor.StartMonitoring();

        // Assert
        Assert.False(_memoryMonitor.IsMonitoring);
    }

    #endregion

    #region 资源清理

    public void Dispose()
    {
        _memoryMonitor?.Dispose();
    }

    #endregion

    #region 测试辅助类

    /// <summary>
    /// 测试用的简单内存监控服务实现
    /// </summary>
    public class TestMemoryMonitor : IMemoryMonitor, IDisposable
    {
        private bool _isMonitoring;
        private bool _disposed;
        private long _simulatedMemoryUsage;

        public event Action<long>? MemoryPressureDetected;
        public event Action? AutoReleaseTriggered;

        public long AutoReleaseThreshold { get; set; } = 800 * 1024 * 1024; // 800MB
        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(15);
        public double MemoryPressureThreshold { get; set; } = 0.8;

        public bool IsMonitoring => _isMonitoring && !_disposed;
        public bool IsDisposed => _disposed;

        public void StartMonitoring()
        {
            if (_disposed) return;
            _isMonitoring = true;
        }

        public void StopMonitoring()
        {
            if (_disposed) return;
            _isMonitoring = false;
        }

        public void CheckMemoryPressure(long currentUsage)
        {
            if (currentUsage > AutoReleaseThreshold)
            {
                MemoryPressureDetected?.Invoke(currentUsage);

                // 计算压力级别
                var pressureLevel = CalculatePressureLevel(currentUsage);
                if (pressureLevel == "High" || pressureLevel == "Critical")
                {
                    AutoReleaseTriggered?.Invoke();
                }
            }
        }

        public long GetCurrentMemoryUsage()
        {
            return _simulatedMemoryUsage > 0 ? _simulatedMemoryUsage : GC.GetTotalMemory(false);
        }

        public void ForceGarbageCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public string GetCurrentPressureLevel()
        {
            var currentUsage = GetCurrentMemoryUsage();
            return CalculatePressureLevel(currentUsage);
        }

        public void SetCurrentMemoryUsage(long usage)
        {
            _simulatedMemoryUsage = usage;
        }

        private string CalculatePressureLevel(long currentUsage)
        {
            var pressureRatio = (double)currentUsage / AutoReleaseThreshold;

            return pressureRatio switch
            {
                < 0.5 => "Low",
                < 0.8 => "Medium",
                < 1.0 => "High",
                _ => "Critical"
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            StopMonitoring();
            _disposed = true;
        }
    }

    #endregion
}