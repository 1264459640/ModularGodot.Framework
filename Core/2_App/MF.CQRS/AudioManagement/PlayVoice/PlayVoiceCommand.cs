using MediatR;

namespace MF.CQRS.AudioManagement.PlayVoice;

/// <summary>
/// 播放语音指令
/// </summary>
public record PlayVoiceCommand : IRequest
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