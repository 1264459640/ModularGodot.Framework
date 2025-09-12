using MF.Commons.Extensions.AudioSystem.System;

namespace MF.CQRS.AudioManagement.AudioControl;

/// <summary>
/// 音频控制结果
/// </summary>
public record AudioControlResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// 结果消息
    /// </summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// 命令ID
    /// </summary>
    public string CommandId { get; init; } = string.Empty;
    
    /// <summary>
    /// 执行的操作类型
    /// </summary>
    public AudioControlOperation Operation { get; init; }
    
    /// <summary>
    /// 音频类型
    /// </summary>
    public AudioEnums.AudioType? AudioType { get; init; }
    
    /// <summary>
    /// 操作前的值（如音量、静音状态）
    /// </summary>
    public object? PreviousValue { get; init; }
    
    /// <summary>
    /// 操作后的值
    /// </summary>
    public object? CurrentValue { get; init; }
    
    /// <summary>
    /// 处理时间戳
    /// </summary>
    public DateTime ProcessedAt { get; init; } = DateTime.Now;
    
    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static AudioControlResult Success(
        string message, 
        string commandId, 
        AudioControlOperation operation,
        AudioEnums.AudioType? audioType = null,
        object? previousValue = null,
        object? currentValue = null)
    {
        return new AudioControlResult
        {
            IsSuccess = true,
            Message = message,
            CommandId = commandId,
            Operation = operation,
            AudioType = audioType,
            PreviousValue = previousValue,
            CurrentValue = currentValue,
            ProcessedAt = DateTime.Now
        };
    }
    
    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static AudioControlResult Failure(
        string message, 
        string commandId, 
        AudioControlOperation operation,
        AudioEnums.AudioType? audioType = null)
    {
        return new AudioControlResult
        {
            IsSuccess = false,
            Message = message,
            CommandId = commandId,
            Operation = operation,
            AudioType = audioType,
            ProcessedAt = DateTime.Now
        };
    }
}