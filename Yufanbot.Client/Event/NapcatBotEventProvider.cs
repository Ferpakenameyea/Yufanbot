using NapPlana.Core.Data.Event.Message;
using NapPlana.Core.Event.Handler;

namespace Yufanbot.Client.Event;

public sealed class NapcatBotEventProvider : IBotEventProvider
{
    public event Action<GroupMessageEvent>? OnGroupMessageReceived
    {
        add => BotEventHandler.OnGroupMessageReceived += value;
        remove => BotEventHandler.OnGroupMessageReceived -= value;
    }
    public event Action<PrivateMessageEvent>? OnPrivateMessageReceived
    {
        add => BotEventHandler.OnPrivateMessageReceived += value;
        remove =>  BotEventHandler.OnPrivateMessageReceived -= value;
    }
}