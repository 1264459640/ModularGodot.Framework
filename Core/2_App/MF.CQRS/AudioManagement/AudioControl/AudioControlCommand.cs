using MediatR;
using MF.Commons.Extensions.AudioSystem.System;

namespace MF.CQRS.AudioManagement.AudioControl;

/// <summary>
/// 音频控制操作类型
/// </summary>
public enum AudioControlOperation
{
    /// <summary>
    /// 设置音量
    /// </summary>
    SetVolume,
    
    /// <summary>
    /// 设置静音状态
    /// </summary>
    SetMute,
    
    /// <summary>
    /// 停止所有音频
    /// </summary>
    StopAll,
    
    /// <summary>
    /// 停止音乐
    /// </summary>
    StopMusic,
    
    /// <summary>
    /// 暂停音乐
    /// </summary>
    PauseMusic,
    
    /// <summary>
    /// 恢复音乐
    /// </summary>
    ResumeMusic,
    
    /// <summary>
    /// 淡入音乐
    /// </summary>
    FadeInMusic,
    
    /// <summary>
    /// 淡出音乐
    /// </summary>
    FadeOutMusic
}

/// <summary>
/// 音频控制命令
/// </summary>
public record AudioControlCommand : IRequest<AudioControlResult>
{
    /// <summary>
    /// 控制操作类型
    /// </summary>
    public AudioControlOperation Operation { get; init; }
    
    /// <summary>
    /// 音频类型（用于音量控制）
    /// </summary>
    public AudioEnums.AudioType? AudioType { get; init; }
    
    /// <summary>
    /// 音量值 (0.0 - 1.0)
    /// </summary>
    public float? Volume { get; init; }
    
    /// <summary>
    /// 静音状态
    /// </summary>
    public bool? MuteState { get; init; }
    
    /// <summary>
    /// 淡入/淡出持续时间（秒）
    /// </summary>
    public float? FadeDuration { get; init; }
    
    /// <summary>
    /// 命令ID
    /// </summary>
    public string CommandId { get; init; } = Guid.NewGuid().ToString();
}