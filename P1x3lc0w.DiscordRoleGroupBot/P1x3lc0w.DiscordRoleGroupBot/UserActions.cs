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
            if (user == null)
            {
                log?.Invoke(new LogMessage(LogSeverity.Warning, nameof(UpdateUserRoles), $"User was null."));
                return;
            }

            try
            {
                if (botData.GuildDictionary.TryGetValue(user.Guild.Id, out GuildData guildData))
                {
                    HashSet<ulong> groupIds = guildData.GetRoleGroups(user.Guild, user.RoleIds);

                    IRole highestGroupRole = null;
                    IList<IRole> rolesToAdd = new List<IRole>();
                    IList<IRole> rolesToRemove = new List<IRole>();

                    IReadOnlyCollection<ulong> userRoleIds = user.RoleIds;

                    foreach (ulong groupId in guildData.GetGroups())
                    {
                        if (groupIds.Contains(groupId))
                        {
                            //User has role in group
                            IRole groupRole = user.Guild.GetRole(groupId);

                            if (groupRole.Position > (highestGroupRole?.Position ?? 0))
                            {
                                highestGroupRole = groupRole;
                            }

                            if (!userRoleIds.Contains(groupId))
                                rolesToAdd.Add(groupRole);
                        }
                        else
                        {
                            //User does not have role in group
                            if (userRoleIds.Contains(groupId))
                                rolesToRemove.Add(user.Guild.GetRole(groupId));
                        }
                    }

                    IList<ulong> guildMirrorRoleIds = guildData.GetMirrorRoles().ToList();
                    IRole userMirrorRole = null;
                    IRole highestColorRole = null;

                    if (highestGroupRole != null)
                    {
                        highestColorRole = (from ulong roleId in userRoleIds
                                            where !groupIds.Contains(roleId) && !guildMirrorRoleIds.Contains(roleId)
                                            let role = user.Guild.GetRole(roleId)
                                            where role.Color != Color.Default
                                            orderby role.Position descending
                                            select role)
                                            .FirstOrDefault();

                        if (highestColorRole == null)
                        {
                            IRole DefaultCololRole = guildData.GetDefaultColorRole(user.Guild);

                            if (DefaultCololRole != null)
                            {
                                userMirrorRole = DefaultCololRole;
                            }
                        }
                        else if (highestColorRole.Position < highestGroupRole.Position)
                        {
                            userMirrorRole = await guildData.GetOrCreateMirrorRole(user.Guild, highestColorRole);
                        }

                        if (userMirrorRole != null)
                        {
                            if (!userRoleIds.Contains(userMirrorRole.Id))
                                rolesToAdd.Add(userMirrorRole);
                        }
                    }

                    foreach (ulong mirrorRoleId in guildMirrorRoleIds)
                        if(highestColorRole == null || mirrorRoleId != highestColorRole.Id)
                            if (userMirrorRole == null || mirrorRoleId != userMirrorRole.Id)
                                if (userRoleIds.Contains(mirrorRoleId))
                                {
                                    IRole mirrorRole = user.Guild.GetRole(mirrorRoleId);

                                    if(mirrorRole != null)
                                    {
                                        rolesToRemove.Add(mirrorRole);
                                    }
                                }

                    if (rolesToAdd.Count > 0)
                        await user.AddRolesAsync(rolesToAdd);

                    if (rolesToRemove.Count > 0)
                        await user.RemoveRolesAsync(rolesToRemove);
                }
                else
                {
                    log?.Invoke(new LogMessage(LogSeverity.Error, nameof(UpdateUserRoles), $"Failed to get guild data for {user.Guild.Name} ({user.Guild.Id}) while updating user roles."));
                }
            }
            catch(Exception e)
            {
                log?.Invoke(new LogMessage(LogSeverity.Error, nameof(UpdateUserRoles), $"Exception while updating user {e.GetType().FullName}: {e.Message}\n{e.StackTrace}", e));
            }
           
        }
    }
}
