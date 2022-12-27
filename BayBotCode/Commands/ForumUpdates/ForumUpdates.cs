using BayBot.Utils;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BayBot.Commands.ForumUpdates {
    public static class ForumUpdates {
        private const string ForumUpdatesCommandName = "forumupdates";

        private const string SetSubCommandName = "set";
        private const string RemoveSubCommandName = "remove";

        private const string UpdateChannelOptionName = "updatechannel";
        private const string MessageOptionName = "message";
        private const string ForumChannelOptionName = "forumchannel";

        private const string ForumsFile = "forumsupdates";

        private static ForumsList Forums { get; set; }

        public static void LoadForums() {
            using StreamReader active = new(Data.Open(ForumsFile, FileMode.OpenOrCreate, FileAccess.Read));
            XmlSerializer serializer = new(typeof(ForumsList));
            try {
                Forums = serializer.Deserialize(active) as ForumsList;
            } catch {
                Forums = new();
            }
        }

        private static void SaveForums() {
            using StreamWriter active = new(Data.Open(ForumsFile, FileMode.Create, FileAccess.Write));
            XmlSerializer serializer = new(typeof(ForumsList));
            serializer.Serialize(active, Forums);
        }

        public static void AddSlashCommands(List<ApplicationCommandProperties> commands) {
            SlashCommandBuilder forumupdates = new SlashCommandBuilder().WithName(ForumUpdatesCommandName)
                .WithDescription("Sends a message in a channel when a new forum post is created in a given forum channel.")
                .WithDefaultPermission(false)
                .WithDMPermission(false)
                .AddOption(new SlashCommandOptionBuilder().WithName(SetSubCommandName)
                    .WithDescription("Sets an update channel.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(UpdateChannelOptionName, ApplicationCommandOptionType.Channel, "The channel to send updates to.", true, channelTypes: new() { ChannelType.News, ChannelType.Text })
                    .AddOption(MessageOptionName, ApplicationCommandOptionType.String, "The message to send with the update (make sure to ping update pings).", true)
                    .AddOptions(Enumerable.Range(1, 5).Select(i => new SlashCommandOptionBuilder().WithName($"{ForumChannelOptionName}{i}")
                        .WithDescription("A forum channel")
                        .WithType(ApplicationCommandOptionType.Channel)
                        .WithRequired(i == 1)
                        .AddChannelType(ChannelType.Forum)).ToArray()))
                .AddOption(new SlashCommandOptionBuilder().WithName(RemoveSubCommandName)
                    .WithDescription("Removes an update channel for forums.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(UpdateChannelOptionName, ApplicationCommandOptionType.Channel, "The channel to unset.", true, channelTypes: new() { ChannelType.News, ChannelType.Text }));
            commands.Add(forumupdates.Build());
        }

        public static async Task HandleCommands(SocketSlashCommand command) {
            if (command.GuildId is not null) {
                SocketSlashCommandDataOption subCommand = command.Data.Options.First();
                switch (subCommand.Name) {
                    case SetSubCommandName:
                        await Set(command, subCommand);
                        break;
                    case RemoveSubCommandName:
                        await Remove(command, subCommand);
                        break;
                }
            }
        }

        public static async Task Set(SocketSlashCommand command, SocketSlashCommandDataOption option) {
            IGuildChannel updateChannel = option.Options.FirstOrDefault(o => o.Name.Equals(UpdateChannelOptionName)).Value as IGuildChannel;
            if (updateChannel is null) {
                await QuickResponse.SendError(command, "How did you get away with not sending an update channel?");
                return;
            }

            string message = option.Options.FirstOrDefault(o => o.Name.Equals(MessageOptionName)).Value as string;
            if (message is null) {
                await QuickResponse.SendError(command, "How did you get away with not sending an additional message?");
                return;
            }

            IGuildChannel[] forumChannels = option.Options.Where(o => o.Name.StartsWith(ForumChannelOptionName)).Select(o => o.Value as IGuildChannel).ToArray();
            if (forumChannels.Length < 1) {
                await QuickResponse.SendError(command, "How did you get away with not sending a forum channel?");
                return;
            }

            ForumsGuild guild = Forums.GetGuildById(command.GuildId.Value);

            Forums forums = guild.GetByUpdateChannelOrDefault(updateChannel.Id);
            if (forums is null) {
                forums = new() { Ids = forumChannels.Select(fc => fc.Id).ToArray(), Message = message, UpdateChannel = updateChannel.Id };
                guild.Forums.Add(forums);
            } else {
                forums.Ids = forumChannels.Select(fc => fc.Id).ToArray();
                forums.Message = message;
            }

            await QuickResponse.SendSuccess(command, $"Successfully set forum update channel {updateChannel.Name} for forums: [ {string.Join(", ", forumChannels.Select(fc => fc.Name))} ].", false);

            SaveForums();
        }

        public static async Task Remove(SocketSlashCommand command, SocketSlashCommandDataOption option) {
            ForumsGuild guild = Forums.GetGuildByIdOrDefault(command.GuildId.Value);
            if (guild is null) {
                await QuickResponse.SendError(command, "This guild has no set forum update channels.");
                return;
            }

            IGuildChannel updateChannel = option.Options.FirstOrDefault(o => o.Name.Equals(UpdateChannelOptionName)).Value as IGuildChannel;
            if (updateChannel is null) {
                await QuickResponse.SendError(command, "How did you get away with not sending an update channel?");
                return;
            }

            Forums forums = guild.GetByUpdateChannelOrDefault(updateChannel.Id);
            if (forums is null) {
                await QuickResponse.SendError(command, "This is not an forum update channel.");
                return;
            }
            guild.Forums.Remove(forums);

            await QuickResponse.SendSuccess(command, $"Successfully removed forum update channel {updateChannel.Name}.", false);

            SaveForums();
        }

        public static async Task TrySendUpdate(SocketThreadChannel thread) {
            ForumsGuild guild = Forums.GetGuildByIdOrDefault(thread.Guild.Id);
            if (guild is null)
                return;

            Forums forums = guild.GetByForumChannelOrDefault(thread.ParentChannel.Id);
            if (forums is null)
                return;

            SocketTextChannel updateChannel = thread.Guild.GetTextChannel(forums.UpdateChannel);
            if (updateChannel is null)
                return;

            string text = $"{forums.Message}\n{MentionUtils.MentionChannel(thread.Id)}";

            await updateChannel.SendMessageAsync(text);
        }
    }
}
