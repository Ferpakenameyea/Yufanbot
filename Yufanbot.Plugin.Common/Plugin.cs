using System.Runtime.Loader;

namespace Yufanbot.Plugin.Common;

public sealed record YFPlugin(IPlugin Entry, PluginMeta Meta, string FileName)
{
    public required AssemblyLoadContext LoadContext { private get; init; }

    public void Unload()
    {
        LoadContext.Unload();
    }
}