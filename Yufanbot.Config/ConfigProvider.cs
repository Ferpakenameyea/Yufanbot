using Microsoft.Extensions.DependencyInjection;

namespace Yufanbot.Config;

public class ConfigProvider(IServiceProvider serviceProvider) : IConfigProvider
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly Dictionary<Type, IConfig?> _cache = [];

    public T Resolve<T>() where T : Config<T>
    {
        T? config = Resolve(typeof(T)) as T;
        return config!;
    }
    
    public IConfig Resolve(Type type)
    {
        if (_cache.TryGetValue(type, out var cachedConfig))
        {
            return cachedConfig!;
        }
        if (!typeof(IConfig).IsAssignableFrom(type))
        {
            throw new ConfigResolveException(type, $"Given type {type} is not a config type. A config type must derive from class Config<SelfType>.");
        }

        IConfig? config = ActivatorUtilities.CreateInstance(_serviceProvider, type) as IConfig;
        _cache[type] = config;
        return config!;
    }
}