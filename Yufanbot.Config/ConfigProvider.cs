using Microsoft.Extensions.DependencyInjection;

namespace Yufanbot.Config;

public class ConfigProvider(IServiceProvider serviceProvider) : IConfigProvider
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public T Resolve<T>() where T : Config<T>
    {
        T? config = Resolve(typeof(T)) as T;
        return config!;
    }
    
    public IConfig Resolve(Type type)
    {
        if (!typeof(IConfig).IsAssignableFrom(type))
        {
            throw new ConfigResolveException(type, $"Given type {type} is not a config type. A config type must derive from class Config<SelfType>.");
        }

        IConfig? config = ActivatorUtilities.CreateInstance(_serviceProvider, type) as IConfig;
        return config!;
    }
}