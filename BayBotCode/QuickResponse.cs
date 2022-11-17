using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace BayBot {
    /// <summary>
    /// Used for quickly responding to slash commands with simple responses
    /// </summary>
    internal static class QuickResponse {
        /// <summary>
        /// Sends an error message as a response to a slash command
        /// </summary>
        /// <param name="command">The slash command</param>
        /// <param name="message">The error message</param>
        /// <param name="ephemeral">Whether or not to only show this to the user who made the slash command</param>
        public static async Task SendError(this SocketSlashCommand command, string message, bool ephemeral = true) =>
            await SendQuickResponse(command, message, Color.Red, ephemeral);
        /// <summary>
        /// Sends a success message as a response to a slash command
        /// </summary>
        /// <param name="command">The slash command</param>
        /// <param name="message">The success message</param>
        /// <param name="ephemeral">Whether or not to only show this to the user who made the slash command</param>
        public static async Task SendSuccess(this SocketSlashCommand command, string message, bool ephemeral = true) =>
            await SendQuickResponse(command, message, Color.Green, ephemeral);

        private static async Task SendQuickResponse(SocketSlashCommand command, string message, Color color, bool ephemeral) =>
            await command.RespondAsync(embed: new EmbedBuilder().WithDescription(message).WithColor(color).Build(), ephemeral: ephemeral);
    }
}
