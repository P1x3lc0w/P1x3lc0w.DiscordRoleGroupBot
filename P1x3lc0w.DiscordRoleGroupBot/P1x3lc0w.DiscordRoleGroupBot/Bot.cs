using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using P1x3lc0w.DiscordRoleGroupBot.Data;
using System;
using System.IO;
using System.Threading.Tasks;

namespace P1x3lc0w.DiscordRoleGroupBot
{
    public class Bot
    {
        public DiscordSocketClient SocketClient { get; private set; }
        internal BotEventHandler BotEventHandler { get; private set; }

        internal BotData Data { get; private set; }
        private BotConfig _config;

        public Bot()
        {
            CreateOrLoadData();

            SocketClient = new DiscordSocketClient();
            BotEventHandler = new BotEventHandler(this);

            SocketClient.Log += Log;

            SocketClient.LoggedIn += BotEventHandler.OnLoggedIn;

            SocketClient.GuildAvailable += BotEventHandler.OnGuildAvailable;

            SocketClient.RoleCreated += BotEventHandler.OnRoleCreated;
            SocketClient.RoleUpdated += BotEventHandler.OnRoleUpdated;
            SocketClient.RoleDeleted += BotEventHandler.OnRoleDeleted;

            SocketClient.GuildMemberUpdated += BotEventHandler.OnGuildMemberUpdated;

            ServiceCollection services = new ServiceCollection();

            services
                .AddSingleton(SocketClient)
                .AddSingleton<CommandHandler>()
                .AddSingleton(this)
                .AddSingleton(new CommandService(new CommandServiceConfig
                {                                       // Add the command service to the collection
                    LogLevel = LogSeverity.Verbose,     // Tell the logger to give Verbose amount of info
                    DefaultRunMode = RunMode.Async,     // Force all commands to run async by default
                }));

            ServiceProvider provider = services.BuildServiceProvider();
            _ = provider.GetRequiredService<CommandHandler>().InstallCommandsAsync();
        }

        private void CreateOrLoadData()
        {
#if DEBUG
            _config = BotConfig.LoadConfigFromFile("botconfig.DEBUG.json");
#else
            _config = BotConfig.LoadConfigFromFile("botconfig.json");
#endif

            Data = File.Exists("savedata.json") ?
                JsonConvert.DeserializeObject<BotData>(File.ReadAllText("savedata.json")) :
                new BotData();
        }

        public Task StartBot()
        {
            SocketClient.LoginAsync(TokenType.Bot, _config.token);
            return Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromHours(5));

                    if (File.Exists("savedata.json"))
                        File.Move("savedata.json", "savedata.old.josn", true);

                    await File.WriteAllTextAsync("savedata.json", JsonConvert.SerializeObject(Data));
                }
            });
        }

        public Task Log(LogMessage arg)
        {
            Console.WriteLine($"[{arg.Severity}] {arg.Message}");
            return Task.CompletedTask;
        }
    }
}