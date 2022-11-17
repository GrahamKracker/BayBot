using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace BayBot {
    /// <summary>
    /// Used for quickly responding to interactions such as slash commands with simple embeded responses
    /// </summary>
    internal static class QuickResponse {
        /// <summary>
        /// Sends an error message as a response to a interaction
        /// </summary>
        /// <param name="interaction">The interaction</param>
        /// <param name="message">The error message</param>
        /// <param name="ephemeral">Whether or not to only show this to the user who made the slash command</param>
        public static async Task SendError(this SocketInteraction interaction, string message, bool ephemeral = true) =>
            await SendResponse(interaction, message, Color.Red, ephemeral);
        /// <summary>
        /// Sends a success message as a response to a interaction
        /// </summary>
        /// <param name="interation">The interaction</param>
        /// <param name="message">The success message</param>
        /// <param name="ephemeral">Whether or not to only show this to the user who made the slash command</param>
        public static async Task SendSuccess(this SocketInteraction interation, string message, bool ephemeral = true) =>
            await SendResponse(interation, message, Color.Green, ephemeral);

        private static async Task SendResponse(SocketInteraction interaction, string message, Color color, bool ephemeral) =>
            await interaction.RespondAsync(embed: new EmbedBuilder().WithDescription(message).WithColor(color).Build(), ephemeral: ephemeral);

        /// <summary>
        /// Follows up with an error message as a response to a interaction
        /// </summary>
        /// <param name="interaction">The interaction</param>
        /// <param name="message">The error message</param>
        /// <param name="ephemeral">Whether or not to only show this to the user who made the slash command</param>
        public static async Task FollowupError(this SocketInteraction interaction, string message, bool ephemeral = true) =>
            await FollowupResponse(interaction, message, Color.Red, ephemeral);
        /// <summary>
        /// Follows up with a success message as a response to a interaction
        /// </summary>
        /// <param name="interation">The interaction</param>
        /// <param name="message">The success message</param>
        /// <param name="ephemeral">Whether or not to only show this to the user who made the slash command</param>
        public static async Task FollowupSuccess(this SocketInteraction interation, string message, bool ephemeral = true) =>
            await FollowupResponse(interation, message, Color.Green, ephemeral);

        private static async Task FollowupResponse(SocketInteraction interaction, string message, Color color, bool ephemeral) =>
            await interaction.FollowupAsync(embed: new EmbedBuilder().WithDescription(message).WithColor(color).Build(), ephemeral: ephemeral);
    }
}
