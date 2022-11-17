using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BayBot.Polling {
    public static class Polls {
        private const string ActivePollsFile = "polls.active";

        private const string PollCommandName = "poll";
        private const string EndPollCommandName = "endpoll";

        private const string OptionTitleName = "title";
        private const string OptionQuestionName = "question";
        private const string OptionIntervalName = "interval";
        private const string OptionEmojiTypeName = "emojitype";

        private static readonly Dictionary<PollEmojiTypes, string[]> EmojiTypes = new() {
            [PollEmojiTypes.Letters] = new string[] { "🇦", "🇧", "🇨", "🇩", "🇪", "🇫", "🇬", "🇭", "🇮", "🇯", "🇰", "🇱", "🇲", "🇳", "🇴", "🇵", "🇶", "🇷", "🇸", "🇹", "🇺", "🇻", "🇼", "🇽", "🇾", "🇿" },
            [PollEmojiTypes.Numbers] = new string[] { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟" },
            [PollEmojiTypes.Circles] = new string[] { "🔴", "🟠", "🟡", "🟢", "🔵", "🟣", "🟤", "⚫", "⚪" },
            [PollEmojiTypes.Squares] = new string[] { "🟥", "🟧", "🟨", "🟩", "🟦", "🟪", "🟫", "⬛", "⬜" },
            [PollEmojiTypes.Thumbs] = new string[] { "👍", "👎" }
        };

        private static PollList ActivePolls { get; set; }

        public static void LoadPolls() {
            using StreamReader active = new(Data.Open(ActivePollsFile, FileMode.OpenOrCreate, FileAccess.Read));
            XmlSerializer serializer = new(typeof(PollList));
            try {
                ActivePolls = serializer.Deserialize(active) as PollList;
            } catch {
                ActivePolls = new();
            }
        }

        private static void SavePolls() {
            using StreamWriter active = new(Data.Open(ActivePollsFile, FileMode.Create, FileAccess.Write));
            XmlSerializer serializer = new(typeof(PollList));
            serializer.Serialize(active, ActivePolls);
        }

        public static void AddSlashCommands(List<ApplicationCommandProperties> commands) {
            SlashCommandBuilder poll = new SlashCommandBuilder().WithName(PollCommandName)
                .WithDescription("Creates a poll.")
                .WithDefaultPermission(false)
                .WithDMPermission(false)
                .AddOption(OptionTitleName, ApplicationCommandOptionType.String, "Title of the poll, will appear at the top.", isRequired: false)
                .AddOption(OptionQuestionName, ApplicationCommandOptionType.String, "The question of the poll, will appear below the title.", isRequired: false)
                .AddOption(OptionIntervalName, ApplicationCommandOptionType.String, "The time taken before ending the poll automatically in days:hours:minutes format.", isRequired: false)
                .AddOption(new SlashCommandOptionBuilder().WithName(OptionEmojiTypeName)
                    .WithDescription("The type of emojis used for the poll. Options past the amount of emojis in the type will be ignored.")
                    .WithType(ApplicationCommandOptionType.Integer)
                    .AddChoice("thumbs", (int)PollEmojiTypes.Thumbs)
                    .AddChoice("letters", (int)PollEmojiTypes.Letters)
                    .AddChoice("numbers", (int)PollEmojiTypes.Numbers)
                    .AddChoice("circles", (int)PollEmojiTypes.Circles)
                    .AddChoice("squares", (int)PollEmojiTypes.Squares))
                .AddOptions(Enumerable.Range(1, 20).Select(number => new SlashCommandOptionBuilder().WithName($"option{number}")
                    .WithDescription($"Option {number}")
                    .WithRequired(false)
                    .WithType(ApplicationCommandOptionType.String)).ToArray());
            commands.Add(poll.Build());

            SlashCommandBuilder endPoll = new SlashCommandBuilder().WithName(EndPollCommandName)
                .WithDescription("Ends a poll immediately.")
                .WithDefaultPermission(false)
                .WithDMPermission(false)
                .AddOption("id", ApplicationCommandOptionType.Integer, "The id of the poll to be ended.", isRequired: true, minValue: 0);
            commands.Add(endPoll.Build());
        }

        public static async Task HandlePollCommands(SocketSlashCommand command) {
            switch (command.CommandName) {
                case PollCommandName:
                    await SendPoll(command);
                    break;
                case EndPollCommandName:
                    await EndPoll(command);
                    break;
            }
        }

        private static async Task SendPoll(SocketSlashCommand command) {
            if (command.ChannelId is not null && command.GuildId is not null) {
                Poll poll = new() {
                    Author = command.User.Id,
                    Channel = command.ChannelId.Value
                };

                List<string> options = new();

                TimeSpan interval = new(0);

                foreach (var option in command.Data.Options) {
                    switch (option.Name) {
                        case OptionTitleName:
                            poll.Title = option.Value as string;
                            break;
                        case OptionQuestionName:
                            poll.Question = option.Value as string;
                            break;
                        case OptionIntervalName:
                            string[] time = (option.Value as string).Split(':');
                            int hours = 0, days = 0;
                            if (!int.TryParse(time[^1], out int minutes)) {
                                await command.SendError("Interval must me in days:hours:minutes format.");
                                return;
                            } else {
                                if (time.Length > 1 && int.TryParse(time[^2], out hours))
                                    if (time.Length > 2)
                                        _ = int.TryParse(time[^3], out days);

                                interval = new(days, hours, minutes, 0);
                                poll.EndTime = DateTime.Now + interval;
                            }
                            break;
                        case OptionEmojiTypeName:
                            poll.EmojiType = (PollEmojiTypes)(long)option.Value;
                            break;
                        case string s when s.StartsWith("option"):
                            options.Add(option.Value as string);
                            break;
                    }
                }

                if (options.Count == 0)
                    options.Add("yes");
                if (options.Count == 1)
                    options.Add("no");

                poll.Options = options.ToArray();

                if (interval.Ticks == 0)
                    poll.EndTime = DateTime.MaxValue;

                ActivePolls.GetGuildById(command.GuildId.Value).RegisterNew(poll);

                EmbedBuilder pollEmbed = new();
                pollEmbed.WithColor(Color.Purple);
                pollEmbed.WithFooter($"Poll ID: {poll.Id}");

                if (!string.IsNullOrEmpty(poll.Title))
                    pollEmbed.WithTitle(poll.Title);

                if (!string.IsNullOrEmpty(poll.Question))
                    pollEmbed.AddField("Question:", poll.Question);

                string[] emojis = EmojiTypes[poll.EmojiType];
                int minLength = Math.Min(poll.Options.Length, emojis.Length);
                for (int i = 0; i < minLength; i++)
                    pollEmbed.AddField(poll.Options[i], emojis[i], true);

                if (interval.Ticks > 0) {
                    List<string> time = new();
                    if (interval.Days > 0)
                        time.Add($"{interval.Days} {Formatting.MatchPlurality("Day", interval.Days)}");
                    if (interval.Hours > 0)
                        time.Add($"{interval.Hours} {Formatting.MatchPlurality("Hour", interval.Hours)}");
                    if (interval.Minutes > 0)
                        time.Add($"{interval.Minutes} {Formatting.MatchPlurality("Minute", interval.Minutes)}");
                    pollEmbed.AddField("Interval:", Formatting.ListItems(time));
                }

                await command.RespondAsync(embed: pollEmbed.Build());
                IUserMessage pollMessage = await command.GetOriginalResponseAsync();
                poll.OriginalMessage = pollMessage.Id;

                // Don't wait for it as that's unnecessary
                _ = pollMessage.AddReactionsAsync(emojis.Take(minLength).Select(emoji => new Emoji(emoji)));

                SavePolls();
            }
        }

        public static async Task HandlePolls() {
            bool changedPolls = false;

            DateTime now = DateTime.Now; // Define here so that the polls are ended in the order they were made
            foreach (PollGuild guild in ActivePolls.Guilds) {
                for (int i = 0; i < guild.Polls.Count; i++) {
                    if (now > guild.Polls[i].EndTime) {
                        await EndPoll(guild, guild.Polls[i]);
                        i--; // Poll was removed
                        changedPolls = true;
                    }
                }
            }

            if (changedPolls)
                SavePolls();
        }

        public static async Task EndPoll(SocketSlashCommand command) {
            if (command.GuildId is not null) {
                PollGuild guild = ActivePolls.GetGuildById(command.GuildId.Value);
                Poll poll = guild.GetById((ulong)(long)command.Data.Options.First().Value);
                if (poll is null)
                    await command.SendError("A poll with that id does not exist for this server.");
                else {
                    await EndPoll(guild, poll);
                    await command.SendSuccess("Ended poll successfully.");
                }
            }
        }

        private static async Task EndPoll(PollGuild guild, Poll poll) {
            guild.Polls.Remove(poll);

            if (poll.OriginalMessage != 0 && poll.Channel != 0 && await BayBotCode.Bot.GetChannelAsync(poll.Channel) is ITextChannel channel) {
                IMessage pollMessage = await channel.GetMessageAsync(poll.OriginalMessage);

                EmbedBuilder pollEmbed = new();
                pollEmbed.WithColor(Color.Purple);
                pollEmbed.WithFooter($"Poll ID: {poll.Id}");

                if (!string.IsNullOrEmpty(poll.Title))
                    pollEmbed.WithTitle(poll.Title);

                if (!string.IsNullOrEmpty(poll.Question))
                    pollEmbed.AddField("Question:", poll.Question);

                string[] emojis = EmojiTypes[poll.EmojiType];
                Dictionary<string, int> pollReactCounts = pollMessage.Reactions.ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value.ReactionCount);
                int minLength = Math.Min(poll.Options.Length, emojis.Length);

                int maxCount = -1;
                List<string> winning = new();

                for (int i = 0; i < minLength; i++) {
                    if (pollReactCounts.ContainsKey(emojis[i])) {
                        int count = pollReactCounts[emojis[i]];
                        if (count > maxCount) {
                            winning.Clear();
                            maxCount = count;
                        }
                        if (count == maxCount)
                            winning.Add(emojis[i]);
                    }
                }

                if (winning.Count > 1) {
                    string winner = winning[BayBotCode.Random.Next(winning.Count)];
                    winning.Clear();
                    winning.Add(winner);
                    pollEmbed.Description += "\n*RNG broke this tie.*";
                }

                for (int i = 0; i < minLength; i++) {
                    string count = pollReactCounts.ContainsKey(emojis[i]) ? $"{pollReactCounts[emojis[i]] - 1}" : "?";
                    pollEmbed.AddField($"{poll.Options[i]}: {count} votes {(winning.Contains(emojis[i]) ? "✅" : "")}", emojis[i], true);
                }

                await channel.SendMessageAsync(embed: pollEmbed.Build(), allowedMentions: AllowedMentions.None, messageReference: new MessageReference(poll.OriginalMessage));
            }
        }
    }
}
