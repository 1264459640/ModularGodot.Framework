using MediatR;

namespace MF.CQRS.ResourceManagement.LoadResource;

/// <summary>
/// 资源加载请求
/// </summary>
public record LoadResourceRequest : IRequest<LoadResourceResponse>
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
    /// 请求ID
    /// </summary>
    public string RequestId { get; init; } = Guid.NewGuid().ToString();
}