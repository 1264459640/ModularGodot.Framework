using MediatR;
using MF.Services.Abstractions.Extensions.AudioSystem;
using MF.Commons.Extensions.AudioSystem.System;
using Godot;

namespace MF.CQRS.AudioManagement.AudioControl;

/// <summary>
/// 音频控制命令处理器
/// </summary>
public class AudioControlCommandHandler : IRequestHandler<AudioControlCommand, AudioControlResult>
{
    private readonly IAudioManagerService _audioManagerService;
    
    public AudioControlCommandHandler(IAudioManagerService audioManagerService)
    {
        _audioManagerService = audioManagerService;
    }
    
    /// <summary>
    /// 处理音频控制命令
    /// </summary>
    public async Task<AudioControlResult> Handle(AudioControlCommand request, CancellationToken cancellationToken)
    {
        try
        {
            GD.Print($"[AudioControlHandler] 执行音频控制操作: {request.Operation}");
            
            return request.Operation switch
            {
                AudioControlOperation.SetVolume => await HandleSetVolume(request),
                AudioControlOperation.SetMute => await HandleSetMute(request),
                AudioControlOperation.StopAll => await HandleStopAll(request),
                AudioControlOperation.StopMusic => await HandleStopMusic(request),
                AudioControlOperation.PauseMusic => await HandlePauseMusic(request),
                AudioControlOperation.ResumeMusic => await HandleResumeMusic(request),
                AudioControlOperation.FadeInMusic => await HandleFadeInMusic(request),
                AudioControlOperation.FadeOutMusic => await HandleFadeOutMusic(request),
                _ => AudioControlResult.Failure(
                    $"不支持的操作类型: {request.Operation}",
                    request.CommandId,
                    request.Operation
                )
            };
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[AudioControlHandler] 音频控制异常: {ex.Message}");
            
            return AudioControlResult.Failure(
                $"控制异常: {ex.Message}",
                request.CommandId,
                request.Operation
            );
        }
    }
    
    private async Task<AudioControlResult> HandleSetVolume(AudioControlCommand request)
    {
        if (!request.AudioType.HasValue || !request.Volume.HasValue)
        {
            return AudioControlResult.Failure(
                "设置音量需要指定音频类型和音量值",
                request.CommandId,
                request.Operation
            );
        }
        
        if (request.Volume < 0.0f || request.Volume > 1.0f)
        {
            return AudioControlResult.Failure(
                "音量值必须在0.0到1.0之间",
                request.CommandId,
                request.Operation,
                request.AudioType
            );
        }
        
        var previousVolume = _audioManagerService.GetVolume(request.AudioType.Value);
        _audioManagerService.SetVolume(request.AudioType.Value, request.Volume.Value);
        
        return AudioControlResult.Success(
            $"音量设置成功: {request.AudioType} = {request.Volume:F2}",
            request.CommandId,
            request.Operation,
            request.AudioType,
            previousVolume,
            request.Volume.Value
        );
    }
    
    private async Task<AudioControlResult> HandleSetMute(AudioControlCommand request)
    {
        if (!request.MuteState.HasValue)
        {
            return AudioControlResult.Failure(
                "设置静音需要指定静音状态",
                request.CommandId,
                request.Operation
            );
        }
        
        var previousMute = _audioManagerService.GetMuteState();
        _audioManagerService.SetMuteState(request.MuteState.Value);
        
        return AudioControlResult.Success(
            $"静音状态设置成功: {(request.MuteState.Value ? "静音" : "取消静音")}",
            request.CommandId,
            request.Operation,
            null,
            previousMute,
            request.MuteState.Value
        );
    }
    
    private async Task<AudioControlResult> HandleStopAll(AudioControlCommand request)
    {
        _audioManagerService.StopAll();
        
        return AudioControlResult.Success(
            "所有音频已停止",
            request.CommandId,
            request.Operation
        );
    }
    
    private async Task<AudioControlResult> HandleStopMusic(AudioControlCommand request)
    {
        _audioManagerService.StopMusic();
        
        return AudioControlResult.Success(
            "背景音乐已停止",
            request.CommandId,
            request.Operation,
            AudioEnums.AudioType.Music
        );
    }
    
    private async Task<AudioControlResult> HandlePauseMusic(AudioControlCommand request)
    {
        _audioManagerService.PauseMusic();
        
        return AudioControlResult.Success(
            "背景音乐已暂停",
            request.CommandId,
            request.Operation,
            AudioEnums.AudioType.Music
        );
    }
    
    private async Task<AudioControlResult> HandleResumeMusic(AudioControlCommand request)
    {
        _audioManagerService.ResumeMusic();
        
        return AudioControlResult.Success(
            "背景音乐已恢复",
            request.CommandId,
            request.Operation,
            AudioEnums.AudioType.Music
        );
    }
    
    private async Task<AudioControlResult> HandleFadeInMusic(AudioControlCommand request)
    {
        var duration = request.FadeDuration ?? 1.0f;
        _audioManagerService.FadeInMusic(duration);
        
        return AudioControlResult.Success(
            $"背景音乐淡入开始，持续时间: {duration}秒",
            request.CommandId,
            request.Operation,
            AudioEnums.AudioType.Music,
            null,
            duration
        );
    }
    
    private async Task<AudioControlResult> HandleFadeOutMusic(AudioControlCommand request)
    {
        var duration = request.FadeDuration ?? 1.0f;
        _audioManagerService.FadeOutMusic(duration);
        
        return AudioControlResult.Success(
            $"背景音乐淡出开始，持续时间: {duration}秒",
            request.CommandId,
            request.Operation,
            AudioEnums.AudioType.Music,
            null,
            duration
        );
    }
}