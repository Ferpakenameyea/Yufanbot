namespace Yufanbot.Config;

public interface IEnvironmentVariableProvider
{
    public string? GetEnvironmentVariable(string name);
}