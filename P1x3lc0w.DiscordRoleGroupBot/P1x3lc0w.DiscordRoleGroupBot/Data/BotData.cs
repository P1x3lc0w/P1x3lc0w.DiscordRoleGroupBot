using Discord;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace P1x3lc0w.DiscordRoleGroupBot.Data
{
    class BotData
    {
        public ConcurrentDictionary<ulong, GuildData> GuildDictionary = new ConcurrentDictionary<ulong, GuildData>();

        public void UpdateGuildByRole(IRole role, Func<LogMessage, Task> log = null)
        {
            if (GuildDictionary.TryGetValue(role.Guild.Id, out GuildData guildData))
            {
                guildData.UpdateRoleGroups(role.Guild);
            }
            else
            {
                log?.Invoke(new LogMessage(LogSeverity.Error, nameof(UpdateGuildByRole), $"Failed to get guild data while updating guild by role {role.Name} ({role.Id})."));
            }
        }
    }
}
