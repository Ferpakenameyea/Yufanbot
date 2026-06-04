using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NapPlana.Core.Bot;
using NapPlana.Core.Data.API;
using NapPlana.Core.Data.Event.Message;
using NapPlana.Core.Data.Message;
using Nexora.Command;
using Nexora.Command.Executor;
using Nexora.Command.Tree;
using Yufanbot.Client.BotEngine;
using Yufanbot.Client.Config;
using Yufanbot.Client.Event;
using Yufanbot.Config;
using Yufanbot.Plugin;
using Yufanbot.Plugin.Common;
using Yufanbot.Plugin.Common.Registration;

using static Nexora.Command.Tree.CommandTreeNode;
using static Yufanbot.Plugin.Common.IPlugin;

namespace Yufanbot.Client;

public sealed class Application
{
    private readonly IConfigProvider _configProvider;
    private readonly CoreConfig _coreConfig;
    private readonly IBotEngine _bot;
    private readonly ILogger<Application> _logger;
    private readonly List<YFPlugin> _plugins = [];
    private readonly IPluginCompiler _pluginCompiler; 
    private readonly Lock _pluginCollectionLock = new();
    private RootNode _commandTreeRoot = new();
    private CommandLocator _commandLocator = null!;
    private readonly IBotEventProvider _botEventProvider;
    public readonly Version BotVersion = new(major: 1, minor: 1, build: 0);

    private event Action<GroupMessageEvent>? OnNonCommandGroupMessageReceived;
    private event Action<PrivateMessageEvent>? OnNonCommandPrivateMessageReceived;

    public Application(IServiceProvider services)
    {
        _logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<Application>();        
        _pluginCompiler = services.GetRequiredService<IPluginCompiler>();
        try
        {
            _botEventProvider = services.GetRequiredService<IBotEventProvider>();
            _configProvider = services.GetRequiredService<IConfigProvider>();
            _coreConfig = _configProvider.Resolve<CoreConfig>();
            _bot = services.GetRequiredService<IBotEngine>();
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

        _botEventProvider.OnGroupMessageReceived += OnRawGroupMessageReceived;
        _botEventProvider.OnPrivateMessageReceived += OnRawPrivateMessageReceived;
        
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

        RegisterSystemCommands();

        _commandLocator = new(_commandTreeRoot.Freeze());
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
                    OnNonCommandGroupMessageReceived += 
                        MessageDispatching.BuildEventDispatcher<GroupMessageEvent>(
                            group,
                            _logger,
                            _coreConfig,
                            _commandTreeRoot);
                    break;
                case EventType.PrivateMessage:
                    OnNonCommandPrivateMessageReceived += 
                        MessageDispatching.BuildEventDispatcher<PrivateMessageEvent>(
                            group,
                            _logger,
                            _coreConfig,
                            _commandTreeRoot);
                    break;
            }
        }
    }
    private void RegisterSystemCommands()
    {
        // TODO: register system commands
    }

    private bool IsCommand(MessageEventBase @event, [NotNullWhen(true)] out string? command)
    {
        if (@event.Messages[0]?.MessageData is TextMessageData textData &&
            textData.Text.StartsWith(_coreConfig.CommandPrefix))
        {
            var commandText = textData.Text[_coreConfig.CommandPrefix.Length..];
            if (!string.IsNullOrWhiteSpace(commandText))
            {
                command = commandText;
                return true;
            }
        }

        command = null;
        return false;
    }

    private bool TryExecuteCommand(MessageEventBase @event, string senderId, string type, string? groupId = null)
    {
        if (!IsCommand(@event, out var command))
        {
            return false;
        }

        try
        {
            var lexer = new CommandLexer(command);
            var callback = _commandLocator.Locate(lexer);
            if (callback is null)
            {
                _logger.LogError("Command: {raw} not found!",
                    command);
                return true;
            }

            callback.Value.args[MessageTypeEventArg] = type;
            callback.Value.args[SenderIdEventArg] = senderId;

            if (groupId is not null)
            {
                callback.Value.args[GroupIdEventArg] = groupId;
            }

            var result = callback.Value.Invoke();
            if (!result.IsSuccess)
            {
                _logger.LogError(result.Error, "Failed in command {raw} execution", command);
            }
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected exception in command {raw} execution", command);
            return false;
        }
    }

    private void OnRawGroupMessageReceived(GroupMessageEvent @event)
    {
        Task.Run(() =>
        {
            if (TryExecuteCommand(@event,
                    senderId: @event.Sender.UserId.ToString(),
                    type: GroupMessageTypeValue,
                    groupId: @event.GroupId.ToString()))
            {
                return;
            }

            try
            {
                OnNonCommandGroupMessageReceived?.Invoke(@event);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception in handling message");
            }
        });
    }

    private void OnRawPrivateMessageReceived(PrivateMessageEvent @event)
    {
        Task.Run(() =>
        {
            if (TryExecuteCommand(@event,
                    senderId: @event.Sender.UserId.ToString(),
                    type: PrivateMessageTypeValue,
                    groupId: null))
            {
                return;
            }

            try
            {
                OnNonCommandPrivateMessageReceived?.Invoke(@event);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception in handling message");
            }
        });
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