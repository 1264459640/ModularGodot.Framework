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

}
