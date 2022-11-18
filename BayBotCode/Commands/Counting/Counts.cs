using BayBot.Core;
using BayBot.Utils;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BayBot.Commands.Counting {
    public static class Counts {
        private const string CountChannelCommandName = "countchannel";
        private const string CountChampRoleCommandName = "countchamprole";
        private const string ChangeCountCommandName = "changecount";
        private const string CountCommandName = "count";
        private const string UserCountCommandName = "usercount";
        private const string MyCountCommandName = "mycount";
        private const string LeaderboardCommandName = "leaderboard";
        private const string FullLeaderboardCommandName = "fullleaderboard";
        private const string SetSubCommandName = "set";
        private const string RemoveSubCommandName = "remove";
        private const string ResetSubCommandName = "reset";
        private const string RecoverSubCommandName = "recover";
        private const string UserSubCommandName = "user";

        public static CountInfoList CountingGuilds { get; set; }

        public static void LoadCounts() {
            using StreamReader active = new(Data.Open("counts.active", FileMode.OpenOrCreate, FileAccess.Read));
            XmlSerializer serializer = new(typeof(CountInfoList));
            try {
                CountingGuilds = serializer.Deserialize(active) as CountInfoList;
            } catch {
                CountingGuilds = new();
            }
            foreach (CountInfo countInfo in CountingGuilds)
                countInfo.Sort();
        }

        public static void SaveCounts() {
            using StreamWriter active = new(Data.Open("counts.active", FileMode.Create, FileAccess.Write));
            XmlSerializer serializer = new(typeof(CountInfoList));
            serializer.Serialize(active, CountingGuilds);
        }

        private static Thread GetCountQueueThread(CountInfo countInfo) {
            string key = $"CountInfoQueueThread/{countInfo.Guild}";
            return InterSave.Get(key) as Thread;
        }

        private static void SetCountQueueThread(CountInfo countInfo, Thread thread) {
            string key = $"CountInfoQueueThread/{countInfo.Guild}";
            InterSave.Set(key, thread);
        }

        private static Queue<IMessage> GetCountQueue(CountInfo countInfo) {
            string key = $"CountInfoQueue/{countInfo.Guild}";
            Queue<IMessage> queue = InterSave.Get(key) as Queue<IMessage>;
            if (queue is null) {
                queue = new Queue<IMessage>();
                SetCountQueue(countInfo, queue);
            }
            return queue;
        }

        private static void SetCountQueue(CountInfo countInfo, Queue<IMessage> queue) {
            string key = $"CountInfoQueue/{countInfo.Guild}";
            InterSave.Set(key, queue);
        }

        public static void HandleCount(IMessage message) {
            if (message.Channel is ITextChannel channel) {
                CountInfo countInfo = CountingGuilds.GetByGuild(channel.GuildId);
                if (channel.Id == countInfo.Channel) {
                    Queue<IMessage> countQueue = GetCountQueue(countInfo);

                    if (!countQueue.Contains(message))
                        countQueue.Enqueue(message);

                    if (GetCountQueueThread(countInfo) is null) {
                        Thread countQueueThread = new(async o => await HandleCountMessages(o as CountInfo)) { IsBackground = true };
                        countQueueThread.Start(countInfo);
                        SetCountQueueThread(countInfo, countQueueThread);
                    }
                }
            }
        }

        private static async Task HandleCountMessages(CountInfo countInfo) {
            try {
                Queue<IMessage> queue = GetCountQueue(countInfo);
                while (queue.TryDequeue(out IMessage message)) {
                    if (message is not null && message.Channel is ITextChannel channel) {
                        bool isValid = false;
                        if (ulong.TryParse(message.Content, out ulong thisCount) && thisCount - countInfo.Count == 1) {
                            IMessage previousMessage = (await channel.GetMessagesAsync(message, Direction.Before, 1).FlattenAsync()).FirstOrDefault();
                            if (previousMessage is null || previousMessage.Author.Id != message.Author.Id) {
                                countInfo.Count++;

                                UserCount userCount = countInfo.GetByUser(message.Author.Id);
                                userCount.Count++;

                                ResolveUserCountRank(countInfo, message.Author.Id, out int rank, out int oldRank);

                                if (rank < oldRank && rank == 0 || countInfo.UsersCount == 1)
                                    await ResolveCountChampRole(countInfo, message.Author as IGuildUser);

                                // Don't wait for it as that's unnecessary
                                if (countInfo.Count % 1000 == 0)
                                    _ = (message as IUserMessage).PinAsync();

                                countInfo.LastMessage = message.Id;

                                isValid = true;
                                SaveCounts();
                            }
                        }
                        if (!isValid)
                            await message.DeleteAsync();
                    }
                }
            } catch (Exception e) {
                Logger.WriteLine(e.Message);
                if (e.InnerException is not null)
                    Logger.WriteLine(e.InnerException.Message);
            }
            SetCountQueueThread(countInfo, null);
        }

        public static void ResolveUserCountRank(CountInfo countInfo, ulong userId, out int newRank, out int oldRank) {
            int rank = countInfo.IndexByUser(userId);
            oldRank = rank;
            while (rank > 0 && countInfo[rank].Count > countInfo[rank - 1].Count)
                countInfo.Swap(rank, --rank);
            while (rank < countInfo.UsersCount - 1 && countInfo[rank].Count < countInfo[rank + 1].Count)
                countInfo.Swap(rank, ++rank);
            newRank = rank;
        }

        public static async Task ResolveCountChampRole(CountInfo countInfo, IGuildUser user, bool nowHas = true) {
            if (countInfo.ChampRole > 0) {
                if (nowHas && !user.RoleIds.Contains(countInfo.ChampRole))
                    await user.AddRoleAsync(countInfo.ChampRole);
                else if (!nowHas && user.RoleIds.Contains(countInfo.ChampRole))
                    await user.RemoveRoleAsync(countInfo.ChampRole);

                if (countInfo.UsersCount > 1) {
                    if (nowHas) {
                        IGuildUser oldTop = await user.Guild.GetUserAsync(countInfo[1].User);
                        if (oldTop is not null && oldTop.RoleIds.Contains(countInfo.ChampRole))
                            await oldTop.RemoveRoleAsync(countInfo.ChampRole);
                    } else {
                        IGuildUser newTop = await user.Guild.GetUserAsync(countInfo[0].User);
                        if (newTop is not null && !newTop.RoleIds.Contains(countInfo.ChampRole))
                            await newTop.AddRoleAsync(countInfo.ChampRole);
                    }
                }
            }
        }

        public static void AddSlashCommands(List<ApplicationCommandProperties> commands) {
            SlashCommandBuilder setCountChannel = new SlashCommandBuilder().WithName(CountChannelCommandName)
                .WithDescription("The channel where counting can take place.")
                .WithDefaultPermission(false)
                .WithDMPermission(false)
                .AddOption(new SlashCommandOptionBuilder().WithName(SetSubCommandName)
                    .WithDescription("Sets the channel for counting.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel for counting", isRequired: true))
                .AddOption(RemoveSubCommandName, ApplicationCommandOptionType.SubCommand, "Stops counting from being recorded in the channel");
            commands.Add(setCountChannel.Build());

            SlashCommandBuilder setCountChampRole = new SlashCommandBuilder().WithName(CountChampRoleCommandName)
                .WithDescription("The role for the member with the highest count.")
                .WithDefaultPermission(false)
                .WithDMPermission(false)
                .AddOption(new SlashCommandOptionBuilder().WithName(SetSubCommandName)
                    .WithDescription("Sets the role for the count champ.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("role", ApplicationCommandOptionType.Role, "The role for the count champ", isRequired: true))
                .AddOption(RemoveSubCommandName, ApplicationCommandOptionType.SubCommand, "Stops the previous role from being given to new count champs.");
            commands.Add(setCountChampRole.Build());

            SlashCommandBuilder changeCount = new SlashCommandBuilder().WithName(ChangeCountCommandName)
                .WithDescription("Change the current count of the server.")
                .WithDefaultPermission(false)
                .WithDMPermission(false)
                .AddOption(new SlashCommandOptionBuilder().WithName(SetSubCommandName)
                    .WithDescription("Set the count to the specified value.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("count", ApplicationCommandOptionType.Integer, "The count to set to", isRequired: true, minValue: 0))
                .AddOption(ResetSubCommandName, ApplicationCommandOptionType.SubCommand, "Resets the count and user counts to 0")
                .AddOption(RecoverSubCommandName, ApplicationCommandOptionType.SubCommand, "Use this if the counts have been desynced.")
                .AddOption(new SlashCommandOptionBuilder().WithName(UserSubCommandName)
                    .WithDescription("Change the current count of a specified user.")
                    .WithType(ApplicationCommandOptionType.SubCommandGroup)
                    .AddOption(new SlashCommandOptionBuilder().WithName(SetSubCommandName)
                        .WithDescription("Set the user's count to the specified value.")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("user", ApplicationCommandOptionType.User, "The user to change the count of.", isRequired: true)
                        .AddOption("count", ApplicationCommandOptionType.Integer, "The count to set to", isRequired: true, minValue: 0))
                    .AddOption(new SlashCommandOptionBuilder().WithName(ResetSubCommandName)
                        .WithDescription("Resets the user's count to 0 and removes them from the leaderboard.")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("user", ApplicationCommandOptionType.User, "The user to reset the count of.", isRequired: true)));
            commands.Add(changeCount.Build());

            SlashCommandBuilder count = new SlashCommandBuilder().WithName(CountCommandName)
                .WithDescription("Sends the current count.")
                .WithDefaultPermission(true)
                .WithDMPermission(false)
                .AddOption("ephemeral", ApplicationCommandOptionType.Boolean, "Whether or not other people will see the result of this command.");
            commands.Add(count.Build());

            SlashCommandBuilder userCount = new SlashCommandBuilder().WithName(UserCountCommandName)
                .WithDescription("Sends the current count of the specified user.")
                .WithDefaultPermission(true)
                .WithDMPermission(false)
                .AddOption("user", ApplicationCommandOptionType.User, "The user to get the count from.", isRequired: true)
                .AddOption("ephemeral", ApplicationCommandOptionType.Boolean, "Whether or not other people will see the result of this command.");
            commands.Add(userCount.Build());

            SlashCommandBuilder myCount = new SlashCommandBuilder().WithName(MyCountCommandName)
                .WithDescription("Sends your current count.")
                .WithDefaultPermission(true)
                .WithDMPermission(false)
                .AddOption("ephemeral", ApplicationCommandOptionType.Boolean, "Whether or not other people will see the result of this command.");
            commands.Add(myCount.Build());

            SlashCommandBuilder leaderboard = new SlashCommandBuilder().WithName(LeaderboardCommandName)
                .WithDescription("Sends the counting leaderboard of the server.")
                .WithDefaultPermission(true)
                .WithDMPermission(false)
                .AddOption("ephemeral", ApplicationCommandOptionType.Boolean, "Whether or not other people will see the result of this command.");
            commands.Add(leaderboard.Build());

            SlashCommandBuilder fullLeaderboard = new SlashCommandBuilder().WithName(FullLeaderboardCommandName)
                .WithDescription("Sends the full counting leaderboard of the server as a file.")
                .WithDefaultPermission(true)
                .WithDMPermission(false)
                .AddOption("ephemeral", ApplicationCommandOptionType.Boolean, "Whether or not other people will see the result of this command.");
            commands.Add(fullLeaderboard.Build());
        }

        public static async Task HandleCountCommands(SocketSlashCommand command) {
            if (command.GuildId is not null) {
                switch (command.CommandName) {
                    case CountChannelCommandName:
                        await SetCountChannel(command);
                        break;
                    case CountChampRoleCommandName:
                        await SetCountChampRole(command);
                        break;
                    case ChangeCountCommandName:
                        await ChangeCount(command);
                        break;
                    case CountCommandName:
                        await SendCount(command);
                        break;
                    case UserCountCommandName:
                    case MyCountCommandName:
                        await SendUserCount(command);
                        break;
                    case LeaderboardCommandName:
                        await Leaderboard(command);
                        break;
                    case FullLeaderboardCommandName:
                        await FullLeaderboard(command);
                        break;
                }
            }
        }

        public static async Task SetCountChannel(SocketSlashCommand command) {
            SocketSlashCommandDataOption subCommand = command.Data.Options.First();
            switch (subCommand.Name) {
                case SetSubCommandName: {
                    if (subCommand.Options.First().Value is ITextChannel channel) {
                        CountInfo countInfo = CountingGuilds.GetByGuild(command.GuildId.Value);
                        countInfo.Channel = channel.Id;
                        GetCountQueue(countInfo).Clear();

                        SaveCounts();

                        await command.SendSuccess($"Set command log channel to <#{channel.Id}>", false);
                    } else
                        await command.SendError($"The channel must be a standard text channel.");
                    break;
                }
                case RemoveSubCommandName: {
                    CountInfo countInfo = CountingGuilds.GetByGuildOrDefault(command.GuildId.Value);
                    if (countInfo is not null && countInfo.Channel != 0) {
                        ulong prev = countInfo.Channel;
                        countInfo.Channel = 0;
                        GetCountQueue(countInfo).Clear();

                        SaveCounts();

                        await command.SendSuccess($"Removed <#{prev}> as the counting channel", false);
                    } else
                        await command.SendError("This server does not have a count channel set.");
                    break;
                }
            }
        }

        public static async Task SetCountChampRole(SocketSlashCommand command) {
            SocketSlashCommandDataOption subCommand = command.Data.Options.First();
            switch (subCommand.Name) {
                case SetSubCommandName: {
                    IRole role = subCommand.Options.First().Value as IRole;
                    CountInfo countInfo = CountingGuilds.GetByGuild(command.GuildId.Value);
                    countInfo.ChampRole = role.Id;

                    if (countInfo.UsersCount > 0 && countInfo[0].Count > 0) {
                        IGuild guild = BayBotCode.Bot.GetGuild(command.GuildId.Value);
                        await (await guild.GetUserAsync(countInfo[0].User)).AddRoleAsync(countInfo.ChampRole);
                    }

                    SaveCounts();

                    await command.SendSuccess($"Set counting champion role to <@&{role.Id}>", false);
                    break;
                }
                case RemoveSubCommandName: {
                    CountInfo countInfo = CountingGuilds.GetByGuildOrDefault(command.GuildId.Value);
                    if (countInfo is not null && countInfo.ChampRole != 0) {
                        ulong prev = countInfo.ChampRole;
                        countInfo.ChampRole = 0;

                        SaveCounts();

                        await command.SendSuccess($"Removed <@&{prev}> as the counting champion role", false);
                    } else
                        await command.SendError("This server does not have a count champ role set.");
                    break;
                }
            }
        }

        public static async Task ChangeCount(SocketSlashCommand command) {
            SocketSlashCommandDataOption subCommand = command.Data.Options.First();
            switch (subCommand.Name) {
                case SetSubCommandName: {
                    ulong newCount = (ulong)(long)subCommand.Options.First().Value;
                    CountInfo countInfo = CountingGuilds.GetByGuild(command.GuildId.Value);
                    countInfo.Count = newCount;

                    SaveCounts();

                    await command.SendSuccess($"Set count to {newCount}.", false);
                    break;
                }
                case ResetSubCommandName: {
                    CountInfo countInfo = CountingGuilds.GetByGuildOrDefault(command.GuildId.Value);
                    if (countInfo is not null) {
                        GetCountQueueThread(countInfo)?.Interrupt();
                        GetCountQueue(countInfo).Clear();
                        countInfo.Count = 0;
                        countInfo.Clear();

                        SaveCounts();

                        await command.SendSuccess("Reset count and all user counts.", false);
                    } else
                        await command.SendError("This server has not counted yet.");
                    break;
                }
                case RecoverSubCommandName: {
                    CountInfo countInfo = CountingGuilds.GetByGuildOrDefault(command.GuildId.Value);
                    if (countInfo is not null) {
                        if (countInfo.Channel != 0) {
                            if (countInfo.LastMessage != 0) {
                                IGuild guild = BayBotCode.Bot.GetGuild(command.GuildId.Value);
                                ITextChannel countingChannel = await guild.GetTextChannelAsync(countInfo.Channel);
                                if (countingChannel is not null) {
                                    List<IMessage> unreadMessages = new(await countingChannel.GetMessagesAsync(countInfo.LastMessage, Direction.After).FlattenAsync());
                                    if (unreadMessages.Count > 0) {
                                        unreadMessages.Sort((a, b) => {
                                            long dif = a.Timestamp.Ticks - b.Timestamp.Ticks;
                                            return dif == 0 ? 0 : dif > 0 ? 1 : -1; // need this because it expects an int
                                        });
                                        foreach (IMessage unreadMessage in unreadMessages)
                                            HandleCount(unreadMessage);

                                        SaveCounts();

                                        await command.SendSuccess("Successfully recovered the count in the server.", false);
                                    } else
                                        await command.SendError("There has not been any messages since the last recorded message.");
                                } else
                                    await command.SendError("Could not find the counting channel.");
                            } else
                                await command.SendError("This server has not had a counting message sent before.");
                        } else
                            await command.SendError("This server does not have a count channel set.");
                    } else
                        await command.SendError("This server has not counted yet.");
                    break;
                }
                case UserSubCommandName: {
                    SocketSlashCommandDataOption subSubCommand = subCommand.Options.First();
                    IGuildUser user = subSubCommand.Options.First().Value as IGuildUser;
                    CountInfo countInfo = CountingGuilds.GetByGuild(command.GuildId.Value);
                    UserCount userCount = countInfo.GetByUser(user.Id);
                    switch (subSubCommand.Name) {
                        case SetSubCommandName: {
                            ulong newCount = (ulong)(long)subSubCommand.Options.ElementAt(1).Value;
                            userCount.Count = newCount;

                            ResolveUserCountRank(countInfo, user.Id, out int rank, out int oldRank);
                            if (rank < oldRank && rank == 0 || countInfo.UsersCount == 1)
                                await ResolveCountChampRole(countInfo, user, true);
                            else if (rank > oldRank && oldRank == 0)
                                await ResolveCountChampRole(countInfo, user, false);

                            SaveCounts();

                            await command.SendSuccess($"Set count of <@{user.Id}> to {newCount}", false);
                            break;
                        }
                        case ResetSubCommandName: {
                            bool wasTop = countInfo[0].User == user.Id;

                            if (wasTop)
                                await user.RemoveRoleAsync(countInfo.ChampRole);

                            countInfo.UserCounts.Remove(userCount);

                            if (wasTop && countInfo.UserCounts.Count > 0 && countInfo[0].Count > 0) {
                                IGuildUser newTop = await user.Guild.GetUserAsync(countInfo[0].User);
                                await newTop.AddRoleAsync(countInfo.ChampRole);
                            }

                            SaveCounts();

                            await command.SendSuccess($"Removed count for <@{user.Id}>.", false);
                            break;
                        }
                    }
                    break;
                }
            }
        }

        public static async Task SendCount(SocketSlashCommand command) {
            bool ephemeral = false;
            if (command.Data.Options.Count > 0)
                ephemeral = (bool)command.Data.Options.First().Value;

            CountInfo countInfo = CountingGuilds.GetByGuild(command.GuildId.Value);

            IGuild guild = BayBotCode.Bot.GetGuild(command.GuildId.Value);

            EmbedBuilder embedBuilder = new();
            embedBuilder.WithAuthor($"{guild.Name} Count", guild.IconUrl);
            embedBuilder.WithColor(Color.Orange);
            embedBuilder.WithDescription($"Count: {countInfo.Count}");
            await command.RespondAsync(embed: embedBuilder.Build(), ephemeral: ephemeral);
        }

        public static async Task SendUserCount(SocketSlashCommand command) {
            IUser user = command.User;
            bool ephemeral = false;
            void FillOptions(SocketSlashCommandDataOption option) {
                switch (option.Name) {
                    case "user":
                        user = option.Value as IUser;
                        break;
                    case "ephemeral":
                        ephemeral = (bool)option.Value;
                        break;
                }
            }
            if (command.Data.Options.Count > 0) {
                SocketSlashCommandDataOption first = command.Data.Options.First();
                FillOptions(first);
                if (command.Data.Options.Count > 1) {
                    SocketSlashCommandDataOption second = command.Data.Options.ElementAt(1);
                    FillOptions(second);
                }
            }

            CountInfo countInfo = CountingGuilds.GetByGuild(command.GuildId.Value);
            UserCount userCount = countInfo.GetByUser(user.Id);

            EmbedBuilder embedBuilder = new();
            embedBuilder.WithAuthor($"{user.Username} Count", user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
            embedBuilder.WithColor(Color.Orange);
            embedBuilder.WithDescription($"Count: {userCount.Count}");
            await command.RespondAsync(embed: embedBuilder.Build(), ephemeral: ephemeral);
        }

        public static async Task Leaderboard(SocketSlashCommand command) {
            bool ephemeral = false;
            if (command.Data.Options.Count > 0)
                ephemeral = (bool)command.Data.Options.First().Value;

            List<UserCount> lb = new(10);

            CountInfo countInfo = CountingGuilds.GetByGuild(command.GuildId.Value);
            lb.AddRange(countInfo.UserCounts.Take(10));
            lb.RemoveAll(uc => uc.Count == 0);

            IGuild guild = BayBotCode.Bot.GetGuild(command.GuildId.Value);

            EmbedBuilder embedBuilder = new();
            embedBuilder.WithAuthor($"{guild.Name} Counting Leaderboard", guild.IconUrl);
            embedBuilder.WithColor(Color.Orange);

            if (lb.Count > 0) {
                string desc = "";
                for (int i = 0; i < lb.Count && lb[i].Count > 0; i++) {
                    int rank = i + 1;
                    string front = rank == 1 ? "🥇" : rank == 2 ? "🥈" : rank == 3 ? "🥉" : $"**#{rank}**";
                    desc += $"{front} <@{lb[i].User}>\n⠀⠀Count: {lb[i].Count}";
                    if (i < lb.Count - 1)
                        desc += "\n\n";
                }
                embedBuilder.WithDescription(desc);
            } else
                embedBuilder.WithDescription("*No Data*");

            await command.RespondAsync(embed: embedBuilder.Build(), ephemeral: ephemeral);
        }

        public static async Task FullLeaderboard(SocketSlashCommand command) {
            bool ephemeral = false;
            if (command.Data.Options.Count > 0)
                ephemeral = (bool)command.Data.Options.First().Value;

            CountInfo countInfo = CountingGuilds.GetByGuild(command.GuildId.Value);

            IGuild guild = BayBotCode.Bot.GetGuild(command.GuildId.Value);

            string title = $"{guild.Name} Counting Full Leaderboard\n";

            string fullLb = title;
            for (int i = 0; i < countInfo.UserCounts.Count; i++) {
                fullLb += $"{i + 1}.\t";
                UserCount userCount = countInfo.UserCounts[i];
                fullLb += userCount.User;
                fullLb += $": {userCount.Count}\n";
            }
            Data.WriteAllText("fullLb.txt", fullLb);

            EmbedBuilder embedBuilder = new();
            embedBuilder.WithAuthor(title);
            embedBuilder.WithColor(Color.Orange);

            await command.RespondWithFileAsync(Data.GetFilePath("fullLb.txt"), embed: embedBuilder.Build(), ephemeral: ephemeral);
        }

        public static async Task<bool> RecoverChannel(IMessage message, string word) {
            if (message.Author.Id == ImportantUsers.BotDev && message.Channel is ITextChannel channel) {
                await message.DeleteAsync();
                ulong firstId = ulong.Parse(word);
                IMessage lastMessage = (await channel.GetMessagesAsync(1).FlattenAsync()).First();
                IMessage firstMessage = await channel.GetMessageAsync(firstId);
                List<IMessage> messages = new();
                do {
                    messages.AddRange((await channel.GetMessagesAsync(firstMessage, Direction.After).FlattenAsync()).Reverse());
                    firstMessage = messages.Last();
                    await Task.Delay(100);
                    Logger.WriteLine($"{messages.Count} {firstMessage.Content}");
                } while (messages.Last().Id != lastMessage.Id);
                CountInfo countInfo = CountingGuilds.GetByGuild(channel.Guild.Id);
                UserCount userCount = countInfo.GetByUser(lastMessage.Author.Id);
                userCount.Count++;
                foreach (IMessage m in messages) {
                    userCount = countInfo.GetByUser(m.Author.Id);
                    userCount.Count++;
                }
                countInfo.Sort();
                SaveCounts();
                Logger.WriteLine("done");
            }
            return false;
        }
    }
}
