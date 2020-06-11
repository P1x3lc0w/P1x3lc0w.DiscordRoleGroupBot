using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace P1x3lc0w.DiscordRoleGroupBot.Data
{
    class BotData
    {
        public ConcurrentDictionary<ulong, GuildData> GuildDictionary = new ConcurrentDictionary<ulong, GuildData>();
    }
}
