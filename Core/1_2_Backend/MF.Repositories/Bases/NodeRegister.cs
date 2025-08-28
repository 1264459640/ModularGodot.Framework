using Autofac;
using MF.Nodes.Abstractions.Bases;
using MF.Repositories.Abstractions.Bases;

namespace MF.Repositories.Bases;

public class NodeRegister(IComponentContext componentContext)
{
    // 定义接口到注册方法的映射
    private readonly Dictionary<Type, Func<object, bool>> _registrations = new();
    
    // 缓存已解析的仓储类型，避免重复反射
    private readonly Dictionary<Type, Type> _repoTypeCache = new();

    public bool Register<T>(T node) where T : INode
    {
        var nodeType = node.GetType();
        var interfaces = nodeType.GetInterfaces()
            .Where(i => i != typeof(INode) && typeof(INode).IsAssignableFrom(i))
            .ToArray();

        // 查找并调用相应的注册方法
        foreach (var interfaceType in interfaces)
        {
            if (_registrations.TryGetValue(interfaceType, out var registerMethod))
            {
                return registerMethod(node);
            }
            else
            {
                // 尝试通过反射动态解析并调用Register方法
                try
                {
                    var repoType = GetOrCacheRepoType(interfaceType);
                    if (componentContext.IsRegistered(repoType))
                    {
                        var repo = componentContext.Resolve(repoType);
                        var registerMethodInfo = repoType.GetMethod("Register");
                        if (registerMethodInfo != null)
                        {
                            return (bool)registerMethodInfo.Invoke(repo, new object[] { node });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 记录异常但继续尝试其他接口
                    Console.WriteLine($"注册节点 {nodeType.Name} 到接口 {interfaceType.Name} 时发生异常: {ex.Message}");
                }
            }
        }

        throw new ArgumentException($"暂不支持的单例节点：{nodeType.Name}，未找到匹配的仓储接口");
    }
    
    private Type GetOrCacheRepoType(Type interfaceType)
    {
        if (!_repoTypeCache.TryGetValue(interfaceType, out var repoType))
        {
            repoType = typeof(ISingletonNodeRepo<>).MakeGenericType(interfaceType);
            _repoTypeCache[interfaceType] = repoType;
        }
        return repoType;
    }

    // 初始化预定义的注册方法映射（如果需要特殊处理的接口）
    public void InitializeRegistrations()
    {
        // 这里可以添加特殊的接口注册方法映射
        // 例如：_registrations[typeof(ISpecialNode)] = (node) => SpecialRegisterMethod(node);
    }
}