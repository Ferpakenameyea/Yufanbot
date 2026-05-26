using Microsoft.Extensions.Logging;
using Yufanbot.Config;

namespace Yufanbot.Plugin;

public class PluginCompilerConfig(ILogger<PluginCompilerConfig> logger, IFileReader fileReader, IEnvironmentVariableProvider environmentVariableProvider) : 
    Config<PluginCompilerConfig>(logger, fileReader, environmentVariableProvider)
{
}