using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NapPlana.Core.Bot;
using NapPlana.Core.Data.Event.Message;
using Nexora.Command.Tree;
using Yufanbot.Client.Config;
using Yufanbot.Client.Event;
using Yufanbot.Config;
using Yufanbot.Plugin;
using Yufanbot.Plugin.Common;
using Yufanbot.Plugin.Common.Registration;

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
    private readonly RootNode _commandTreeRoot = new();
    private readonly IBotEventProvider _botEventProvider;

    public Application(IServiceProvider services)
    {
        _logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<Application>();        
        _pluginCompiler = services.GetRequiredService<IPluginCompiler>();
        try
        {
            _botEventProvider = services.GetRequiredService<IBotEventProvider>();
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
        await InitializePluginsAsync();

        RegisterEvents();
        RegisterCommands();
        
        while (true)
        {
            await Task.Delay(200);
        }
    }


    private void RegisterCommands()
    {
        foreach (var plugin in _plugins)
        {
            try
            {
                plugin.Entry.RegisterCommands(_commandTreeRoot);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to register commands for plugin '{pluginName}' (id: {pluginId})",
                    plugin.Meta.Name,
                    plugin.Meta.Id);
            }
        }
    }

    private void RegisterEvents()
    {
        var registerGroups = 
            _plugins.SelectMany(plugin => 
                from m in plugin.Entry.GetType()
                            .GetMethods(
                                BindingFlags.Static | 
                                BindingFlags.Instance | 
                                BindingFlags.Public |
                                BindingFlags.NonPublic)
                let attr = m.GetCustomAttribute<ListenToEventAttribute>()
                where attr is not null
                select (
                    Listener: m, 
                    Attribute: attr,
                    Instance: plugin.Entry
                )
            ).GroupBy(r => r.Attribute.RegisterEventType);
        
        foreach (var group in registerGroups)
        {
            switch (group.Key)
            {
                case EventType.GroupMessage:
                    _botEventProvider.OnGroupMessageReceived += 
                        MessageDispatching.BuildEventDispatcher<GroupMessageEvent>(
                            group,
                            _logger,
                            _coreConfig,
                            _commandTreeRoot);
                    break;
                case EventType.PrivateMessage:
                    _botEventProvider.OnPrivateMessageReceived += 
                        MessageDispatching.BuildEventDispatcher<PrivateMessageEvent>(
                            group,
                            _logger,
                            _coreConfig,
                            _commandTreeRoot);
                    break;
            }
        }
    }

    private async Task InitializePluginsAsync()
    {
        foreach (var plugin in _plugins)
        {
            plugin.Entry.OnInitialize();
        }

        await Task.WhenAll(_plugins.Select(p => p.Entry.OnInitializeAsync()));
    }
}