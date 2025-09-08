using System.Collections.Concurrent;
using System.Diagnostics;
using MF.Infrastructure.Abstractions.Core.Logging;
using MF.Infrastructure.Abstractions.Core.Monitoring;
using MF.Infrastructure.Bases;

namespace MF.Infrastructure.Core.Monitoring;

/// <summary>
/// 性能监控实现
/// </summary>
public class PerformanceMonitor : BaseInfrastructure, IPerformanceMonitor
{
    private readonly IGameLogger _logger;
    private readonly ConcurrentDictionary<string, MetricData> _metrics = new();
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, TimerData> _timers = new();
    private readonly ConcurrentDictionary<string, ActiveTimer> _activeTimers = new();
    
    public PerformanceMonitor(IGameLogger logger)
    {
        _logger = logger;
        
        _logger.LogInformation("PerformanceMonitor initialized");
    }
    
    public void RecordMetric(string name, double value, Dictionary<string, string>? tags = null)
    {
        if (_disposed) return;
        
        try
        {
            var key = CreateKey(name, tags);
            _metrics.AddOrUpdate(key, 
                new MetricData { Name = name, Tags = tags ?? new Dictionary<string, string>() },
                (k, existing) => existing);
            
            var metricData = _metrics[key];
            lock (metricData)
            {
                metricData.Values.Add(value);
                metricData.Count++;
                metricData.Sum += value;
                metricData.Min = Math.Min(metricData.Min, value);
                metricData.Max = Math.Max(metricData.Max, value);
                metricData.LastUpdated = DateTime.UtcNow;
                
                // 保持最近1000个值
                if (metricData.Values.Count > 1000)
                {
                    metricData.Values.RemoveAt(0);
                }
            }
            
            _logger.LogDebug("Metric recorded: {Name} = {Value}", name, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording metric: {Name}", name);
        }
    }
    
    public void RecordCounter(string name, long value = 1, Dictionary<string, string>? tags = null)
    {
        if (IsDisposed) return;
        
        try
        {
            var key = CreateKey(name, tags);
            _counters.AddOrUpdate(key, value, (k, existing) => existing + value);
            
            _logger.LogDebug("Counter recorded: {Name} += {Value}", name, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording counter: {Name}", name);
        }
    }
    
    public void RecordTimer(string name, TimeSpan duration, Dictionary<string, string>? tags = null)
    {
        if (IsDisposed) return;
        
        try
        {
            var key = CreateKey(name, tags);
            _timers.AddOrUpdate(key,
                new TimerData { Name = name, Tags = tags ?? new Dictionary<string, string>() },
                (k, existing) => existing);
            
            var timerData = _timers[key];
            lock (timerData)
            {
                timerData.Durations.Add(duration);
                timerData.Count++;
                timerData.TotalTime += duration;
                timerData.MinTime = timerData.MinTime == TimeSpan.Zero ? duration : TimeSpan.FromTicks(Math.Min(timerData.MinTime.Ticks, duration.Ticks));
                timerData.MaxTime = TimeSpan.FromTicks(Math.Max(timerData.MaxTime.Ticks, duration.Ticks));
                timerData.LastUpdated = DateTime.UtcNow;
                
                // 保持最近1000个值
                if (timerData.Durations.Count > 1000)
                {
                    timerData.Durations.RemoveAt(0);
                }
            }
            
            _logger.LogDebug("Timer recorded: {Name} = {Duration}ms", name, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording timer: {Name}", name);
        }
    }
    
    public IDisposable StartTimer(string name, Dictionary<string, string>? tags = null)
    {
        CheckDisposed();
        
        var timer = new ActiveTimer(name, tags, this);
        var key = CreateKey(name, tags);
        _activeTimers.TryAdd(key + "_" + timer.Id, timer);
        
        _logger.LogDebug("Timer started: {Name}", name);
        return timer;
    }
    


    
    private string CreateKey(string name, Dictionary<string, string>? tags)
    {
        if (tags == null || tags.Count == 0)
            return name;
        
        var tagString = string.Join(",", tags.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{name}[{tagString}]";
    }
    

    
    internal void CompleteTimer(ActiveTimer timer)
    {
        try
        {
            var key = CreateKey(timer.Name, timer.Tags) + "_" + timer.Id;
            _activeTimers.TryRemove(key, out _);
            
            RecordTimer(timer.Name, timer.Elapsed, timer.Tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing timer: {TimerName}", timer.Name);
        }
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger.LogInformation("Disposing PerformanceMonitor");
            
            // 完成所有活跃的计时器
            foreach (var timer in _activeTimers.Values)
            {
                timer.Dispose();
            }
            
            _metrics.Clear();
            _counters.Clear();
            _timers.Clear();
            _activeTimers.Clear();
            
            _logger.LogInformation("PerformanceMonitor disposed");
        }
        
        base.Dispose(disposing);
    }
}

/// <summary>
/// 指标数据
/// </summary>
internal class MetricData
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
    public List<double> Values { get; set; } = new();
    public long Count { get; set; }
    public double Sum { get; set; }
    public double Min { get; set; } = double.MaxValue;
    public double Max { get; set; } = double.MinValue;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 计时器数据
/// </summary>
internal class TimerData
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
    public List<TimeSpan> Durations { get; set; } = new();
    public long Count { get; set; }
    public TimeSpan TotalTime { get; set; }
    public TimeSpan MinTime { get; set; }
    public TimeSpan MaxTime { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 活跃计时器
/// </summary>
internal class ActiveTimer : IDisposable
{
    private readonly PerformanceMonitor _monitor;
    private readonly Stopwatch _stopwatch;
    private bool _disposed;
    
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; }
    public Dictionary<string, string>? Tags { get; }
    public TimeSpan Elapsed => _stopwatch.Elapsed;
    
    public ActiveTimer(string name, Dictionary<string, string>? tags, PerformanceMonitor monitor)
    {
        Name = name;
        Tags = tags;
        _monitor = monitor;
        _stopwatch = Stopwatch.StartNew();
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _stopwatch.Stop();
        _monitor.CompleteTimer(this);
        _disposed = true;
    }
}