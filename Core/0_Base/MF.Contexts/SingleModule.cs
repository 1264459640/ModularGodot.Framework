using System.Reflection;
using Autofac;
using MF.Contexts.Attributes;
using Module = Autofac.Module;

namespace MF.Contexts;

public class SingleModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        try
        {
            // 注册 IMemoryCache
            builder.RegisterType<Microsoft.Extensions.Caching.Memory.MemoryCache>()
                .As<Microsoft.Extensions.Caching.Memory.IMemoryCache>()
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
                .Where(t => !t.IsDefined(typeof(SkipRegistrationAttribute), false));
            
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