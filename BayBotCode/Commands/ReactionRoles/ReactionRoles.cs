using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BayBot.Commands.ReactionRoles {
    public static class ReactionRoles {
        private const string ReactionRoleCommandName = "reactionrole";

        private const string OptionTitleName = "title";
        private const string OptionDescriptionName = "description";
        private const string OptionMultipleName = "allowmultiple";
        private const string OptionRoleName = "role";
        private const string OptionEmojiName = "emoji";

        public static void AddSlashCommands(List<ApplicationCommandProperties> commands) {
            SlashCommandBuilder reactionRole = new SlashCommandBuilder().WithName(ReactionRoleCommandName)
                .WithDescription("Set up a message where members can assign their own roles.")
                .WithDefaultPermission(false)
                .WithDMPermission(false)
                .AddOption(OptionTitleName, ApplicationCommandOptionType.String, "Set the title of the message.")
                .AddOption(OptionDescriptionName, ApplicationCommandOptionType.String, "Set the description of the message.")
                .AddOption(OptionMultipleName, ApplicationCommandOptionType.Boolean, "Set to false if members can only select one role. (default true)")
                .AddOptions(Enumerable.Range(1, 20).Select(num => {
                    int n = (int)Math.Ceiling(num / 2f);
                    return num switch {
                        int _ when num % 2 == 1 => new SlashCommandOptionBuilder().WithName($"{OptionRoleName}{n}")
                            .WithDescription($"Role {n}")
                            .WithRequired(n == 1)
                            .WithType(ApplicationCommandOptionType.Role),
                        _ => new SlashCommandOptionBuilder().WithName($"{OptionEmojiName}{n}")
                            .WithDescription($"Role {n} emoji")
                            .WithRequired(false)
                            .WithType(ApplicationCommandOptionType.String)
                    };
                }).ToArray());
            commands.Add(reactionRole.Build());
        }

        public static async Task HandleCommands(SocketSlashCommand command) {
            if (command.GuildId is not null && command.CommandName.Equals(ReactionRoleCommandName)) {
                string title = null;
                string description = null;
                bool multiple = true;
                SortedDictionary<int, (IRole, IEmote)> roles = new();

                foreach (SocketSlashCommandDataOption option in command.Data.Options) {
                    switch (option.Name) {
                        case OptionTitleName: {
                            title = option.Value as string;
                            break;
                        }
                        case OptionDescriptionName: {
                            description = option.Value as string;
                            break;
                        }
                        case OptionMultipleName: {
                            multiple = (bool)option.Value;
                            break;
                        }
                        case string s when s.StartsWith(OptionRoleName): {
                            int index = int.Parse(s[OptionRoleName.Length..]);
                            if (roles.ContainsKey(index)) {
                                (IRole role, IEmote) roleEmotePair = roles[index];
                                roleEmotePair.role = option.Value as IRole;
                                roles[index] = roleEmotePair;
                            } else
                                roles[index] = (option.Value as IRole, null);
                            break;
                        }
                        case string s when s.StartsWith(OptionEmojiName): {
                            int index = int.Parse(s[OptionEmojiName.Length..]);

                            IEmote iEmote = null;
                            if (Emoji.TryParse(option.Value as string, out Emoji emoji))
                                iEmote = emoji;
                            else if (Emote.TryParse(option.Value as string, out Emote emote))
                                iEmote = emote;

                            if (iEmote is null)
                                continue;
                            
                            if (roles.ContainsKey(index)) {
                                (IRole, IEmote emote) roleEmotePair = roles[index];
                                roleEmotePair.emote = iEmote;
                                roles[index] = roleEmotePair;
                            } else
                                roles[index] = (null, iEmote);

                            break;
                        }
                    }
                }

                if (description is null) {
                    if (multiple)
                        description = "Use this to assign yourself some roles.";
                    else
                        description = "Use this to assign yourself a role.";
                }

                EmbedBuilder embed = new EmbedBuilder().WithDescription(description)
                    .WithTitle(title);

                ComponentBuilder components = new();

                if (multiple) {
                    int i = 0;
                    foreach((IRole role, IEmote emote) in roles.Values) {
                        ButtonBuilder button = new ButtonBuilder().WithCustomId($"{i++}")
                            .WithStyle(ButtonStyle.Secondary)
                            .WithEmote(emote)
                            .WithLabel(role.Name);
                        components.WithButton(button);
                    }
                } else {
                    SelectMenuBuilder selectMenu = new SelectMenuBuilder().WithPlaceholder("Choose a role...")
                        .WithMinValues(1)
                        .WithMaxValues(1)
                        .AddOption("None", "-1");
                    int i = 0;
                    foreach((IRole role, IEmote emote) in roles.Values)
                        selectMenu.AddOption(role.Name, $"{i++}", emote: emote);
                    components.WithSelectMenu(selectMenu);
                }

                await command.RespondAsync(embed: embed.Build(), components: components.Build());
            }
        }
    }
}
