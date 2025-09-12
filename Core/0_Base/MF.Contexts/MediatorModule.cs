using System.Reflection;
using Autofac;
using MediatR.Extensions.Autofac.DependencyInjection;
using MediatR.Extensions.Autofac.DependencyInjection.Builder;

namespace MF.Contexts;

public class MediatorModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // 注册所有命令处理程序和通知处理程序
        var servicesAssembly = Assembly.Load("MF.Services");
        var cqrsAssembly = Assembly.Load("MF.CQRS");
        var configuration = MediatRConfigurationBuilder.Create(servicesAssembly, cqrsAssembly)
            .WithAllOpenGenericHandlerTypesRegistered()
            .Build();
        builder.RegisterMediatR(configuration);
    }
}
