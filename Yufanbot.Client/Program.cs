using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Yufanbot.Client;
using Yufanbot.Client.BotEngine;
using Yufanbot.Client.Event;
using Yufanbot.Config;
using Yufanbot.Plugin;

ServiceCollection services = new();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .CreateLogger();

services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddSerilog(); 
});

services.AddSingleton<IFileReader, FileReader>();
services.AddSingleton<IEnvironmentVariableProvider, EnvironmentVariableProvider>();
services.AddSingleton<IConfigProvider, ConfigProvider>();
services.AddSingleton<IPluginCompiler, PluginCompiler>();
services.AddSingleton<IBotEventProvider, NapcatBotEventProvider>();
services.AddSingleton<IBotEngine, NapcatBotEngine>();

var application = new Application(services.BuildServiceProvider());
await application.RunAsync();