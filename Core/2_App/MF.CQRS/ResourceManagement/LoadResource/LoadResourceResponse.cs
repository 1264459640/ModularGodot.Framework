namespace MF.CQRS.ResourceManagement.LoadResource;

/// <summary>
/// 资源加载响应
/// </summary>
public record LoadResourceResponse
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
    /// 请求ID
    /// </summary>
    public string RequestId { get; init; } = string.Empty;
    
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
    public static LoadResourceResponse Success(string message, string requestId, string resourcePath, ResourceType resourceType, long loadTimeMs, long resourceSize = 0)
    {
        return new LoadResourceResponse
        {
            IsSuccess = true,
            Message = message,
            RequestId = requestId,
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
    public static LoadResourceResponse Failure(string message, string requestId, string resourcePath, ResourceType resourceType)
    {
        return new LoadResourceResponse
        {
            IsSuccess = false,
            Message = message,
            RequestId = requestId,
            ResourcePath = resourcePath,
            ResourceType = resourceType,
            LoadTimeMs = 0,
            ResourceSize = 0,
            ProcessedAt = DateTime.Now
        };
    }
}