
using NapPlana.Core.Bot;
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

    public Task StartAsync() => _napBot.StartAsync();
}