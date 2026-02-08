using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NapPlana.Core.Bot;
using Yufanbot.Client.Config;

namespace Yufanbot.Client;

public sealed class Application
{
    private readonly ServiceProvider _services;
    private readonly CoreConfig _coreConfig;
    private readonly NapBot _bot;
    private readonly ILogger _logger;

    private Application(ServiceProvider services)
    {
        _services = services;
        _coreConfig = services.GetRequiredService<CoreConfig>();
        _logger = services.GetRequiredService<LoggerFactory>().CreateLogger<Application>();
        _bot = PlanaBotFactory.Create()
            .SetSelfId(_coreConfig.SelfId)
            .SetConnectionType(NapPlana.Core.Data.BotConnectionType.WebSocketClient)
            .SetIp(_coreConfig.NapcatIP)
            .SetPort(_coreConfig.NapcatPort)
            .SetToken(_coreConfig.NapcatToken)
            .Build();
    }
}