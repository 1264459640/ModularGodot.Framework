using System.Reflection;
using Autofac;
using MF.Contexts.Attributes;
using Module = Autofac.Module;
using MF.Data.Transient.Infrastructure.Monitoring; // ResourceSystemConfig
using Microsoft.Extensions.Caching.Memory; // IMemoryCache
using MF.Infrastructure.Abstractions.Core.Logging; // IGameLogger

namespace MF.Contexts;

public class SingleModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        try
        {
            // 显式注册资源系统配置（供 ResourceManager 等使用）
            builder.RegisterType<ResourceSystemConfig>()
                .AsSelf()
                .SingleInstance();

            // 显式注册 IMemoryCache（MemoryCacheService 依赖）
            builder.RegisterInstance(new MemoryCache(new MemoryCacheOptions()))
                .As<IMemoryCache>()
                .SingleInstance();

            // 只加载实现程序集
            var assemblyNames = new[]
            {
                "MF.Services",
                "MF.Repositories",
                "MF.Infrastructure"
            };
            
            foreach (var assemblyName in assemblyNames)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    RegisterAssemblyTypes(assembly, builder);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load assembly {assemblyName}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Assembly loading failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 注册程序集中的所有类型
    /// </summary>
    private void RegisterAssemblyTypes(Assembly assembly, ContainerBuilder builder)
    {
        try
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => !t.IsDefined(typeof(SkipRegistrationAttribute), false))
                // 过滤开放泛型/仍包含未闭合类型参数的类型
                .Where(t => !t.IsGenericTypeDefinition && !t.ContainsGenericParameters)
                // 过滤编译器生成的类型（闭包类、状态机、匿名类型等）
                .Where(t =>
                {
                    var isCompilerGenerated = t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false);
                    var name = t.Name;
                    var looksCompilerGenerated = name.StartsWith("<") || name.Contains("DisplayClass") || name.Contains("d__") || name.Contains("AnonymousType");
                    return !isCompilerGenerated && !looksCompilerGenerated;
                });
            
            foreach (var type in types)
            {
                try
                {
                    builder.RegisterType(type)
                        .AsImplementedInterfaces()
                        .AsSelf()
                        .SingleInstance();
                    
                    System.Diagnostics.Debug.WriteLine($"Registered: {type.Name} with all implemented interfaces");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to register {type.Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to process assembly {assembly.FullName}: {ex.Message}");
        }
    }
}