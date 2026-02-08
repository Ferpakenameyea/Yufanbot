using Microsoft.Extensions.DependencyInjection;

namespace Yufanbot.Config;

public interface IConfigProvider
{
    public T Resolve<T>() where T : Config<T>;
    public IConfig Resolve(Type type);
}