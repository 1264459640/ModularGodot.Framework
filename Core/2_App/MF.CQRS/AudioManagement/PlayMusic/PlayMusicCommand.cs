using MediatR;

namespace MF.CQRS.AudioManagement.PlayMusic;

/// <summary>
/// 播放音乐指令
/// </summary>
public record PlayMusicCommand : IRequest
{
    /// <summary>
    /// 音频文件路径
    /// </summary>
    public string AudioPath { get; init; } = string.Empty;
    
    /// <summary>
    /// 淡入持续时间（秒）
    /// </summary>
    public float FadeDuration { get; init; } = 0.0f;
    
    /// <summary>
    /// 是否循环播放
    /// </summary>
    public bool Loop { get; init; } = false;
    
    /// <summary>
    /// 请求ID
    /// </summary>
    public string RequestId { get; init; } = Guid.NewGuid().ToString();
}