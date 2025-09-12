using MediatR;
using MF.Services.Abstractions.Extensions.AudioSystem;
using Godot;

namespace MF.CQRS.AudioManagement.PlayMusic;

/// <summary>
/// 播放音乐指令处理器
/// </summary>
public class PlayMusicCommandHandler : IRequestHandler<PlayMusicCommand>
{
    private readonly IAudioManagerService _audioManagerService;
    
    public PlayMusicCommandHandler(IAudioManagerService audioManagerService)
    {
        _audioManagerService = audioManagerService;
    }
    
    /// <summary>
    /// 处理播放音乐指令
    /// </summary>
    public async Task Handle(PlayMusicCommand command, CancellationToken cancellationToken)
    {
        try
        {
            GD.Print($"[MusicHandler] 开始播放音乐: {command.AudioPath}");
            
            // 验证指令参数
            if (string.IsNullOrEmpty(command.AudioPath))
            {
                GD.PrintErr($"[MusicHandler] 音频路径不能为空: {command.RequestId}");
                return;
            }
            
            // 调用音乐播放服务
            _audioManagerService.PlayMusic(
                command.AudioPath, 
                command.FadeDuration, 
                command.Loop
            );
            
            GD.Print($"[MusicHandler] 音乐播放成功: {command.AudioPath}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MusicHandler] 音乐播放异常: {ex.Message}, RequestId: {command.RequestId}");
        }
    }
}