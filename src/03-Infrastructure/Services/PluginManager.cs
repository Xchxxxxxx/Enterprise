using EfCore.Enterprise.Shared.DependencyInjection;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Services;

public interface IPluginManager
{
    Task<List<Assembly>> LoadPluginsAsync(string pluginDirectory);
    List<IPlugin> GetLoadedPlugins();
    Task<Assembly?> LoadPluginAsync(string pluginPath);
    void UnloadPlugin(string pluginName);
}

public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    Task InitializeAsync();
    Task ShutdownAsync();
}

[Injectable(ServiceLifetime.Singleton)]
public class PluginManager : IPluginManager
{
    private readonly Dictionary<string, PluginLoadContext> _loadedContexts = new();
    private readonly List<IPlugin> _plugins = new();
    private readonly ILogger<PluginManager> _logger;

    public PluginManager(ILogger<PluginManager> logger)
    {
        _logger = logger;
    }

    public async Task<List<Assembly>> LoadPluginsAsync(string pluginDirectory)
    {
        var assemblies = new List<Assembly>();

        if (!Directory.Exists(pluginDirectory))
        {
            _logger.LogWarning("插件目录不存�? {Path}", pluginDirectory);
            return assemblies;
        }

        var dllFiles = Directory.GetFiles(pluginDirectory, "*.dll");
        foreach (var dll in dllFiles)
        {
            var assembly = await LoadPluginAsync(dll);
            if (assembly != null) assemblies.Add(assembly);
        }

        return assemblies;
    }

    public List<IPlugin> GetLoadedPlugins() => _plugins;

    public async Task<Assembly?> LoadPluginAsync(string pluginPath)
    {
        try
        {
            var context = new PluginLoadContext(pluginPath);
            var assembly = context.LoadFromAssemblyPath(pluginPath);

            _loadedContexts[Path.GetFileNameWithoutExtension(pluginPath)] = context;

            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var type in pluginTypes)
            {
                if (Activator.CreateInstance(type) is IPlugin plugin)
                {
                    await plugin.InitializeAsync();
                    _plugins.Add(plugin);
                    _logger.LogInformation("插件已加�? {Name} v{Version}", plugin.Name, plugin.Version);
                }
            }

            return assembly;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "插件加载失败: {Path}", pluginPath);
            return null;
        }
    }

    public void UnloadPlugin(string pluginName)
    {
        if (_loadedContexts.TryGetValue(pluginName, out var context))
        {
            var plugin = _plugins.FirstOrDefault(p => p.Name == pluginName);
            if (plugin != null)
            {
                plugin.ShutdownAsync().GetAwaiter().GetResult();
                _plugins.Remove(plugin);
            }

            context.Unload();
            _loadedContexts.Remove(pluginName);
            _logger.LogInformation("插件已卸�? {Name}", pluginName);
        }
    }
}

public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }
}
