using Discord;
using Discord.Commands;
using P1x3lc0w.DiscordRoleGroupBot.Data;
using P1x3lc0w.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace P1x3lc0w.DiscordRoleGroupBot
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        public Bot SourceBot { get; set; }

        private Task ReplyErrorAsync(string text)
            => ReplyAsync($":x: {text}");

        private Task ReplySuccessAsync(string text)
            => ReplyAsync($":white_check_mark: {text}");

        private Task ReplyInfoAsync(string text)
            => ReplyAsync($":information_source: {text}");

        [Command("admin config role add")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public Task AddGroupRole(IRole role)
            => SetGroupRoleAsync(role, true);

        [Command("admin config role remove")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public Task RemoveGroupRole(IRole role)
            => SetGroupRoleAsync(role, false);

        [Command("admin config role set")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetGroupRoleAsync(IRole role, bool value)
        {
            if (SourceBot.Data.GuildDictionary.TryGetValue(Context.Guild.Id, out GuildData guildData))
            {
                if (guildData.GroupRoles.AddOrUpdate(role.Id, value))
                {
                    await ReplySuccessAsync($"Role {role.Name} is now {(value ? "" : "not ")}a group role.");
                }
                else
                {
                    await ReplyInfoAsync($"Role {role.Name} is already {(value ? "" : "not ")}a group role.");
                }
            }
            else await ReplyErrorAsync("Failed to get guild data, try again.");
        } 
    }
}
