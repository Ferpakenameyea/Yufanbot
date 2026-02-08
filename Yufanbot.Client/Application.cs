using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NapPlana.Core.Bot;
using Yufanbot.Client.Config;
using Yufanbot.Config;

namespace Yufanbot.Client;

public sealed class Application
{
    private readonly IConfigProvider _configProvider;
    private readonly CoreConfig _coreConfig;
    private readonly NapBot _bot;
    private readonly ILogger<Application> _logger;

    public Application(ServiceProvider services)
    {
        _logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<Application>();        
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

    public async Task RunAsync()
    {
        await _bot.StartAsync();
        while (true)
        {
            await Task.Delay(200);
        }
    }
}