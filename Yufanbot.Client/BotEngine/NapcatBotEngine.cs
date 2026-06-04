using NapPlana.Core.Bot;
using NapPlana.Core.Bot.BotInstance;
using NapPlana.Core.Data.API;
using Yufanbot.Client.Config;
using Yufanbot.Config;

namespace Yufanbot.Client.BotEngine;

internal sealed class NapcatBotEngine : IBotEngine
{
    private readonly NapBot _napBot;

    public NapcatBotEngine(IConfigProvider configProvider)
    {
        CoreConfig coreConfig = configProvider.Resolve<CoreConfig>();
        _napBot = PlanaBotFactory.Create()
            .SetSelfId(coreConfig.SelfId)
            .SetConnectionType(NapPlana.Core.Data.BotConnectionType.WebSocketClient)
            .SetIp(coreConfig.NapcatIP)
            .SetPort(coreConfig.NapcatPort)
            .SetToken(coreConfig.NapcatToken)
            .Build();
    }

#pragma warning disable CS0618
    public Task StartAsync() => _napBot.StartAsync();
#pragma warning restore CS0618
    public Task SendGroupMessageAsync(GroupMessageSend message) => _napBot.SendGroupMessageAsync(message);
    public Task SendPrivateMessageAsync(PrivateMessageSend message) => _napBot.SendPrivateMessageAsync(message);
}