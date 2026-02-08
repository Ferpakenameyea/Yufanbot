using Microsoft.Extensions.Logging;
using Yufanbot.Config;

namespace Yufanbot.Client.Config;

public class CoreConfig(ILogger<CoreConfig> logger, IFileReader fileReader, IEnvironmentVariableProvider environmentVariableProvider) : 
    Config<CoreConfig>(logger, fileReader, environmentVariableProvider)
{
    [ConfigEntry("napcat.ip", ConfigEntryGetType.FromConfigFile)]
    public string NapcatIP { get; set; } = string.Empty;
    
    [ConfigEntry("napcat.port", ConfigEntryGetType.FromConfigFile)]
    public int NapcatPort { get; set; }    

    [ConfigEntry("napcat.token", ConfigEntryGetType.FromConfigFile)]
    public string NapcatToken { get; set; } = string.Empty;

    [ConfigEntry("napcat.self_id", ConfigEntryGetType.FromConfigFile)]
    public long SelfId { get; set; } = 0;
}