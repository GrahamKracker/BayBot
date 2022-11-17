using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BayBot {
    public static class GuildLogs {
        private const string LogChannelCommandName = "logchannel";
        private const string SetLogChannelSubCommandName = "set";
        private const string RemoveLogChannelSubCommandName = "remove";

        // Key is guild id, value is channel id
        public static Dictionary<ulong, ulong> LogChannels { get; } = new();

        public static void LoadLogChannels() {
            using BinaryReader active = new(Data.Open("commandLogChannels.active", FileMode.OpenOrCreate, FileAccess.Read));
            LogChannels.Clear();
            bool errored = false;
            while (!errored) {
                try {
                    LogChannels.Add(active.ReadUInt64(), active.ReadUInt64());
                } catch {
                    errored = true;
                }
            }
        }

        private static void SaveLogChannels() {
            using BinaryWriter active = new(Data.Open("commandLogChannels.active", FileMode.Create, FileAccess.Write));
            foreach (ulong guild in LogChannels.Keys) {
                active.Write(guild);
                active.Write(LogChannels[guild]);
            }
        }

        public static void AddSlashCommands(List<ApplicationCommandProperties> commands) {
            SlashCommandBuilder setLogChannel = new SlashCommandBuilder().WithName(LogChannelCommandName)
                .WithDescription("The text channel where logs for commands used will go.")
                .WithDefaultPermission(false)
                .WithDMPermission(false)
                .AddOption(new SlashCommandOptionBuilder().WithName(SetLogChannelSubCommandName)
                    .WithDescription("Sets the channel that logs will be sent to.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel the logs will be in", isRequired: true))
                .AddOption(RemoveLogChannelSubCommandName, ApplicationCommandOptionType.SubCommand, "Stops logs from being sent to the currently set channel.");
            commands.Add(setLogChannel.Build());
        }

        public static async Task HandleLogCommands(SocketSlashCommand command) {
            switch (command.CommandName) {
                case LogChannelCommandName:
                    await SetLogChannel(command);
                    break;
            }
        }

        public static async Task SetLogChannel(SocketSlashCommand command) {
            if (command.GuildId is not null) {
                SocketSlashCommandDataOption subCommand = command.Data.Options.First();
                switch (subCommand.Name) {
                    case SetLogChannelSubCommandName:
                        if (subCommand.Options.First().Value is ITextChannel channel) {
                            if (LogChannels.ContainsKey(command.GuildId.Value))
                                LogChannels[command.GuildId.Value] = channel.Id;
                            else
                                LogChannels.Add(command.GuildId.Value, channel.Id);

                            SaveLogChannels();

                            await command.SendSuccess($"Set log channel to <#{channel.Id}>.", false);
                        } else
                            await command.SendError($"The channel must be a standard text channel.");
                        break;

                    case RemoveLogChannelSubCommandName:
                        if (LogChannels.Remove(command.GuildId.Value, out ulong prev)) {
                            SaveLogChannels();

                            await command.SendSuccess($"Removed <#{prev}> as the log channel.", false);
                        } else
                            await command.SendError("This server does not have a log channel set.");
                        break;
                }
            }
        }
    }
}
