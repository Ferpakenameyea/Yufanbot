using NapPlana.Core.Bot;

namespace Yufanbot.Plugin.Common;

public interface IPlugin
{
    public const string PluginSuffix = ".yf";
    public void OnInitialize(NapBot bot);
    public Task OnInitializeAsync(NapBot bot)
    {
        return Task.CompletedTask;
    }
}
