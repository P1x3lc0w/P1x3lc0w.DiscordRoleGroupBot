using Discord;
using P1x3lc0w.Common;
using P1x3lc0w.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace P1x3lc0w.DiscordRoleGroupBot.Data
{
    class GuildData
    {
        public ConcurrentDictionary<ulong, bool> GroupRoles = new ConcurrentDictionary<ulong, bool>();
        public ConcurrentDictionary<ulong, ulong> MirrorRoles = new ConcurrentDictionary<ulong, ulong>();
        public ConcurrentDictionary<ulong, ulong?> RoleGroups = new ConcurrentDictionary<ulong, ulong?>();

        public HashSet<ulong> GetRoleGroups(IEnumerable<ulong> roleIds)
        {
            HashSet<ulong> groups = new HashSet<ulong>();
            
            foreach(ulong roleId in roleIds)
            {
                if(RoleGroups.TryGetValue(roleId, out ulong? groupRoleId) && groupRoleId.HasValue)
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

        public void UpdateRoleGroups(IGuild guild)
        {
            List<ComparalbeKeyValuePair<int, IRole>> groupRolePositions = new List<ComparalbeKeyValuePair<int, IRole>>();

            foreach(KeyValuePair<ulong, bool> keyValuePair in GroupRoles)
            {
                if (keyValuePair.Value)
                {
                    IRole role = guild.GetRole(keyValuePair.Key);
                    groupRolePositions.InsertSort(new ComparalbeKeyValuePair<int, IRole>(role.Position, role));
                }
            }

            void UpdateRoleGroup(IRole role)
            {
                if (!IsGroupRole(role))
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

        public bool IsGroupRole(IRole role)
            => IsGroupRole(role.Id);
        public bool IsGroupRole(ulong roleId)
            => GroupRoles.TryGetValue(roleId, out bool isGroupRole) || isGroupRole;
    }
}
