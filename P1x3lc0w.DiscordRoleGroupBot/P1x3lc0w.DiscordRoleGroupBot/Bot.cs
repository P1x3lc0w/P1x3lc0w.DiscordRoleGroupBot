using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using P1x3lc0w.DiscordRoleGroupBot.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace P1x3lc0w.DiscordRoleGroupBot
{
    public class Bot
    {
        public DiscordSocketClient SocketClient { get; private set; }
        internal BotEventHandler BotEventHandler { get; private set; }

        internal BotData Data { get; private set; }

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

            SocketClient.UserUpdated += BotEventHandler.OnUserUpdated;

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
            Data = new BotData();
        }
        public Task StartBot(string token) 
            => SocketClient.LoginAsync(TokenType.Bot, token);

        public Task Log(LogMessage arg)
        {
            Console.WriteLine($"[{arg.Severity}] {arg.Message}");
            return Task.CompletedTask;
        }
    }
}
