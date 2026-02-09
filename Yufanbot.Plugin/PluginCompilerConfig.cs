using Microsoft.Extensions.Logging;
using Yufanbot.Config;

namespace Yufanbot.Plugin;

public class PluginCompilerConfig(ILogger<PluginCompilerConfig> logger, IFileReader fileReader, IEnvironmentVariableProvider environmentVariableProvider) : 
    Config<PluginCompilerConfig>(logger, fileReader, environmentVariableProvider)
{
    [ConfigEntry("plugin.compiler.sources", ConfigEntryGetType.FromConfigFile)]
    public List<string> NugetSources { get; set; } = [
        "https://api.nuget.org/v3/index.json"
    ];
}