using MediatR;
using MF.Services.Abstractions.Extensions.AudioSystem;
using Godot;

namespace MF.CQRS.AudioManagement.PlayVoice;

/// <summary>
/// 播放语音指令处理器
/// </summary>
public class PlayVoiceCommandHandler : IRequestHandler<PlayVoiceCommand>
{
    private readonly IAudioManagerService _audioManagerService;
    
    public PlayVoiceCommandHandler(IAudioManagerService audioManagerService)
    {
        _audioManagerService = audioManagerService;
    }
    
    /// <summary>
    /// 处理播放语音指令
    /// </summary>
    public async Task Handle(PlayVoiceCommand command, CancellationToken cancellationToken)
    {
        try
        {
            GD.Print($"[VoiceHandler] 开始播放语音: {command.AudioPath}");
            
            // 验证指令参数
            if (string.IsNullOrEmpty(command.AudioPath))
            {
                GD.PrintErr($"[VoiceHandler] 音频路径不能为空: {command.RequestId}");
                return;
            }
            
            // 调用语音播放服务
            _audioManagerService.PlayVoice(command.AudioPath);
            
            GD.Print($"[VoiceHandler] 语音播放成功: {command.AudioPath}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[VoiceHandler] 语音播放异常: {ex.Message}, RequestId: {command.RequestId}");
        }
    }
}