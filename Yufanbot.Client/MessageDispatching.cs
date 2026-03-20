using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.Logging;
using NapPlana.Core.Data.Event.Message;
using NapPlana.Core.Data.Message;
using Nexora.Command.Executor;
using Nexora.Command.Tree;
using Yufanbot.Client.Config;
using Yufanbot.Plugin.Common;
using Yufanbot.Plugin.Common.Registration;

internal static class MessageDispatching
{
    /// <summary>
    /// Try to execute the raw content of a message event as a command
    /// </summary>
    /// <param name="e"></param>
    /// <returns>If the content of the message event is a recognized command pattern</returns>
    private static bool TryExecuteCommand(
        MessageEventBase e, 
        CoreConfig coreConfig, 
        RootNode commandTreeRoot,
        ILogger logger)
    {
        var messages = e.Messages;
        if (messages.Count != 1 || messages[0] is not TextMessage textMessage)
        {
            return false;
        }

        var text = (textMessage.MessageData as TextMessageData)?.Text;

        if (text == null || text.StartsWith(coreConfig.CommandPrefix) != true)
        {
            return false;
        }

        var rawCommand = text[coreConfig.CommandPrefix.Length..];
        try
        {
            CommandLexer commandLexer = new(rawCommand);
            CommandLocator locator = new(commandTreeRoot);

            var execution = locator.Locate(commandLexer);
            if (execution == null)
            {
                logger.LogInformation("Command not found: {raw}", rawCommand);
                return true;
            }

            var result = execution.Value.Invoke();
            if (result.IsSuccess)
            {
                logger.LogInformation("Command executed: {raw}", rawCommand);
            }
            else
            {
                logger.LogWarning(
                    "Execution failed: {exception}",
                    result.Error);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in command execution. Raw command: {raw}", rawCommand);
        }

        return true;
    }

    internal static Action<T> BuildEventDispatcher<T>(
        IEnumerable<(MethodInfo Listener, ListenToEventAttribute Attribute, IPlugin Instance)> registrations,
        ILogger logger,
        CoreConfig coreConfig,
        RootNode commandTreeRoot) where T : MessageEventBase
    {
        ImmutableList<Func<T, bool>> invokeList = registrations
            .OrderByDescending(r => r.Attribute.Priority)
            .Select(r => BuildDelegate<T>(r.Listener, r.Instance, logger))
            .OfType<Func<T, bool>>()
            .ToImmutableList();

        if (coreConfig.ShowConfigRegistration)
        {
            var names = invokeList.Select(invoke => " - " + invoke.Method.Name);
            var display = string.Join(",\n", names);
            logger.LogInformation("Registration result group:\n{}", display);
        }

        return e =>
        {
            if (TryExecuteCommand(
                e,
                coreConfig,
                commandTreeRoot,
                logger))
            {
                return;
            }

            foreach (var listener in invokeList)
            {
                try
                {
                    bool intercepted = listener.Invoke(e);
                    if (intercepted)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in event handling.");
                }
            }
        };
    }

    private static Func<T, bool>? BuildDelegate<T>(MethodInfo method, IPlugin invocationInstance, ILogger logger)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != 1 || !parameters[0].ParameterType.IsAssignableFrom(typeof(T)))
        {
            logger.LogError(
                "Registration failed: Method '{}'(from assembly '{}', in class '{}') " + 
                "should have only exactly one parameter and should have type of {}!",
                method.Name,
                method.DeclaringType?.Assembly.FullName,
                method.DeclaringType?.FullName,
                typeof(T).FullName);
            return null;
        }

        if (method.ReturnType != typeof(bool))
        {
            logger.LogError(
                "Registration failed: Method '{}'(from assembly '{}', in class '{}') " + 
                "should return bool!",
                method.Name,
                method.DeclaringType?.Assembly.FullName,
                method.DeclaringType?.FullName);
            return null;
        }
        try
        {
            if (method.IsStatic) {
                return method.CreateDelegate<Func<T, bool>>();
            }

            return method.CreateDelegate<Func<T, bool>>(invocationInstance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "Method bind failed: Cannot create an instance of delegate of method" + 
                " '{}'(from assembly '{}', in class '{}')",
                method.Name,
                method.DeclaringType?.Assembly.FullName,
                method.DeclaringType?.FullName);
            return null;
        }
    }
}