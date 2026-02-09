using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Yufanbot.Client;
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

var application = new Application(services.BuildServiceProvider());
await application.RunAsync();