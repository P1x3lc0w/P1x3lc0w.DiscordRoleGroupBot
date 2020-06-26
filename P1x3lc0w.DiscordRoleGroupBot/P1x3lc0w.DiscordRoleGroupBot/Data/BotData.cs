using Discord;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace P1x3lc0w.DiscordRoleGroupBot.Data
{
    internal class BotData
    {
        public ConcurrentDictionary<ulong, GuildData> GuildDictionary = new ConcurrentDictionary<ulong, GuildData>();

        public void UpdateGuildByRole(IRole role, Func<LogMessage, Task> log = null)
        {
            try
            {
                if (GuildDictionary.TryGetValue(role.Guild.Id, out GuildData guildData))
                {
                    guildData.UpdateGroupRoles(role.Guild);
                    guildData.UpdateRoleGroups(role.Guild);
                }
                else
                {
                    log?.Invoke(new LogMessage(LogSeverity.Error, nameof(UpdateGuildByRole), $"Failed to get guild data while updating guild by role {role.Name} ({role.Id})."));
                }
            }
            catch (Exception e)
            {
                log?.Invoke(new LogMessage(LogSeverity.Error, nameof(UpdateGuildByRole), $"Exception while updating guild {e.GetType().FullName}: {e.Message}\n{e.StackTrace}"));
            }
        }
    }
}