﻿using Discord;
using P1x3lc0w.DiscordRoleGroupBot.Data;
using System.Threading.Tasks;

namespace P1x3lc0w.DiscordRoleGroupBot
{
    internal class BotEventHandler
    {
        public Bot SourceBot { get; private set; }

        public bool DisableUserUpdate { get; set; } = false;

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
            if (!DisableUserUpdate)
                await UserActions.UpdateUserRoles(after, SourceBot.Data, SourceBot.Log);
        }

        internal async Task OnLoggedIn()
        {
            _ = SourceBot.Log(new LogMessage(LogSeverity.Info, nameof(BotEventHandler.OnLoggedIn), "Bot logged in."));
            await SourceBot.SocketClient.StartAsync();
        }
    }
}