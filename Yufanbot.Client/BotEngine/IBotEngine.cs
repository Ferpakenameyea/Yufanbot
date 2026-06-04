using NapPlana.Core.Data.API;
using Yufanbot.Plugin.Common;

namespace Yufanbot.Client.BotEngine;

internal interface IBotEngine : IBot
{
    public Task StartAsync();
}