using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NapPlana.Core.Bot;
using Newtonsoft.Json;
using Yufanbot.Client.Config;
using Yufanbot.Config;
using Yufanbot.Plugin;
using Yufanbot.Plugin.Common;

namespace Yufanbot.Client;

public sealed class Application
{
    private readonly IConfigProvider _configProvider;
    private readonly CoreConfig _coreConfig;
    private readonly NapBot _bot;
    private readonly ILogger<Application> _logger;
    private readonly List<YFPlugin> _plugins = [];
    private readonly IPluginCompiler _pluginCompiler; 
    private readonly Lock _pluginCollectionLock = new();

    public Application(ServiceProvider services)
    {
        _logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<Application>();        
        _pluginCompiler = services.GetRequiredService<IPluginCompiler>();
        try
        {
            _configProvider = services.GetRequiredService<IConfigProvider>();
            _coreConfig = _configProvider.Resolve<CoreConfig>();
            _bot = PlanaBotFactory.Create()
                .SetSelfId(_coreConfig.SelfId)
                .SetConnectionType(NapPlana.Core.Data.BotConnectionType.WebSocketClient)
                .SetIp(_coreConfig.NapcatIP)
                .SetPort(_coreConfig.NapcatPort)
                .SetToken(_coreConfig.NapcatToken)
                .Build();
        } 
        catch (Exception e)
        {
            _logger.LogCritical(e, "Critical error when initializing application.");    
            throw;
        }
    }

    private async Task LoadPluginsAsync()
    {
        DirectoryInfo directoryInfo = new(_coreConfig.PluginDirectory);
        if (!directoryInfo.Exists)
        {
            directoryInfo.Create();
            return;
        }
        var extensionFiles = directoryInfo.GetFiles().Where(file => file.Suffix() == ".yf");

        await Parallel.ForEachAsync(extensionFiles, async (file, _) =>
        {
            var plugin = await _pluginCompiler.CompilePluginAsync(file.FullName);
            if (plugin != null)
            {
                using var scope = _pluginCollectionLock.EnterScope();
                _plugins.Add(plugin);
            } 
        });
        return;
    }

    public async Task RunAsync()
    {
        await LoadPluginsAsync();
        _logger.LogInformation("{count} plugins were loaded.", _plugins.Count);
        if (_plugins.Count == 0)
        {
            _logger.LogWarning("You're running a yufan bot without any plugin, no actions will be done.");
        }
        else
        {
            foreach (var plugin in _plugins)
            {
                _logger.LogInformation("- {pluginname} {pluginversion}", 
                    plugin.Meta.Name, 
                    plugin.Meta.Version);
            }
        }
        await _bot.StartAsync();
        foreach (var plugin in _plugins)
        {
            plugin.Entry.OnInitialize(_bot);
            await plugin.Entry.OnInitializeAsync(_bot);
        }
        
        while (true)
        {
            await Task.Delay(200);
        }
    }
}