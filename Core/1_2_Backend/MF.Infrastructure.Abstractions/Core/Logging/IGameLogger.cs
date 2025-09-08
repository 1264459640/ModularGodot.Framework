namespace MF.Infrastructure.Abstractions.Core.Logging;

/// <summary>
/// 游戏日志接口 - Standard级别
/// </summary>
public interface IGameLogger
{
    /// <summary>
    /// 记录调试信息
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    void LogDebug(string message, params object[] args);
    
    /// <summary>
    /// 记录一般信息
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    void LogInformation(string message, params object[] args);
    
    /// <summary>
    /// 记录警告信息
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    void LogWarning(string message, params object[] args);
    
    /// <summary>
    /// 记录错误信息
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    void LogError(string message, params object[] args);
    
    /// <summary>
    /// 记录错误信息（带异常）
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    void LogError(Exception exception, string message, params object[] args);
    
    /// <summary>
    /// 记录严重错误信息
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    void LogCritical(string message, params object[] args);
    
    /// <summary>
    /// 记录严重错误信息（带异常）
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    void LogCritical(Exception exception, string message, params object[] args);
    
}
