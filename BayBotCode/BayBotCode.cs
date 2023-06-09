﻿using BayBot.Commands.Counting;
using BayBot.Commands.Echo;
using BayBot.Commands.ForumUpdates;
using BayBot.Commands.Info;
using BayBot.Commands.Logging;
using BayBot.Commands.Polling;
using BayBot.Commands.ReactionRoles;
using BayBot.Core;
using BayBot.Utils;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BayBot {
    /// <summary>
    /// The class to handle all the bot functions
    /// </summary>
    public static class BayBotCode {
        private const string Prefix = "hi!";

        /// <summary>
        /// BayBot
        /// </summary>
        public static DiscordSocketClient Bot { get; set; }

        /// <summary>
        /// A global random instance
        /// </summary>
        public static Random Random { get; } = new();

        /// <summary>
        /// Initializes the bot to be able to handle interactions
        /// </summary>
        /// <param name="bot">BayBot</param>
        /// <param name="dataFolder">The data folder</param>
        public static void Init(DiscordSocketClient bot, string dataFolder) {
            // Set BayBot and the data folder
            Data.Folder = dataFolder;
            Bot = bot;

            // Receive events
            Bot.MessageReceived += HandleMessage;
            Bot.SlashCommandExecuted += HandleSlashCommand;
            Bot.SelectMenuExecuted += HandleSelectMenus;
            Bot.ThreadCreated += HandleThreadCreated;

            // Load all the saved data
            GuildLogs.LoadLogChannels();
            Counts.LoadCounts();
            Polls.LoadPolls();
            Echo.LoadMessages();
            ForumUpdates.LoadForums();

            // Register all the slash commands
            try {
                List<ApplicationCommandProperties> commands = new();

                Polls.AddSlashCommands(commands);
                Info.AddSlashCommands(commands);
                GuildLogs.AddSlashCommands(commands);
                Counts.AddSlashCommands(commands);
                ReactionRoles.AddSlashCommands(commands);
                Echo.AddSlashCommands(commands);
                ForumUpdates.AddSlashCommands(commands);

                Bot.BulkOverwriteGlobalApplicationCommandsAsync(commands.ToArray());
            } catch (Exception e) {
                Logger.WriteLine(e.ToString());
                if (e.InnerException is not null)
                    Logger.WriteLine(e.InnerException.ToString());
            }
        }

        /// <summary>
        /// Unsubscribe from every event
        /// </summary>
        public static void Exit() {
            Bot.MessageReceived -= HandleMessage;
            Bot.SlashCommandExecuted -= HandleSlashCommand;
            Bot.SelectMenuExecuted -= HandleSelectMenus;
            Bot.ThreadCreated -= HandleThreadCreated;
        }

        /// <summary>
        /// Handles the slash commands
        /// </summary>
        /// <param name="command">The slash command to handle</param>
        private static async Task HandleSlashCommand(SocketSlashCommand command) {
            // Pass the command through each type
            await Polls.HandleCommands(command);
            await Info.HandleCommands(command);
            await GuildLogs.HandleCommands(command);
            await Counts.HandleCommands(command);
            await ReactionRoles.HandleCommands(command);
            await Echo.HandleCommands(command);
            await ForumUpdates.HandleCommands(command);

            // Test if the command was handled (and not in dms) to send a log
            if (command.HasResponded && command.GuildId is not null) {
                IGuild guild = Bot.GetGuild(command.GuildId.Value);
                // Test if this guid has a log channel set
                if (GuildLogs.LogChannels.TryGetValue(guild.Id, out ulong logChannelId)) {
                    // Get the command options and truncate it to a max value
                    string commandOptions = UnravelCommandOptions(command.Data.Options);
                    commandOptions = commandOptions.Length > 1024 ? commandOptions[..1023] : commandOptions;

                    // Make log embed
                    EmbedBuilder commandLog = new EmbedBuilder().WithTitle("Slash Command Used")
                        .WithDescription($"By <@{command.User.Id}>\nIn <#{command.Channel.Id}>")
                        .AddField("Command:", command.CommandName)
                        .AddField("Params:", commandOptions)
                        .WithColor(Color.DarkBlue)
                        .WithCurrentTimestamp();

                    // Get the log channel and send the log
                    IMessageChannel logChannel = await guild.GetChannelAsync(logChannelId) as IMessageChannel;
                    if (logChannel is not null)
                        await logChannel.SendMessageAsync(embed: commandLog.Build());
                }
            } else
                await command.SendError("Not a valid command, or in a DM.");
        }

        /// <summary>
        /// Handles select menus
        /// </summary>
        /// <param name="component">The select menu to handle</param>
        public static async Task HandleSelectMenus(SocketMessageComponent component) {
            // Pass the component through each type
            await Polls.HandlePollOptions(component);
        }

        /// <summary>
        /// Unravels the slash command options into a string
        /// </summary>
        /// <param name="options">The slash command options</param>
        /// <returns>The unraveled string</returns>
        private static string UnravelCommandOptions(IReadOnlyCollection<SocketSlashCommandDataOption> options) {
            string unraveled = "";
            foreach (SocketSlashCommandDataOption option in options) {
                // If the option does not have any sub-options
                if (option.Options is null || option.Options.Count == 0) {
                    // Format option name: option data
                    unraveled += $"{option.Name}: ";
                    switch (option.Type) {
                        case ApplicationCommandOptionType.Attachment:
                            unraveled += $"attachment {option.Value}";
                            break;
                        case ApplicationCommandOptionType.Channel:
                            unraveled += $"<#{(option.Value as IGuildChannel).Id}>";
                            break;
                        case ApplicationCommandOptionType.Mentionable:
                            if (option.Value is IRole role)
                                unraveled += $"<@&{role.Id}>";
                            if (option.Value is IUser user)
                                unraveled += $"<@{user.Id}>";
                            break;
                        case ApplicationCommandOptionType.Role:
                            unraveled += $"<@&{(option.Value as IRole).Id}>";
                            break;
                        case ApplicationCommandOptionType.User:
                            unraveled += $"<@{(option.Value as IUser).Id}>";
                            break;
                        case ApplicationCommandOptionType.SubCommand:
                            unraveled += "subcommand";
                            break;
                        default:
                            unraveled += option.Value;
                            break;
                    }
                    unraveled += '\n';
                } else { // If the option does have sub-options
                    // Format option name: command type
                    // └ sub-options
                    unraveled += $"{option.Name}: ";
                    switch (option.Type) {
                        case ApplicationCommandOptionType.SubCommand:
                            unraveled += "subcommand";
                            break;
                        case ApplicationCommandOptionType.SubCommandGroup:
                            unraveled += "subcommand group";
                            break;
                    }
                    unraveled += "\n└ ";
                    unraveled += $"{UnravelCommandOptions(option.Options).Replace("\n", "└ ")}\n";
                }
            }
            // Return the unraveled string
            return unraveled.Length > 0 ? unraveled[..^1] : "[None]";
        }

        /// <summary>
        /// Handles a message sent in a channel BayBot can see
        /// </summary>
        /// <param name="message">The message</param>
        private static async Task HandleMessage(IMessage message) {
            if (message.Content.ToLower().StartsWith(Prefix))
                await HandleCommand(message);

            Counts.HandleCount(message);
        }

        /// <summary>
        /// Handle message based commands
        /// </summary>
        /// <param name="message">The message that contains the command</param>
        /// <param name="overrideCommand">Another command to take precedence over the one in the message. Used for in code command calling.</param>
        public static async Task HandleCommand(IMessage message, string overrideCommand = null) {
            // The message content
            string content = overrideCommand ?? message.Content;

            // The parts of the command, the command name and params
            string[] parts = content[Prefix.Length..].Split(' ');

            // Return if the command is empty
            if (parts.Length < 1)
                return;

            // Splits the parts into command name and params
            string command = parts[0].ToLower();
            string[] words = parts[1..];

            // Pass the command through each one
            bool commandSuccess = command switch {
                "recoverchannel" =>
                    await Counts.RecoverChannel(message, words[0]),
                _ => false
            };

            // If succeeded send log in log channel
            if (commandSuccess && message.Channel is IGuildChannel channel && GuildLogs.LogChannels.TryGetValue(channel.GuildId, out ulong value)) {
                EmbedBuilder commandLog = new();
                commandLog.WithTitle("Command Used");
                commandLog.WithDescription($"By <@{message.Author.Id}>\nIn <#{channel.Id}>");
                commandLog.AddField("Command:", command);
                if (words.Length > 0)
                    commandLog.AddField("Params:", string.Join(' ', words));
                commandLog.WithColor(Color.Blue);
                commandLog.WithCurrentTimestamp();
                await ((await channel.Guild.GetChannelAsync(value)) as IMessageChannel).SendMessageAsync(embed: commandLog.Build());
            }
        }

        /// <summary>
        /// Receive when a thread is created on a guild channel or forum
        /// </summary>
        /// <param name="thread">The thread created</param>
        public static async Task HandleThreadCreated(SocketThreadChannel thread) {
            await ForumUpdates.TrySendUpdate(thread);
        }

        /// <summary>
        /// Updates only polls currently
        /// </summary>
        public static async Task Update() {
            try {
                await Polls.HandlePolls();
            } catch (Exception e) {
                Logger.WriteLine(e.ToString());
                if (e.InnerException is not null)
                    Logger.WriteLine(e.InnerException.ToString());
            }
        }
    }
}
