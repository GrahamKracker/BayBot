using BayBot.Utils;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BayBot.Commands.Echo {
    public static class Echo {
        private const string EchoCommandName = "echo";

        private const string SendSubCommandName = "send";
        private const string SaveSubCommandName = "save";
        private const string DeleteSubCommandName = "delete";
        private const string ListSubCommandName = "list";
        private const string ThisSubCommandName = "this";

        private const string OptionNameName = "name";
        private const string OptionTextName = "text";
        private const string OptionOverwriteName = "overwrite";
        private const string OptionEphemeralName = "ephemeral";

        private const string MessagesFile = "messages";

        private static MessageList Messages { get; set; }

        public static void LoadMessages() {
            using StreamReader active = new(Data.Open(MessagesFile, FileMode.OpenOrCreate, FileAccess.Read));
            XmlSerializer serializer = new(typeof(MessageList));
            try {
                Messages = serializer.Deserialize(active) as MessageList;
            } catch {
                Messages = new();
            }
        }

        private static void SaveMessages() {
            using StreamWriter active = new(Data.Open(MessagesFile, FileMode.Create, FileAccess.Write));
            XmlSerializer serializer = new(typeof(MessageList));
            serializer.Serialize(active, Messages);
        }

        public static void AddSlashCommands(List<ApplicationCommandProperties> commands) {
            SlashCommandBuilder echo = new SlashCommandBuilder().WithName(EchoCommandName)
                .WithDescription("Echoes a saved message.")
                .WithDefaultPermission(false)
                .WithDMPermission(false)
                .AddOption(new SlashCommandOptionBuilder().WithName(SendSubCommandName)
                    .WithDescription("Send an echoed message.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(OptionNameName, ApplicationCommandOptionType.String, "The name of the saved message.", true))
                .AddOption(new SlashCommandOptionBuilder().WithName(SaveSubCommandName)
                    .WithDescription("Save a message to be echoed later.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(OptionNameName, ApplicationCommandOptionType.String, "The name of the message to be saved.", true)
                    .AddOption(OptionTextName, ApplicationCommandOptionType.String, "The message to be saved. Use \\n in place of newlines because discord doesn't allow them.", true)
                    .AddOption(OptionOverwriteName, ApplicationCommandOptionType.Boolean, "Whether or not to overwrite an existing message. (default false)", false)
                    .AddOption(OptionEphemeralName, ApplicationCommandOptionType.Boolean, "Whether or not this response is only visible to you. (default true)", false))
                .AddOption(new SlashCommandOptionBuilder().WithName(DeleteSubCommandName)
                    .WithDescription("Delete a saved message.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(OptionNameName, ApplicationCommandOptionType.String, "The name of the saved message to delete.", true)
                    .AddOption(OptionEphemeralName, ApplicationCommandOptionType.Boolean, "Whether or not this response is only visible to you. (default true)", false))
                .AddOption(new SlashCommandOptionBuilder().WithName(ListSubCommandName)
                    .WithDescription("List all names of saved messages.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(OptionEphemeralName, ApplicationCommandOptionType.Boolean, "Whether or not this response is only visible to you. (default true)", false))
                .AddOption(new SlashCommandOptionBuilder().WithName(ThisSubCommandName)
                    .WithDescription("Echo given message.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(OptionTextName, ApplicationCommandOptionType.String, "The message to be saved. Use \\n in place of newlines because discord doesn't allow them.", true));
            commands.Add(echo.Build());
        }

        public static async Task HandleCommands(SocketSlashCommand command) {
            if (command.GuildId is not null && command.CommandName.Equals(EchoCommandName)) {
                SocketSlashCommandDataOption subCommand = command.Data.Options.First();
                switch (subCommand.Name) {
                    case SendSubCommandName:
                        await Send(command, subCommand);
                        break;
                    case SaveSubCommandName:
                        await Save(command, subCommand);
                        break;
                    case DeleteSubCommandName:
                        await Delete(command, subCommand);
                        break;
                    case ListSubCommandName:
                        await List(command, subCommand);
                        break;
                    case ThisSubCommandName:
                        await This(command, subCommand);
                        break;
                }
            }
        }

        private static async Task Send(SocketSlashCommand command, SocketSlashCommandDataOption option) {
            MessageGuild guild = Messages.GetGuildByIdOrDefault(command.GuildId.Value);
            if (guild is null || guild.Messages.Count == 0) {
                await QuickResponse.SendError(command, "There are no saved messages in this guild.");
                return;
            }

            string name = option.Options.FirstOrDefault(o => o.Name.Equals(OptionNameName))?.Value as string;
            if (name is null) {
                await QuickResponse.SendError(command, "How did you get away with not sending a name?");
                return;
            }

            Message message = guild.GetByNameOrDefault(name);
            if (message is null) {
                await QuickResponse.SendError(command, "There is no message by this name. Try using the /echo save command.");
                return;
            }

            EmbedBuilder embed = new EmbedBuilder().WithDescription(message.Content);

            await command.RespondAsync(embed: embed.Build());
        }

        private static async Task Save(SocketSlashCommand command, SocketSlashCommandDataOption option) {
            MessageGuild guild = Messages.GetGuildById(command.GuildId.Value);

            string name = option.Options.FirstOrDefault(o => o.Name.Equals(OptionNameName))?.Value as string;
            if (name is null) {
                await QuickResponse.SendError(command, "How did you get away with not sending a name?");
                return;
            }

            string message = option.Options.FirstOrDefault(o => o.Name.Equals(OptionTextName))?.Value as string;
            if (message is null) {
                await QuickResponse.SendError(command, "How did you get away with not sending a message?");
                return;
            }
            message = message.Replace("\\n", "\n");

            bool overwrite = (option.Options.FirstOrDefault(o => o.Name.Equals(OptionOverwriteName))?.Value as bool?) ?? false;
            bool ephemeral = (option.Options.FirstOrDefault(o => o.Name.Equals(OptionEphemeralName))?.Value as bool?) ?? true;

            Message oldMessage = guild.GetByNameOrDefault(name);
            if (oldMessage is not null) {
                if (overwrite) {
                    oldMessage.Content = message;
                    await QuickResponse.SendSuccess(command, $"Successfully overwrote message {name} with content:\n{message}", ephemeral);
                } else {
                    await QuickResponse.SendError(command, "Message with same name already exists.");
                    return;
                }
            } else {
                guild.Messages.Add(new() { Name = name, Content = message });
                await QuickResponse.SendSuccess(command, $"Successfully added message {name} with content:\n{message}", ephemeral);
            }

            SaveMessages();
        }

        private static async Task Delete(SocketSlashCommand command, SocketSlashCommandDataOption option) {
            MessageGuild guild = Messages.GetGuildByIdOrDefault(command.GuildId.Value);
            if (guild is null || guild.Messages.Count == 0) {
                await QuickResponse.SendError(command, "There are no saved messages in this guild.");
                return;
            }

            string name = option.Options.FirstOrDefault(o => o.Name.Equals(OptionNameName))?.Value as string;
            if (name is null) {
                await QuickResponse.SendError(command, "How did you get away with not sending a name?");
                return;
            }

            bool ephemeral = (option.Options.FirstOrDefault(o => o.Name.Equals(OptionEphemeralName))?.Value as bool?) ?? true;

            Message message = guild.GetByNameOrDefault(name);
            if (message is null) {
                await QuickResponse.SendError(command, "There is no message by this name. Try using the /echo save command.");
                return;
            }

            guild.Messages.Remove(message);
            SaveMessages();

            await QuickResponse.SendSuccess(command, $"Successfully deleted message {name}.", ephemeral);
        }

        private static async Task List(SocketSlashCommand command, SocketSlashCommandDataOption option) {
            MessageGuild guild = Messages.GetGuildByIdOrDefault(command.GuildId.Value);
            if (guild is null || guild.Messages.Count == 0) {
                await QuickResponse.SendError(command, "There are no saved messages in this guild.");
                return;
            }

            bool ephemeral = (option.Options.FirstOrDefault(o => o.Name.Equals(OptionEphemeralName))?.Value as bool?) ?? true;

            string messageNames = "";

            foreach (var message in guild.Messages)
                messageNames += message.Name + "\n";

            EmbedBuilder embed = new EmbedBuilder().WithTitle("Echoable Messages")
                .WithDescription(messageNames);

            await command.RespondAsync(embed: embed.Build(), ephemeral: ephemeral);
        }

        private static async Task This(SocketSlashCommand command, SocketSlashCommandDataOption option) {
            string message = option.Options.FirstOrDefault(o => o.Name.Equals(OptionTextName))?.Value as string;
            if (message is null) {
                await QuickResponse.SendError(command, "How did you get away with not sending a message?");
                return;
            }
            message = message.Replace("\\n", "\n");

            EmbedBuilder embed = new EmbedBuilder().WithDescription(message);

            await command.RespondAsync(embed: embed.Build());
        }
    }
}
