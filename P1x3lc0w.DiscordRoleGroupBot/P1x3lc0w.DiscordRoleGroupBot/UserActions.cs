using Discord;
using P1x3lc0w.DiscordRoleGroupBot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P1x3lc0w.DiscordRoleGroupBot
{
    static class UserActions
    {
        public static async Task UpdateUserRoles(IGuildUser user, BotData botData, Func<LogMessage, Task> log = null)
        {
            if(botData.GuildDictionary.TryGetValue(user.Guild.Id, out GuildData guildData))
            {
                HashSet<ulong> groupIds = guildData.GetRoleGroups(user.RoleIds);

                foreach(ulong groupId in guildData.GetGroups())
                {
                    if (groupIds.Contains(groupId))
                    {
                        //User has role in group
                        if (!user.RoleIds.Contains(groupId))
                            await user.AddRoleAsync(user.Guild.GetRole(groupId));
                    }
                    else
                    {
                        //User does not have role in group
                        if (user.RoleIds.Contains(groupId))
                            await user.RemoveRoleAsync(user.Guild.GetRole(groupId));
                    }
                }
            }
            else
            {
                log?.Invoke(new LogMessage(LogSeverity.Error, nameof(UpdateUserRoles), $"Failed to get guild data for {user.Guild.Name} ({user.Guild.Id}) while updating user roles."));
            }
        }
    }
}
