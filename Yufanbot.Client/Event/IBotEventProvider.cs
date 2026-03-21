using NapPlana.Core.Data.Event.Message;

namespace Yufanbot.Client.Event;

public interface IBotEventProvider
{
    public event Action<GroupMessageEvent>? OnGroupMessageReceived;
    public event Action<PrivateMessageEvent>? OnPrivateMessageReceived;
}