namespace Yufanbot.Plugin.Common;

public interface IPlugin
{
    public const string PluginSuffix = ".yf";
    public void OnInitialize();
    public Task OnInitializeAsync()
    {
        return Task.CompletedTask;
    }
}
