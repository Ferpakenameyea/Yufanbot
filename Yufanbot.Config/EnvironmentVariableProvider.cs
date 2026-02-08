namespace Yufanbot.Config;

public class EnvironmentVariableProvider : IEnvironmentVariableProvider
{
    public string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}