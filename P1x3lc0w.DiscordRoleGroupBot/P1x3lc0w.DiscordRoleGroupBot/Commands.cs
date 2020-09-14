using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using P1x3lc0w.DiscordRoleGroupBot.Data;
using P1x3lc0w.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P1x3lc0w.DiscordRoleGroupBot
{
    public class Commands : ModuleBase<ICommandContext>
    {
        public Bot SourceBot { get; set; }

        private Task ReplyErrorAsync(string text)
            => ReplyAsync($":x: {text}");

        private Task ReplySuccessAsync(string text)
            => ReplyAsync($":white_check_mark: {text}");

        private Task ReplyInfoAsync(string text)
            => ReplyAsync($":information_source: {text}");

        /// <summary>
        /// Adds all roles with a certain prefix as group roles.
        /// </summary>
        /// <param name="prefix">The prefix to filter by.</param>
        [Command("config role addall")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddAllGroupRoles(string prefix)
        {
            foreach (IRole role in Context.Guild.Roles)
            {
                if (role.Name.TrimStart().StartsWith(prefix))
                {
                    await AddGroupRole(role);
                }
            }
        }

        /// <summary>
        /// Add a role as a group role.
        /// </summary>
        /// <param name="role">The role to be added as a group role.</param>
        [Command("config role add")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public Task AddGroupRole(IRole role)
            => SetGroupRoleAsync(role, true);

        /// <summary>
        /// Removes a role as a group role.
        /// </summary>
        /// <param name="role">The role to be removed as a group role.</param>
        [Command("config role remove")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public Task RemoveGroupRole(IRole role)
            => SetGroupRoleAsync(role, false);

        /// <summary>
        /// Sets weather or not a role should be a group role.
        /// </summary>
        /// <param name="role">The role to be set or removed as a group role.</param>
        /// <param name="value">Weather or not the role should be a group role.</param>
        [Command("config role set")]
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

                guildData.UpdateRoleGroups(Context.Guild);
            }
            else await ReplyErrorAsync("Failed to get guild data, try again.");
        }

        /// <summary>
        /// Sets the role to be used as a mirror role if the user does not have a color.
        /// </summary>
        /// <param name="role">The role to be used as a mirror role if the user does not have a color.</param>
        [Command("config defaultcolorrole")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetDefaultColorRole(IRole role)
        {
            if (SourceBot.Data.GuildDictionary.TryGetValue(Context.Guild.Id, out GuildData guildData))
            {
                guildData.DefaultColorRoleId = role.Id;

                await ReplySuccessAsync($"Set default color role to {role.Name} ({role.Id})");
            }
            else await ReplyErrorAsync("Failed to get guild data, try again.");
        }

        /// <summary>
        /// Set if the guild allows custim color roles
        /// </summary>
        /// <param name="value">Whether or not to allow custom color roles.</param>
        [Command("config allowCustomColorRoles")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetAllowCustomColorRoles(bool value)
        {
            if (SourceBot.Data.GuildDictionary.TryGetValue(Context.Guild.Id, out GuildData guildData))
            {
                guildData.AllowCustomColorRoles = value;

                await ReplySuccessAsync($"Set AllowCustomColorRoles to `{value}`");
            }
            else await ReplyErrorAsync("Failed to get guild data, try again.");
        }

        /// <summary>
        /// Set specific Roles to be disallowed as custom color roles.
        /// </summary>
        ///  <param name="role">The role to be allowed or disallowed as a custom color role.</param>
        /// <param name="value">Whether or not to allow the role as a custom color role.</param>
        [Command("config allowCustomColorRole")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetAllowCustomColorRoles(IRole role, bool value)
        {
            if (SourceBot.Data.GuildDictionary.TryGetValue(Context.Guild.Id, out GuildData guildData))
            {
                if (value)
                {
                    if (guildData.DisallowedCustomColorRoles.Remove(role.Id))
                    {
                        await ReplySuccessAsync($"Role `{role.Name}` is now allowed as a custom color role.");
                    }
                    else
                    {
                        await ReplyErrorAsync($"Role `{role.Name}` is already allowed as a custom color role.");
                    }
                }
                else
                {
                    if (guildData.DisallowedCustomColorRoles.Add(role.Id))
                    {
                        await ReplySuccessAsync($"Role `{role.Name}` is now disallowed as a custom color role.");
                    }
                    else
                    {
                        await ReplyErrorAsync($"Role `{role.Name}` is already disallowed as a custom color role.");
                    }
                }
            }
            else await ReplyErrorAsync("Failed to get guild data, try again.");
        }

        /// <summary>
        /// Set a role as the user's custom color role
        /// </summary>
        ///  <param name="role">The role to be set as a custom color role.</param>
        [Command("colorrole")]
        public async Task SetCustomColorRoles(IRole role)
        {
            if (SourceBot.Data.GuildDictionary.TryGetValue(Context.Guild.Id, out GuildData guildData))
            {
                if (guildData.AllowCustomColorRoles)
                {
                    if (guildData.DisallowedCustomColorRoles.Contains(role.Id))
                    {
                        await ReplyErrorAsync($"Sorry {Context.User.Mention}, the admins of this guild have disallowed the role `{role.Name}` to be used as a custom color role.");
                        return;
                    }

                    if (!((IGuildUser)Context.User).RoleIds.Contains(role.Id))
                    {
                        await ReplyErrorAsync($"Sorry {Context.User.Mention}, you don't have the role `{role.Name}`");
                        return;
                    }

                    UserData userData = guildData.Users.GetOrAdd(Context.User.Id, (id) => new UserData());

                    userData.CustomColorRole = role.Id;

                    await UserActions.UpdateUserRoles(Context.User as IGuildUser, SourceBot.Data, SourceBot.Log);

                    await ReplySuccessAsync($"{Context.User.Mention}, set your color role to `{role.Name}`");
                }
                else
                {
                    await ReplyErrorAsync($"Sorry {Context.User.Mention}, the admins of this guild have not enabled custom color roles.");
                }
            }
            else await ReplyErrorAsync("Failed to get guild data, try again.");
        }

        private const int ROLES_PER_PAGE = 30;

        /// <summary>
        /// Dumps information about all roles.
        /// </summary>
        [Command("admin debug roleinfos")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DumpRoleInfos()
        {
            if (SourceBot.Data.GuildDictionary.TryGetValue(Context.Guild.Id, out GuildData guildData))
            {
                int lastPage = Context.Guild.Roles.Count / ROLES_PER_PAGE;

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(":information_source: Role Information:\n");

                IOrderedEnumerable<IRole> roles = from IRole role in Context.Guild.Roles
                                                  orderby role.Position descending
                                                  select role;

                for (int page = 0; page <= lastPage; page++)
                {
                    stringBuilder.Append("Page ");
                    stringBuilder.Append(page + 1);
                    stringBuilder.Append(" of ");
                    stringBuilder.Append(lastPage + 1);
                    stringBuilder.Append("```");

                    foreach (IRole role in roles.Skip(page * ROLES_PER_PAGE).Take(ROLES_PER_PAGE))
                    {
                        stringBuilder.Append(role.Position.ToString("000"));
                        stringBuilder.Append(' ');

                        if (guildData.IsGroupRole(role))
                        {
                            stringBuilder.Append("[GROUP] ");
                            stringBuilder.Append(role.Name);
                            stringBuilder.Append(" (");
                            stringBuilder.Append(role.Id);
                            stringBuilder.Append(")");
                        }
                        else
                        {
                            stringBuilder.Append("        ");
                            stringBuilder.Append(role.Name);
                            stringBuilder.Append(" (");
                            stringBuilder.Append(role.Id);

                            if (guildData.RoleGroups.TryGetValue(role.Id, out ulong? groupRoleId) && groupRoleId.HasValue)
                            {
                                IRole groupRole = Context.Guild.GetRole(groupRoleId.Value);
                                stringBuilder.Append("; Group:");
                                stringBuilder.Append(groupRole.Name);
                            }
                            else
                            {
                                stringBuilder.Append("; No Group");
                            }

                            stringBuilder.Append(")");
                        }

                        stringBuilder.Append('\n');
                    }

                    stringBuilder.Append("```");

                    await ReplyAsync(stringBuilder.ToString());

                    stringBuilder.Clear();
                }
            }
            else await ReplyErrorAsync("Failed to get guild data, try again.");
        }

        /// <summary>
        /// Dumps information about the role groups of a user.
        /// </summary>
        [Command("admin debug usergroups")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DumpUserRoleGroups(IGuildUser user)
        {
            if (SourceBot.Data.GuildDictionary.TryGetValue(Context.Guild.Id, out GuildData guildData))
            {
                HashSet<ulong> groupIds = guildData.GetRoleGroups(user.Guild, user.RoleIds);

                StringBuilder stringBuilder = new StringBuilder();

                stringBuilder.Append("```\n");

                foreach (ulong groupId in groupIds)
                {
                    IRole groupRole = Context.Guild.GetRole(groupId);

                    stringBuilder.Append(groupRole.Name);
                    stringBuilder.Append(" (");
                    stringBuilder.Append(groupRole.Id);
                    stringBuilder.Append(")\n");
                }

                stringBuilder.Append("```");

                await ReplyAsync(stringBuilder.ToString());
            }
            else await ReplyErrorAsync("Failed to get guild data, try again.");
        }

        /// <summary>
        /// Dumps bot data.
        /// </summary>
        /// <returns></returns>
        [Command("admin debug data")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public Task DumpData()
            => ReplyAsync($"```{JsonConvert.SerializeObject(SourceBot.Data)}```");

        /// <summary>
        /// Forces an update for all users.
        /// </summary>
        [Command("admin user updateall")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task UpdateAllUsers()
        {
            SourceBot.BotEventHandler.DisableUserUpdate = true;

            IReadOnlyCollection<IGuildUser> users = await Context.Guild.GetUsersAsync();

            int counter = 0;

            foreach (IGuildUser user in users)
            {
                if (counter % 10 == 0)
                {
                    await ReplyInfoAsync($"Updating users ({counter}/{users.Count})");
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }

                await UserActions.UpdateUserRoles(user, SourceBot.Data,
                    async (msg) =>
                    {
                        switch (msg.Severity)
                        {
                            case LogSeverity.Critical:
                            case LogSeverity.Error:
                                await ReplyErrorAsync(msg.Message);
                                break;

                            default:
                                await ReplyInfoAsync(msg.Message);
                                break;
                        }
                    }
                );

                counter++;
            }

            await ReplyInfoAsync("Updated all users.");
            SourceBot.BotEventHandler.DisableUserUpdate = false;
        }

        /// <summary>
        /// Forces an update for a user.
        /// </summary>
        /// <param name="user">The user to be updated.</param>
        [Command("admin user update")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task UpdateUser(IGuildUser user)
        {
            await UserActions.UpdateUserRoles(user, SourceBot.Data,
                    async (msg) =>
                    {
                        switch (msg.Severity)
                        {
                            case LogSeverity.Critical:
                            case LogSeverity.Error:
                                await ReplyErrorAsync(msg.Message);
                                break;

                            default:
                                await ReplyInfoAsync(msg.Message);
                                break;
                        }
                    }
                );

            await ReplyInfoAsync("User update started.");
        }

        /// <summary>
        /// Saves the bot's data.
        /// </summary>
        /// <returns></returns>
        [Command("admin save")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Save()
        {
            if (SourceBot.BotConfig.IsAdministrator(Context.User))
            {
                await File.WriteAllTextAsync("savedata.json", JsonConvert.SerializeObject(SourceBot.Data));
                await ReplySuccessAsync("Saved.");
            }
            else
            {
                await ReplyErrorAsync($"Sorry {Context.User.Mention}, only bot administrators are allowed to use this command.");
            }
        }
    }
}