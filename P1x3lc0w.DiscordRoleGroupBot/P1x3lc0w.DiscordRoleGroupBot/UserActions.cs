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
                HashSet<ulong> groupIds = guildData.GetRoleGroups(user.Guild, user.RoleIds);

                IRole highestGroupRole = null;
                IList<IRole> rolesToAdd = new List<IRole>();
                IList<IRole> rolesToRemove = new List<IRole>();

                foreach (ulong groupId in guildData.GetGroups())
                {
                    if (groupIds.Contains(groupId))
                    {
                        //User has role in group
                        IRole groupRole = user.Guild.GetRole(groupId);
                        
                        if(groupRole.Position > (highestGroupRole?.Position ?? 0))
                        {
                            highestGroupRole = groupRole;
                        }

                        if (!user.RoleIds.Contains(groupId))
                            rolesToAdd.Add(groupRole);
                    }
                    else
                    {
                        //User does not have role in group
                        if (user.RoleIds.Contains(groupId))
                            rolesToRemove.Add(user.Guild.GetRole(groupId));
                    }
                }

                if (rolesToAdd.Count > 0)
                    await user.AddRolesAsync(rolesToAdd);

                if (rolesToRemove.Count > 0)
                    await user.RemoveRolesAsync(rolesToRemove);

                if(highestGroupRole != null)
                {
                    IRole highestColorRole = (from ulong roleId in user.RoleIds
                                              where !groupIds.Contains(roleId)
                                              let role = user.Guild.GetRole(roleId)
                                              where role.Color != Color.Default
                                              orderby role.Position descending
                                              select role)
                                              .FirstOrDefault();

                    if(highestColorRole == null)
                    {
                        IRole DefaultCololRole = guildData.GetDefaultColorRole(user.Guild);

                        if(DefaultCololRole != null)
                        {
                            await user.AddRoleAsync(DefaultCololRole);
                        }
                    }
                    else if (highestColorRole.Position < highestGroupRole.Position)
                    {
                        await user.AddRoleAsync(await guildData.GetOrCreateMirrorRole(user.Guild, highestColorRole));
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
