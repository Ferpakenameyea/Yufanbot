
using NapPlana.Core.Data.API;

namespace Yufanbot.Plugin.Common;

public interface IBot
{
    public Task SendGroupMessageAsync(GroupMessageSend message);
    public Task SendPrivateMessageAsync(PrivateMessageSend message);
}