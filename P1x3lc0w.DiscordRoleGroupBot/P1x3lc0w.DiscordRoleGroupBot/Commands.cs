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

        [Command("admin config role addall")]
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

                guildData.UpdateRoleGroups(Context.Guild);
            }
            else await ReplyErrorAsync("Failed to get guild data, try again.");
        }

        [Command("admin config defaultcolorrole")]
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

        const int ROLES_PER_PAGE = 30;

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

        [Command("admin debug usergroups")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DumpUserRoleGroups(IGuildUser user)
        {
            if (SourceBot.Data.GuildDictionary.TryGetValue(Context.Guild.Id, out GuildData guildData))
            {
                HashSet<ulong> groupIds = guildData.GetRoleGroups(user.Guild, user.RoleIds);

                StringBuilder stringBuilder = new StringBuilder();

                stringBuilder.Append("```\n");

                foreach(ulong groupId in groupIds)
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

        [Command("admin debug data")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public Task DumpData() 
            => ReplyAsync($"```{JsonConvert.SerializeObject(SourceBot.Data)}```");

        [Command("admin user updateall")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task UpdateAllUsers()
        {
            SourceBot.BotEventHandler.DisableUserUpdate = true;

            IReadOnlyCollection<IGuildUser> users = await Context.Guild.GetUsersAsync();

            int counter = 0;

            foreach (IGuildUser user in users)
            {
                if(counter % 10 == 0)
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


        [Command("admin save")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Save()
        { 
            await File.WriteAllTextAsync("savedata.json", JsonConvert.SerializeObject(SourceBot.Data));
            await ReplySuccessAsync("Saved.");
        }
    }
}
