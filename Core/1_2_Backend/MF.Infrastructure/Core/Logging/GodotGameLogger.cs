using Godot;
using MF.Infrastructure.Abstractions.Core.Logging;
using MF.Infrastructure.Bases;

namespace MF.Infrastructure.Logging;

/// <summary>
/// Godot游戏日志实现
/// </summary>
public class GodotGameLogger : BaseInfrastructure, IGameLogger
{
    private static readonly Dictionary<string, Color> DefaultLogColors = new()
    {
        { "Trace", Colors.Gray },
        { "Debug", Colors.Cyan },
        { "Information", Colors.White },
        { "Warning", Colors.Yellow },
        { "Error", Colors.Red },
        { "Critical", Colors.DarkRed }
    };
    
    private readonly string _categoryName;
    private Dictionary<string, Color> _logColors = new(DefaultLogColors);
    private readonly object _lock = new();
    
    public GodotGameLogger(string categoryName)
    {
        _categoryName = categoryName;
    }
    
    public void LogDebug(string message, params object[] args)
    {
        Log("Debug", message, args);
    }
    
    public void LogInformation(string message, params object[] args)
    {
        Log("Information", message, args);
    }
    
    public void LogWarning(string message, params object[] args)
    {
        Log("Warning", message, args);
    }
    
    public void LogError(string message, params object[] args)
    {
        Log("Error", message, args);
    }
    
    public void LogError(Exception exception, string message, params object[] args)
    {
        var fullMessage = args.Length > 0 ? string.Format(message, args) : message;
        fullMessage += $"\nException: {exception}";
        Log("Error", fullMessage);
    }
    
    public void LogCritical(string message, params object[] args)
    {
        Log("Critical", message, args);
    }
    
    public void LogCritical(Exception exception, string message, params object[] args)
    {
        var fullMessage = args.Length > 0 ? string.Format(message, args) : message;
        fullMessage += $"\nException: {exception}";
        Log("Critical", fullMessage);
    }
    

    
    private void Log(string level, string message, params object[] args)
    {
        if (IsDisposed) return;
        
        try
        {
            var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] [{level}] [{_categoryName}] {formattedMessage}";
            
            // 控制台输出
            if (_logColors.TryGetValue(level, out var color))
            {
                GD.PrintRich($"[color={color.ToHtml()}]{logMessage}[/color]");
            }
            else
            {
                GD.Print(logMessage);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Logging error: {ex.Message}");
        }
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // 没有需要特殊处理的资源
        }
        
        base.Dispose(disposing);
    }
}

/// <summary>
/// 泛型游戏日志实现
/// </summary>
/// <typeparam name="T">日志类别类型</typeparam>
public class GodotGameLogger<T> : GodotGameLogger, IGameLogger<T>
{
    public GodotGameLogger() : base(typeof(T).Name)
    {
    }
}
