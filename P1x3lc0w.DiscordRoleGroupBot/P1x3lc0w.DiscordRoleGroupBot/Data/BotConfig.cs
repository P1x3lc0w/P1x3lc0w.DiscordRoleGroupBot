using Newtonsoft.Json;
using System.IO;

namespace P1x3lc0w.DiscordRoleGroupBot.Data
{
    public struct BotConfig
    {
        public string token;

        public static BotConfig LoadConfigFromFile(string filePath)
            => JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(filePath));
    }
}