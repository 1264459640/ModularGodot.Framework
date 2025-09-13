using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Godot;
using MF.Contexts.Attributes;

namespace MF.Contexts
{
    public class DependencyInjectionModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();

            // Path to the Extensions directory using relative path
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string extensionsPath;
            
            // Try to find the project root by looking for the .sln file
            var currentDir = new DirectoryInfo(baseDirectory);
            while (currentDir != null && !currentDir.GetFiles("*.sln").Any())
            {
                currentDir = currentDir.Parent;
            }
            
            if (currentDir != null)
            {
                // Found project root, navigate to Extensions
                extensionsPath = Path.Combine(currentDir.FullName, "ModularGodot.Framework", "Extensions");
            }
            else
            {
                // Fallback: assume standard Godot structure
                extensionsPath = Path.Combine(baseDirectory, "..", "..", "..", "..", "..", "ModularGodot.Framework", "Extensions");
                extensionsPath = Path.GetFullPath(extensionsPath);
            }

            if (Directory.Exists(extensionsPath))
            {
                var csprojFiles = Directory.GetFiles(extensionsPath, "*.csproj", SearchOption.AllDirectories);
                var assemblyNames = csprojFiles.Select(Path.GetFileNameWithoutExtension).ToList();

                foreach (var assemblyName in assemblyNames)
                {
                    try
                    {
                        if (assemblies.Any(a => a.GetName().Name == assemblyName))
                        {
                            continue;
                        }

                        var assembly = Assembly.Load(assemblyName);
                        assemblies.Add(assembly);
                        GD.Print($"Successfully loaded extension assembly by name: {assemblyName}");
                    }
                    catch (FileNotFoundException)
                    {
                        GD.PrintErr($"Assembly file not found for extension '{assemblyName}'. Make sure the project is built and the DLL is in the application's output directory.");
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"Error loading extension assembly '{assemblyName}': {ex.Message}");
                    }
                }
            }
            else
            {
                GD.Print($"Extensions directory not found at: {extensionsPath}");
            }

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute<InjectableAttribute>() != null);

                foreach (var type in types)
                {
                    builder.RegisterType(type).AsSelf().AsImplementedInterfaces().SingleInstance();
                }
            }
        }
    }
}