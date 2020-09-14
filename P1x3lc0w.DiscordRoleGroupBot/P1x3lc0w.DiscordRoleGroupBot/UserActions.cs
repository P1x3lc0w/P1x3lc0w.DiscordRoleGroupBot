using Discord;
using P1x3lc0w.DiscordRoleGroupBot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace P1x3lc0w.DiscordRoleGroupBot
{
    internal static class UserActions
    {
        /// <summary>
        /// Updates a user's roles:
        ///     - Adds any necessary group roles beased on the user's roles.
        ///     - Adds any unnecessary group roles.
        ///     - Adds or removes mirror roles to keep the user's color.
        /// </summary>
        /// <param name="user">The user to be updated.</param>
        /// <param name="botData">The bot data, contains the guild's settings for the bot.</param>
        /// <param name="log">A callback to be called to report log messages.</param>
        public static async Task UpdateUserRoles(IGuildUser user, BotData botData, Func<LogMessage, Task> log = null)
        {
            //User should not be null, we can only update an exsiting user.
            if (user == null)
            {
                log?.Invoke(new LogMessage(LogSeverity.Warning, nameof(UpdateUserRoles), $"User was null."));
                return;
            }

            try
            {
                //Get the guild's settings for the bot.
                if (botData.GuildDictionary.TryGetValue(user.Guild.Id, out GuildData guildData))
                {
                    //Get a list of all the group roles a user should be in based on their roles.
                    HashSet<ulong> groupIds = guildData.GetRoleGroups(user.Guild, user.RoleIds);

                    IRole highestGroupRole = null;
                    IList<IRole> rolesToAdd = new List<IRole>();
                    IList<IRole> rolesToRemove = new List<IRole>();

                    IReadOnlyCollection<ulong> userRoleIds = user.RoleIds;

                    //Go through all group roles to see wich should be added or removed.
                    foreach (ulong groupId in guildData.GetGroups())
                    {
                        if (groupIds.Contains(groupId))
                        {
                            //User has a role in this group.

                            IRole groupRole = user.Guild.GetRole(groupId);

                            //Keep track of the highest group role of the user,
                            //to decide if we need to add a mirror role later.
                            if (groupRole.Position > (highestGroupRole?.Position ?? 0))
                            {
                                highestGroupRole = groupRole;
                            }

                            //Add the group role if the user does not already have it.
                            if (!userRoleIds.Contains(groupId))
                                rolesToAdd.Add(groupRole);
                        }
                        else
                        {
                            //User does not have role in group.

                            //Remove the unnecessary if the user has it.
                            if (userRoleIds.Contains(groupId))
                                rolesToRemove.Add(user.Guild.GetRole(groupId));
                        }
                    }

                    IList<ulong> guildMirrorRoleIds = guildData.GetMirrorRoles().ToList();

                    IRole userMirrorRole = 
                        await GetMirrorRole(
                            user: user,
                            userRoleIds: userRoleIds,
                            guildMirrorRoleIds: guildMirrorRoleIds,
                            groupIds: groupIds,
                            guildData: guildData,
                            highestGroupRole: highestGroupRole,
                            rolesToAdd: rolesToAdd,
                            guild: user.Guild
                        );


                    //Go through all of the guild's mirror roles to remove any unecessary ones.
                    foreach (ulong mirrorRoleId in guildMirrorRoleIds)
                        //We should only remove a mirror role if its not the user's current mirror role.
                        if (userMirrorRole == null || mirrorRoleId != userMirrorRole.Id)
                            if (userRoleIds.Contains(mirrorRoleId))
                            {
                                IRole mirrorRole = user.Guild.GetRole(mirrorRoleId);

                                if (mirrorRole != null)
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
            catch (Exception e)
            {
                log?.Invoke(new LogMessage(LogSeverity.Error, nameof(UpdateUserRoles), $"Exception while updating user {e.GetType().FullName}: {e.Message}\n{e.StackTrace}", e));
            }
        }

        private static async Task<IRole> GetMirrorRole(
            IGuildUser user,
            IReadOnlyCollection<ulong> userRoleIds,
            IList<ulong> guildMirrorRoleIds,
            HashSet<ulong> groupIds,
            GuildData guildData,
            IRole highestGroupRole,
            IList<IRole> rolesToAdd,
            IGuild guild
        )
        {

            IRole userMirrorRole = null;

            if (guildData.AllowCustomColorRoles)
            {
                if(guildData.Users.TryGetValue(user.Id, out UserData userData))
                {
                    if (
                        userData.CustomColorRole > 10 &&
                        !guildData.DisallowedCustomColorRoles.Contains(userData.CustomColorRole) &&
                        userRoleIds.Contains(userData.CustomColorRole)
                        )
                    {
                        userMirrorRole = await guildData.GetOrCreateMirrorRole(guild, guild.GetRole(userData.CustomColorRole));
                    }
                }
            }

            //If the user does not have a group role, we do not need to add a mirror role.
            if (highestGroupRole != null)
            {
                IRole highestColorRole = null;

                //Get the highest role with the following conditions:
                //  - Is not a group role.
                //  - Is not a mirror role.
                //  - Has a color (Other that default).
                highestColorRole = (from ulong roleId in userRoleIds
                                    where !groupIds.Contains(roleId) && !guildMirrorRoleIds.Contains(roleId)
                                    let role = user.Guild.GetRole(roleId)
                                    where role.Color != Color.Default
                                    orderby role.Position descending
                                    select role)
                                    .FirstOrDefault();

                if (highestColorRole == null)
                {
                    //If the user does not have a role that would give them a color,
                    //but still has a group role, their default color would get overridden.
                    //Therefore we see if the guild has a default color role configured.

                    IRole DefaultColorRole = guildData.GetDefaultColorRole(user.Guild);

                    if (DefaultColorRole != null)
                    {
                        userMirrorRole = DefaultColorRole;
                    }
                }
                else if (highestColorRole.Position < highestGroupRole.Position)
                {
                    //If the user's highest color role is below is highest group role their color would get overridden.
                    //To avoid this we get or create a mirror role to restore their color.

                    userMirrorRole = await guildData.GetOrCreateMirrorRole(user.Guild, highestColorRole);
                }
            }

            if (userMirrorRole != null)
            {
                if (!userRoleIds.Contains(userMirrorRole.Id))
                    rolesToAdd.Add(userMirrorRole);
            }

            return userMirrorRole;
        }
    }
}