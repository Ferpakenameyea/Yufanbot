namespace Yufanbot.Config;

[Serializable]
public class ConfigResolveException(Type configType, string message) : Exception($"failed to resolve configuration type {configType} : {message}")
{
}