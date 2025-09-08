using Godot;
using MF.Infrastructure.Abstractions.Core.Logging;
using MF.Infrastructure.Bases;
using System.Text.RegularExpressions;

namespace MF.Infrastructure.Core.Logging;

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
    
    private Dictionary<string, Color> _logColors = new(DefaultLogColors);
    private readonly object _lock = new();
    
    // 支持 Microsoft.Extensions.Logging 风格的命名占位符：{Name} 或 {Value:F1}
    private static readonly Regex MessageTemplateRegex = new("\\{(?<name>[A-Za-z_][A-Za-z0-9_]*)(:(?<format>[^}]+))?\\}", RegexOptions.Compiled);

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
        var formattedCore = args.Length > 0 ? SafeFormat(message, args) : message;
        var fullMessage = formattedCore + $"\nException: {exception}";
        Log("Error", fullMessage);
    }
    
    public void LogCritical(string message, params object[] args)
    {
        Log("Critical", message, args);
    }
    
    public void LogCritical(Exception exception, string message, params object[] args)
    {
        var formattedCore = args.Length > 0 ? SafeFormat(message, args) : message;
        var fullMessage = formattedCore + $"\nException: {exception}";
        Log("Critical", fullMessage);
    }
    

    
    private void Log(string level, string message, params object[] args)
    {
        if (IsDisposed) return;
        
        try
        {
            var formattedMessage = args.Length > 0 ? SafeFormat(message, args) : message;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] [{level}] {formattedMessage}";
            
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

    // 将命名占位符转换为顺序占位符（{Name} -> {0}，{Percent:F1} -> {0:F1}）以兼容 string.Format
    private static string ConvertMessageTemplate(string template)
    {
        int index = 0;
        return MessageTemplateRegex.Replace(template, m =>
        {
            var fmt = m.Groups["format"].Success ? ":" + m.Groups["format"].Value : string.Empty;
            var replaced = "{" + (index++) + fmt + "}";
            return replaced;
        });
    }

    private static string SafeFormat(string template, object[] args)
    {
        try
        {
            // 先尝试按命名模板转换
            var converted = ConvertMessageTemplate(template);
            return string.Format(converted, args);
        }
        catch
        {
            // 回退：直接返回原模板与参数的拼接，避免抛异常中断业务
            return args.Length > 0 ? ($"{template} | args: " + string.Join(", ", args.Select(a => a?.ToString()))) : template;
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

