using Discord;
using P1x3lc0w.Common;
using P1x3lc0w.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace P1x3lc0w.DiscordRoleGroupBot.Data
{
    internal class GuildData
    {
        public ConcurrentDictionary<ulong, bool> GroupRoles { get; set; } = new ConcurrentDictionary<ulong, bool>();
        public ConcurrentDictionary<ulong, ulong?> MirrorRoles { get; set; } = new ConcurrentDictionary<ulong, ulong?>();
        public ConcurrentDictionary<ulong, ulong?> RoleGroups { get; set; } = new ConcurrentDictionary<ulong, ulong?>();
        public ulong DefaultColorRoleId { get; set; }

        public IRole GetDefaultColorRole(IGuild guild)
            => guild.GetRole(DefaultColorRoleId);

        public int GetMirrorRolePosition(IGuild guild)
            => (from keyValue in GroupRoles
                where keyValue.Value
                let role = guild.GetRole(keyValue.Key)
                orderby role.Position descending
                select role.Position)
                .FirstOrDefault() + 1;

        public async Task<IRole> GetOrCreateMirrorRole(IGuild guild, IRole role)
        {
            if (MirrorRoles.TryGetValue(role.Id, out ulong? mirrorRoleId) && mirrorRoleId.HasValue)
            {
                IRole mirrorRole = guild.GetRole(mirrorRoleId.Value);

                if (mirrorRole == null)
                {
                    MirrorRoles.TryUpdate(role.Id, null, mirrorRoleId);
                    return await GetOrCreateMirrorRole(guild, role);
                }
                else
                {
                    return mirrorRole;
                }
            }
            else
            {
                IRole mirrorRole = await guild.CreateRoleAsync("▸" + role.Name, GuildPermissions.None, role.Color, false, null);

                bool updateFailed = false;

                MirrorRoles.AddOrUpdate(
                        role.Id,
                        (roleId) => mirrorRole.Id,
                        (roleId, previouisValue) =>
                        {
                            if (previouisValue.HasValue)
                            {
                                updateFailed = true;
                                return previouisValue;
                            }
                            else
                            {
                                return mirrorRole.Id;
                            }
                        }
                    );

                if (!updateFailed)
                {
                    await mirrorRole.ModifyAsync(
                            (roleProperties) =>
                            {
                                roleProperties.Position = GetMirrorRolePosition(guild);
                            }
                        );

                    return mirrorRole;
                }
                else
                {
                    return await GetOrCreateMirrorRole(guild, role);
                }
            }
        }

        public HashSet<ulong> GetRoleGroups(IGuild guild, IEnumerable<ulong> roleIds)
        {
            HashSet<ulong> groups = new HashSet<ulong>();

            foreach (ulong roleId in roleIds)
            {
                if (roleId == guild.EveryoneRole.Id)
                    continue;

                if (RoleGroups.TryGetValue(roleId, out ulong? groupRoleId) && groupRoleId.HasValue)
                {
                    groups.Add(groupRoleId.Value);
                }
            }

            return groups;
        }

        public IEnumerable<ulong> GetGroups()
        {
            foreach (KeyValuePair<ulong, bool> keyValuePair in GroupRoles)
                if (keyValuePair.Value)
                    yield return keyValuePair.Key;
        }

        public IEnumerable<ulong> GetMirrorRoles()
        {
            foreach (KeyValuePair<ulong, ulong?> keyValuePair in MirrorRoles)
                if (keyValuePair.Value.HasValue)
                    yield return keyValuePair.Value.Value;
        }

        public void UpdateRoleGroups(IGuild guild)
        {
            List<ComparalbeKeyValuePair<int, IRole>> groupRolePositions = new List<ComparalbeKeyValuePair<int, IRole>>();

            foreach (KeyValuePair<ulong, bool> keyValuePair in GroupRoles)
            {
                if (keyValuePair.Value)
                {
                    IRole role = guild.GetRole(keyValuePair.Key);
                    groupRolePositions.InsertSort(new ComparalbeKeyValuePair<int, IRole>(role.Position, role));
                }
            }

            void UpdateRoleGroup(IRole role)
            {
                if (IsGroupRole(role) || role.Id == role.Guild.EveryoneRole.Id)
                {
                    RoleGroups.AddOrUpdate(role.Id, null);
                }
                else
                {
                    foreach (ComparalbeKeyValuePair<int, IRole> groupRolePosition in groupRolePositions)
                    {
                        if (role.Position < groupRolePosition.key)
                        {
                            RoleGroups.AddOrUpdate(role.Id, groupRolePosition.value.Id);
                            return;
                        }
                    }

                    RoleGroups.AddOrUpdate(role.Id, null);
                }
            }

            foreach (IRole role in guild.Roles)
                UpdateRoleGroup(role);
        }

        public void UpdateGroupRoles(IGuild guild)
        {
            foreach (KeyValuePair<ulong, bool> keyValuePair in GroupRoles)
            {
                if (guild.GetRole(keyValuePair.Key) == null)
                {
                    GroupRoles.TryRemove(keyValuePair.Key, out _);
                }
            }
        }

        public bool IsGroupRole(IRole role)
            => IsGroupRole(role.Id);

        public bool IsGroupRole(ulong roleId)
            => GroupRoles.TryGetValue(roleId, out bool isGroupRole) || isGroupRole;
    }
}