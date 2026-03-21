using Nexora.Command.Tree;

namespace Yufanbot.Plugin.Common;

public interface IPlugin
{
    public const string PluginSuffix = ".yf";
    public void OnInitialize() {}
    public Task OnInitializeAsync() => Task.CompletedTask;
    public void RegisterCommands(RootNode root) {}
}
