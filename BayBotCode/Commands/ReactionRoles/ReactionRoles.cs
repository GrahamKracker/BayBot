using Discord;
using System.Collections.Generic;

namespace BayBot.Commands.ReactionRoles {
    public static class ReactionRoles {
        private const string ReactionRoleCommandName = "reactionrole";

        public static void AddSlashCommands(List<ApplicationCommandProperties> commands) {
            SlashCommandBuilder reactionRole = new SlashCommandBuilder().WithName(ReactionRoleCommandName)
                .WithDescription("Sets up a message where members can assign their own roles.");
        }
    }
}
