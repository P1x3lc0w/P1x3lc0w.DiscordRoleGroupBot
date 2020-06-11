using Discord;
using Discord.WebSocket;
using P1x3lc0w.DiscordRoleGroupBot.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace P1x3lc0w.DiscordRoleGroupBot
{
    class BotEventHandler
    {
        public Bot SourceBot { get; private set; }

        public BotEventHandler(Bot sourceBot)
        {
            SourceBot = sourceBot;
        }

        public Task OnRoleCreated(IRole role)
        {
            SourceBot.Data.UpdateGuildByRole(role, SourceBot.Log);
            return Task.CompletedTask;
        }

        internal Task OnRoleUpdated(IRole before, IRole after)
        {
            SourceBot.Data.UpdateGuildByRole(after, SourceBot.Log);
            return Task.CompletedTask;
        }

        internal Task OnRoleDeleted(IRole role)
        {
            SourceBot.Data.UpdateGuildByRole(role, SourceBot.Log);
            return Task.CompletedTask;
        }

        internal Task OnGuildAvailable(IGuild guild)
        {
            if (!SourceBot.Data.GuildDictionary.ContainsKey(guild.Id))
            {
                SourceBot.Data.GuildDictionary.TryAdd(guild.Id, new GuildData());
            }

            return Task.CompletedTask;
        }

        internal async Task OnGuildMemberUpdated(IGuildUser before, IGuildUser after) 
        {
            //await UserActions.UpdateUserRoles(after, SourceBot.Data, SourceBot.Log);
        }

        internal async Task OnLoggedIn()
        {
            _ = SourceBot.Log(new LogMessage(LogSeverity.Info, nameof(BotEventHandler.OnLoggedIn), "Bot logged in."));
            await SourceBot.SocketClient.StartAsync();
        }
    }
}
