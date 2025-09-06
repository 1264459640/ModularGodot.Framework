using Autofac;
using MF.Commons;
using MF.Nodes.Abstractions.Bases;
using MF.Repositories.Bases;
using MF.Services.Bases;

namespace MF.Contexts;

public class Contexts : LazySingleton<Contexts>
{
    private IContainer Container { get; init; }

    private NodeRegister? _nodeRegister;

    public bool RegisterSingleNode<T>(T singleton) where T : INode
    {
        return _nodeRegister != null && _nodeRegister.Register(singleton);
    }
    public Contexts()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<NodeRegister>().SingleInstance();
        builder.RegisterModule<SingleModule>();
        builder.RegisterModule<MediatorModule>();

        // 注册 NodeRegister
        builder.Register(c => new NodeRegister(c)).SingleInstance();

        Container = builder.Build();

        _nodeRegister = Container.Resolve<NodeRegister>();
    }

    public ILifetimeScope RegisterNode<TNode, TRepo, TService>(TNode scene)
        where TNode : class, INode
        where TRepo : NodeRepo<TNode>
        where TService : BaseService
    {
        var scope = Container.BeginLifetimeScope(builder =>
        {
            builder.RegisterInstance(scene).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TRepo>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TService>().AsSelf().AsImplementedInterfaces().SingleInstance();
        });
        scope.Resolve<TRepo>();
        scope.Resolve<TService>();
        return scope;
    }

    public ILifetimeScope RegisterNode<TNode, TService>(TNode scene)
        where TNode : class, INode
        where TService : BaseService
    {
        var scope = Container.BeginLifetimeScope(builder =>
        {
            builder.RegisterInstance(scene).AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<TService>().AsSelf().AsImplementedInterfaces().SingleInstance();
        });
        scope.Resolve<TService>();
        return scope;
    }

    /// <summary>
    /// 从容器中解析服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    public T ResolveService<T>() where T : class
    {
        return Container.Resolve<T>();
    }
    
    /// <summary>
    /// 尝试从容器中解析服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="service">输出的服务实例</param>
    /// <returns>是否解析成功</returns>
    public bool TryResolveService<T>(out T? service) where T : class
    {
        return Container.TryResolve(out service);
    }
    
    /// <summary>
    /// 检查服务是否已注册
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>是否已注册</returns>
    public bool IsServiceRegistered<T>() where T : class
    {
        return Container.IsRegistered<T>();
    }

}
