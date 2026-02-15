using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Loader;

internal class PluginLoadContext : AssemblyLoadContext
{
    private readonly ReadOnlyDictionary<string, byte[]> _privateDllBytes;
    private readonly string _pluginRoot;

    private static readonly string[] SharedDlls =
    [
        "Yufanbot.Plugin.Common",
        "Microsoft.Extensions.Logging.Abstractions",
        "Microsoft.Extensions.DependencyInjection.Abstractions"
    ];

    public PluginLoadContext(string pluginRoot, string entryDllName) : base(isCollectible: true)
    {
        _pluginRoot = pluginRoot;
        Dictionary<string, byte[]> privateAssemblies = new();

        var dlls = Directory.GetFiles(_pluginRoot, "*.dll", SearchOption.AllDirectories);

        foreach (var dll in dlls)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(dll);
            if (nameWithoutExt == Path.GetFileNameWithoutExtension(entryDllName))
            {
                continue;
            }
            if (SharedDlls.Contains(nameWithoutExt))
            {
                continue;
            }

            privateAssemblies[nameWithoutExt] = File.ReadAllBytes(dll);
        }

        _privateDllBytes = privateAssemblies.AsReadOnly();
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name != null && SharedDlls.Contains(assemblyName.Name))
        {
            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().FullName == assemblyName.FullName);
            if (loaded != null) 
            {
                return loaded; 
            }
        }

        if (_privateDllBytes.TryGetValue(assemblyName.Name!, out var bytes))
        {
            using var ms = new MemoryStream(bytes);
            return LoadFromStream(ms);
        }

        return null;
    }

    public Assembly LoadEntryAssembly(string entryDllPath)
    {
        var entryBytes = File.ReadAllBytes(entryDllPath);
        using var ms = new MemoryStream(entryBytes);
        return LoadFromStream(ms);
    }
}
