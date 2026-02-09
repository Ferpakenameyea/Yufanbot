namespace Yufanbot.Plugin;

public interface IPluginCompiler
{
    Task<Common.YFPlugin?> CompilePluginAsync(string path);
}