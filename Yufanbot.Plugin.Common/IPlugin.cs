namespace Yufanbot.Plugin.Common;

public interface IPlugin
{
    public const string PluginSuffix = ".yf";
    public void OnInitialize();
}
