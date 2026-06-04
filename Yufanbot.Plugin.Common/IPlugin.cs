using Nexora.Command.Tree;

namespace Yufanbot.Plugin.Common;

public interface IPlugin
{
    public const string GroupIdEventArg = "<>_group_id";
    public const string SenderIdEventArg = "<>_sender";
    public const string MessageTypeEventArg = "<>_type";
    public const string GroupMessageTypeValue = "<>_type_group";
    public const string PrivateMessageTypeValue = "<>_type_private";
    public const string PluginSuffix = ".yf";
    public void OnInitialize() {}
    public Task OnInitializeAsync() => Task.CompletedTask;
    public void RegisterCommands(RootNode root) {}
}
