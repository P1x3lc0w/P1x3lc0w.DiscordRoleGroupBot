using Newtonsoft.Json;
using System.IO;

namespace P1x3lc0w.DiscordRoleGroupBot.Data
{
    public struct BotConfig
    {
        public readonly string token;
        public readonly ulong[] administrators;

        public BotConfig(string token, ulong[] administrators)
        {
            this.token = token;
            this.administrators = administrators;
        }

        public static BotConfig ReadConfigFromFile(string filePath)
            => JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(filePath));

        public bool IsAdministrator(IUser user)
            => this.administrators.Any(id => id == user.Id);
    }
}