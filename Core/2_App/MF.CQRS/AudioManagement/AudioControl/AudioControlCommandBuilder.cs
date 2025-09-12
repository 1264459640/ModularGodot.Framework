using MF.Commons.Extensions.AudioSystem.System;

namespace MF.CQRS.AudioManagement.AudioControl;

/// <summary>
/// 音频控制命令构建器
/// </summary>
public class AudioControlCommandBuilder
{
    private AudioControlOperation _operation;
    private AudioEnums.AudioType? _audioType;
    private float? _volume;
    private bool? _muteState;
    private float? _fadeDuration;
    private string? _commandId;

    private AudioControlCommandBuilder(AudioControlOperation operation)
    {
        _operation = operation;
    }

    /// <summary>
    /// 设置音频类型
    /// </summary>
    public AudioControlCommandBuilder WithAudioType(AudioEnums.AudioType audioType)
    {
        _audioType = audioType;
        return this;
    }

    /// <summary>
    /// 设置音量
    /// </summary>
    public AudioControlCommandBuilder WithVolume(float volume)
    {
        _volume = Math.Clamp(volume, 0.0f, 1.0f);
        return this;
    }

    /// <summary>
    /// 设置静音状态
    /// </summary>
    public AudioControlCommandBuilder WithMuteState(bool mute)
    {
        _muteState = mute;
        return this;
    }

    /// <summary>
    /// 设置淡入/淡出持续时间
    /// </summary>
    public AudioControlCommandBuilder WithFadeDuration(float duration)
    {
        _fadeDuration = Math.Max(0.0f, duration);
        return this;
    }

    /// <summary>
    /// 设置命令ID
    /// </summary>
    public AudioControlCommandBuilder WithCommandId(string commandId)
    {
        _commandId = commandId;
        return this;
    }

    /// <summary>
    /// 构建命令
    /// </summary>
    public AudioControlCommand Build()
    {
        return new AudioControlCommand
        {
            Operation = _operation,
            AudioType = _audioType,
            Volume = _volume,
            MuteState = _muteState,
            FadeDuration = _fadeDuration,
            CommandId = _commandId ?? Guid.NewGuid().ToString()
        };
    }

    /// <summary>
    /// 创建设置音量的命令构建器
    /// </summary>
    public static AudioControlCommandBuilder SetVolume(AudioEnums.AudioType audioType, float volume)
    {
        return new AudioControlCommandBuilder(AudioControlOperation.SetVolume)
            .WithAudioType(audioType)
            .WithVolume(volume);
    }

    /// <summary>
    /// 创建设置静音的命令构建器
    /// </summary>
    public static AudioControlCommandBuilder SetMute(bool mute)
    {
        return new AudioControlCommandBuilder(AudioControlOperation.SetMute)
            .WithMuteState(mute);
    }

    /// <summary>
    /// 创建停止所有音频的命令构建器
    /// </summary>
    public static AudioControlCommandBuilder StopAll()
    {
        return new AudioControlCommandBuilder(AudioControlOperation.StopAll);
    }

    /// <summary>
    /// 创建停止音乐的命令构建器
    /// </summary>
    public static AudioControlCommandBuilder StopMusic()
    {
        return new AudioControlCommandBuilder(AudioControlOperation.StopMusic);
    }

    /// <summary>
    /// 创建暂停音乐的命令构建器
    /// </summary>
    public static AudioControlCommandBuilder PauseMusic()
    {
        return new AudioControlCommandBuilder(AudioControlOperation.PauseMusic);
    }

    /// <summary>
    /// 创建恢复音乐的命令构建器
    /// </summary>
    public static AudioControlCommandBuilder ResumeMusic()
    {
        return new AudioControlCommandBuilder(AudioControlOperation.ResumeMusic);
    }

    /// <summary>
    /// 创建淡入音乐的命令构建器
    /// </summary>
    public static AudioControlCommandBuilder FadeInMusic(float duration = 1.0f)
    {
        return new AudioControlCommandBuilder(AudioControlOperation.FadeInMusic)
            .WithFadeDuration(duration);
    }

    /// <summary>
    /// 创建淡出音乐的命令构建器
    /// </summary>
    public static AudioControlCommandBuilder FadeOutMusic(float duration = 1.0f)
    {
        return new AudioControlCommandBuilder(AudioControlOperation.FadeOutMusic)
            .WithFadeDuration(duration);
    }
}