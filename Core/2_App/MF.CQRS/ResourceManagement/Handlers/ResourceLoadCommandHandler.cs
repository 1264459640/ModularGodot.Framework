using MediatR;
using MF.Commands;
using Godot;
using System.Diagnostics;

namespace MF.Commands;

/// <summary>
/// 资源加载命令处理器
/// </summary>
public class ResourceLoadCommandHandler : IRequestHandler<ResourceLoadCommand, ResourceLoadResult>
{
    /// <summary>
    /// 处理资源加载命令
    /// </summary>
    /// <param name="request">资源加载命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源加载结果</returns>
    public async Task<ResourceLoadResult> Handle(ResourceLoadCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            GD.Print($"[CommandHandler] 开始加载资源: {request.ResourcePath} (类型: {request.ResourceType})");
            
            // 根据资源类型进行不同的加载处理
            var loadResult = await LoadResourceByType(request, cancellationToken);
            
            stopwatch.Stop();
            
            if (loadResult.success)
            {
                GD.Print($"[CommandHandler] 资源加载成功: {request.ResourcePath}, 耗时: {stopwatch.ElapsedMilliseconds}ms, 大小: {loadResult.size} bytes");
                return ResourceLoadResult.Success(
                    $"资源加载成功: {System.IO.Path.GetFileName(request.ResourcePath)}",
                    request.CommandId,
                    request.ResourcePath,
                    request.ResourceType,
                    stopwatch.ElapsedMilliseconds,
                    loadResult.size
                );
            }
            else
            {
                GD.PrintErr($"[CommandHandler] 资源加载失败: {request.ResourcePath}, 原因: {loadResult.error}");
                return ResourceLoadResult.Failure(
                    $"资源加载失败: {loadResult.error}",
                    request.CommandId,
                    request.ResourcePath,
                    request.ResourceType
                );
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            GD.PrintErr($"[CommandHandler] 资源加载异常: {ex.Message}");
            return ResourceLoadResult.Failure(
                $"加载异常: {ex.Message}",
                request.CommandId,
                request.ResourcePath,
                request.ResourceType
            );
        }
    }
    
    /// <summary>
    /// 根据资源类型加载资源
    /// </summary>
    private async Task<(bool success, long size, string error)> LoadResourceByType(ResourceLoadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            Resource? resource = null;
            
            if (request.IsAsync)
            {
                // 异步加载
                await Task.Run(() => {
                    resource = GD.Load(request.ResourcePath);
                }, cancellationToken);
            }
            else
            {
                // 同步加载
                resource = GD.Load(request.ResourcePath);
            }
            
            if (resource == null)
            {
                return (false, 0, "资源不存在或加载失败");
            }
            
            // 获取资源大小（估算）
            long resourceSize = EstimateResourceSize(resource, request.ResourceType);
            
            // 根据资源类型进行特定验证
            var validationResult = ValidateResourceByType(resource, request.ResourceType);
            if (!validationResult.isValid)
            {
                return (false, 0, validationResult.error);
            }
            
            return (true, resourceSize, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, 0, ex.Message);
        }
    }
    
    /// <summary>
    /// 估算资源大小
    /// </summary>
    private long EstimateResourceSize(Resource resource, ResourceType resourceType)
    {
        try
        {
            return resourceType switch
            {
                ResourceType.Image when resource is Texture2D texture => texture.GetWidth() * texture.GetHeight() * 4, // RGBA
                ResourceType.Audio when resource is AudioStream audio => 1024 * 1024, // 估算1MB
                ResourceType.Scene => 512 * 1024, // 估算512KB
                ResourceType.Material => 64 * 1024, // 估算64KB
                _ => 1024 // 默认1KB
            };
        }
        catch
        {
            return 1024; // 默认1KB
        }
    }
    
    /// <summary>
    /// 根据资源类型验证资源
    /// </summary>
    private (bool isValid, string error) ValidateResourceByType(Resource resource, ResourceType resourceType)
    {
        try
        {
            return resourceType switch
            {
                ResourceType.Image => resource is Texture2D ? (true, string.Empty) : (false, "不是有效的图像资源"),
                ResourceType.Audio => resource is AudioStream ? (true, string.Empty) : (false, "不是有效的音频资源"),
                ResourceType.Scene => resource is PackedScene ? (true, string.Empty) : (false, "不是有效的场景资源"),
                ResourceType.Material => resource is Material ? (true, string.Empty) : (false, "不是有效的材质资源"),
                _ => (true, string.Empty) // 其他类型默认通过
            };
        }
        catch (Exception ex)
        {
            return (false, $"验证异常: {ex.Message}");
        }
    }
}