using MediatR;

namespace MF.CQRS.AudioManagement.PlaySound;

/// <summary>
/// 播放音效指令
/// </summary>
public record PlaySoundCommand : IRequest
{
    /// <summary>
    /// 音频文件路径
    /// </summary>
    public string AudioPath { get; init; } = string.Empty;
    
    /// <summary>
    /// 请求ID
    /// </summary>
    public string RequestId { get; init; } = Guid.NewGuid().ToString();
}