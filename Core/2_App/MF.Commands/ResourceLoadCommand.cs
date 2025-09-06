using MediatR;

namespace MF.Commands;

/// <summary>
/// 资源加载命令
/// </summary>
public record ResourceLoadCommand : IRequest<ResourceLoadResult>
{
    /// <summary>
    /// 资源路径
    /// </summary>
    public string ResourcePath { get; init; } = string.Empty;
    
    /// <summary>
    /// 资源类型
    /// </summary>
    public ResourceType ResourceType { get; init; }
    
    /// <summary>
    /// 是否异步加载
    /// </summary>
    public bool IsAsync { get; init; } = true;
    
    /// <summary>
    /// 命令ID
    /// </summary>
    public string CommandId { get; init; } = Guid.NewGuid().ToString();
}

/// <summary>
/// 资源类型枚举
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// 图像资源
    /// </summary>
    Image,
    
    /// <summary>
    /// 音频资源
    /// </summary>
    Audio,
    
    /// <summary>
    /// 场景资源
    /// </summary>
    Scene,
    
    /// <summary>
    /// 材质资源
    /// </summary>
    Material,
    
    /// <summary>
    /// 其他资源
    /// </summary>
    Other
}



/// <summary>
/// 资源加载结果
/// </summary>
public record ResourceLoadResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// 返回消息
    /// </summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// 命令ID
    /// </summary>
    public string CommandId { get; init; } = string.Empty;
    
    /// <summary>
    /// 资源路径
    /// </summary>
    public string ResourcePath { get; init; } = string.Empty;
    
    /// <summary>
    /// 资源类型
    /// </summary>
    public ResourceType ResourceType { get; init; }
    
    /// <summary>
    /// 加载耗时（毫秒）
    /// </summary>
    public long LoadTimeMs { get; init; }
    
    /// <summary>
    /// 资源大小（字节）
    /// </summary>
    public long ResourceSize { get; init; }
    
    /// <summary>
    /// 处理时间戳
    /// </summary>
    public DateTime ProcessedAt { get; init; } = DateTime.Now;
    
    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static ResourceLoadResult Success(string message, string commandId, string resourcePath, ResourceType resourceType, long loadTimeMs, long resourceSize = 0)
    {
        return new ResourceLoadResult
        {
            IsSuccess = true,
            Message = message,
            CommandId = commandId,
            ResourcePath = resourcePath,
            ResourceType = resourceType,
            LoadTimeMs = loadTimeMs,
            ResourceSize = resourceSize,
            ProcessedAt = DateTime.Now
        };
    }
    
    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static ResourceLoadResult Failure(string message, string commandId, string resourcePath, ResourceType resourceType)
    {
        return new ResourceLoadResult
        {
            IsSuccess = false,
            Message = message,
            CommandId = commandId,
            ResourcePath = resourcePath,
            ResourceType = resourceType,
            LoadTimeMs = 0,
            ResourceSize = 0,
            ProcessedAt = DateTime.Now
        };
    }
}