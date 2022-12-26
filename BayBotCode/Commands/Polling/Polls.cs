using BayBot.Core;
using BayBot.Utils;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BayBot.Commands.Polling {
    public static class Polls {
        private const string ActivePollsFile = "polls.active";

        private const string PollCommandName = "poll";
        private const string EndPollCommandName = "endpoll";

        private const string OptionTitleName = "title";
        private const string OptionQuestionName = "question";
        private const string OptionIntervalName = "interval";
        private const string OptionEmojiTypeName = "emojitype";
        private const string OptionAttachmentName = "attachment";

        private const string OptionsMenuName = "pollOptionsMenu";

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
                .AddOption(OptionAttachmentName, ApplicationCommandOptionType.Attachment, "An attachment, under 8MB, to go along with the poll. (eg. .png, .txt, .dll, etc)", isRequired: false)
                .AddOptions(Enumerable.Range(1, 20).Select(num => new SlashCommandOptionBuilder().WithName($"option{num}")
                    .WithDescription($"Option {num}")
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

        public static async Task HandleCommands(SocketSlashCommand command) {
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

                IAttachment attachment = null;

                foreach (SocketSlashCommandDataOption option in command.Data.Options) {
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
                        case OptionAttachmentName:
                            attachment = (IAttachment)option.Value;
                            poll.AttachmentUrl = attachment.Url;
                            poll.AttachmentType = attachment.ContentType;
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

                SelectMenuBuilder optionsMenu = new SelectMenuBuilder()
                    .WithCustomId($"{OptionsMenuName}{command.GuildId.Value},{poll.Id}")
                    .WithPlaceholder("Select an option.")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .AddOption("No Vote", "-1");

                int optionCount = poll.OptionCount;
                string[] emojis = poll.Emojis;
                for (int i = 0; i < optionCount; i++)
                    optionsMenu.AddOption(options[i], $"{i}", emote: new Emoji(emojis[i]));

                ComponentBuilder components = new ComponentBuilder().WithSelectMenu(optionsMenu);

                if (poll.HasAttachment && attachment.Size > 8 * 1024 * 1024) {
                    await command.RespondAsync(text: "Please no files over 8MB, thanks 😊", ephemeral: true);
                } else if (poll.HasAttachment && !poll.HasImageAttachment) {
                    await command.DeferAsync();

                    HttpClient httpClient = new();
                    Stream fileStream = await httpClient.GetStreamAsync(attachment.Url);
                    FileAttachment file = new(fileStream, attachment.Filename, attachment.Description, attachment.IsSpoiler());

                    await command.ModifyOriginalResponseAsync(message => {
                        message.Embed = poll.BuildEmbed(true, false);
                        message.Components = components.Build();
                        message.Attachments = new FileAttachment[] { file };
                    });
                } else
                    await command.RespondAsync(embed: poll.BuildEmbed(true, false), components: components.Build());

                poll.OriginalMessage = (await command.GetOriginalResponseAsync()).Id;

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

        // Do not respond unless success because it will lock in their decision if you do
        public static async Task HandlePollOptions(SocketMessageComponent component) {
            if (component.Data.CustomId.StartsWith(OptionsMenuName)) {
                string stringChoice = component.Data.Values.First();
                string[] stringIds = component.Data.CustomId[OptionsMenuName.Length..].Split(',');

                if (int.TryParse(stringChoice, out int option) && ulong.TryParse(stringIds[0], out ulong guildId) && ulong.TryParse(stringIds[1], out ulong pollId)) {
                    PollGuild guild = ActivePolls.GetGuildByIdOrDefault(guildId);

                    if (guild is not null) {
                        Poll poll = guild.GetByIdOrDefault(pollId);

                        if (poll is not null) {
                            Choice choice = poll.GetChoiceByUserOrDefault(component.User.Id);
                            if (choice is not null) {
                                if (option == -1)
                                    poll.Choices.Remove(choice);
                                else
                                    choice.Option = option;
                            } else if (option != -1)
                                poll.Choices.Add(new Choice() { Option = option, User = component.User.Id });

                            SavePolls();

                            await component.UpdateAsync(message => message.Embed = poll.BuildEmbed(true, false));
                        }
                    }
                }
            }
        }

        public static async Task EndPoll(SocketSlashCommand command) {
            if (command.GuildId is not null) {
                PollGuild guild = ActivePolls.GetGuildById(command.GuildId.Value);
                Poll poll = guild.GetByIdOrDefault((ulong)(long)command.Data.Options.First().Value);
                if (poll is null)
                    await command.SendError("A poll with that id does not exist for this server.");
                else {
                    await command.DeferAsync(ephemeral: true);
                    await EndPoll(guild, poll);
                    await command.FollowupSuccess("Ended poll successfully.");
                }
            }
        }

        private static async Task EndPoll(PollGuild guild, Poll poll) {
            guild.Polls.Remove(poll);

            try {
                if (poll.OriginalMessage != 0 && poll.Channel != 0 && await BayBotCode.Bot.GetChannelAsync(poll.Channel) is IMessageChannel channel) {
                    if (await channel.GetMessageAsync(poll.OriginalMessage) as IUserMessage is not null) {
                        await (await channel.GetMessageAsync(poll.OriginalMessage) as IUserMessage).ModifyAsync(message => message.Embed = poll.BuildEmbed(false, false));
                        await channel.SendMessageAsync(embed: poll.BuildEmbed(false, true), allowedMentions: AllowedMentions.None, messageReference: new MessageReference(poll.OriginalMessage));
                    }
                }
            } catch (Exception e) {
                Logger.WriteLine(e.ToString());
                if (e.InnerException is not null)
                    Logger.WriteLine(e.InnerException.ToString());
            }

            SavePolls();
        }

        internal static Poll FirstOrDefault(Func<object, bool> value) {
            throw new NotImplementedException();
        }
    }
}
