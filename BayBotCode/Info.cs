using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BayBot {
    public static class Info {
        private const string PingCommandName = "ping";
        private const string LatencyCommandName = "latency";

        public static void AddSlashCommands(List<ApplicationCommandProperties> commands) {
            SlashCommandBuilder ping = new SlashCommandBuilder().WithName(PingCommandName)
                .WithDescription("See if the bot is online.");
            commands.Add(ping.Build());

            SlashCommandBuilder latency = new SlashCommandBuilder().WithName(LatencyCommandName)
                .WithDescription("Sends an estimated round-trip latency, in milliseconds.");
            commands.Add(latency.Build());
        }

        public static async Task HandleInfoCommands(SocketSlashCommand command) {
            switch (command.CommandName) {
                case PingCommandName:
                    await SendPing(command);
                    break;
                case LatencyCommandName:
                    await SendLatency(command);
                    break;
            }
        }

        public static async Task SendPing(SocketSlashCommand command) => await command.SendSuccess("Pong");

        public static async Task SendLatency(SocketSlashCommand command) => await command.SendSuccess($"Latency: {BayBotCode.Bot.Latency} ms");
    }
}
