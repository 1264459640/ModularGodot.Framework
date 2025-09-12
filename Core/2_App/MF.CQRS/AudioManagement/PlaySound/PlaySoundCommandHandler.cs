using MediatR;
using MF.Services.Abstractions.Extensions.AudioSystem;
using Godot;

namespace MF.CQRS.AudioManagement.PlaySound;

/// <summary>
/// 播放音效指令处理器
/// </summary>
public class PlaySoundCommandHandler : IRequestHandler<PlaySoundCommand>
{
    private readonly IAudioManagerService _audioManagerService;
    
    public PlaySoundCommandHandler(IAudioManagerService audioManagerService)
    {
        _audioManagerService = audioManagerService;
    }
    
    /// <summary>
    /// 处理播放音效指令
    /// </summary>
    public async Task Handle(PlaySoundCommand command, CancellationToken cancellationToken)
    {
        try
        {
            GD.Print($"[SoundHandler] 开始播放音效: {command.AudioPath}");
            
            // 验证指令参数
            if (string.IsNullOrEmpty(command.AudioPath))
            {
                GD.PrintErr($"[SoundHandler] 音频路径不能为空: {command.RequestId}");
                return;
            }
            
            // 调用音效播放服务
            _audioManagerService.PlaySound(command.AudioPath);
            
            GD.Print($"[SoundHandler] 音效播放成功: {command.AudioPath}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SoundHandler] 音效播放异常: {ex.Message}, RequestId: {command.RequestId}");
        }
    }
}