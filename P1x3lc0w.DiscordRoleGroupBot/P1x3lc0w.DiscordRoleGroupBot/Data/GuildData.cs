using Discord;
using P1x3lc0w.Common;
using P1x3lc0w.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace P1x3lc0w.DiscordRoleGroupBot.Data
{
    class GuildData
    {
        public ConcurrentDictionary<ulong, bool> GroupRoles = new ConcurrentDictionary<ulong, bool>();
        public ConcurrentDictionary<ulong, ulong> MirrorRoles = new ConcurrentDictionary<ulong, ulong>();
        public ConcurrentDictionary<ulong, ulong> RoleGroups = new ConcurrentDictionary<ulong, ulong>();

        public void UpdateRoleGroups(IGuild guild)
        {
            List<ComparalbeKeyValuePair<int, IRole>> groupRolePositions = new List<ComparalbeKeyValuePair<int, IRole>>();

            foreach(KeyValuePair<ulong, bool> keyValuePair in GroupRoles)
            {
                IRole role = guild.GetRole(keyValuePair.Key);
                groupRolePositions.InsertSort(new ComparalbeKeyValuePair<int, IRole>());
            }
        }
    }
}
