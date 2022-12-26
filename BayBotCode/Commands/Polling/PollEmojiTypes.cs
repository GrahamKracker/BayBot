namespace BayBot.Commands.Polling {
    /// <summary>
    /// All the types of emojis
    /// </summary>
    public enum PollEmojiTypes : byte {
        /// <summary>
        /// 👍👎
        /// </summary>
        Thumbs,
        /// <summary>
        /// 🇦🇧🇨🇩🇪🇫🇬🇭🇮🇯🇰🇱🇲🇳🇴🇵🇶🇷🇸🇹🇺🇻🇼🇽🇾🇿
        /// </summary>
        Letters,
        /// <summary>
        /// 1️⃣2️⃣3️⃣4️⃣5️⃣6️⃣7️⃣8️⃣9️⃣🔟
        /// </summary>
        Numbers,
        /// <summary>
        /// 🔴🟠🟡🟢🔵🟣🟤⚫⚪
        /// </summary>
        Circles,
        /// <summary>
        /// 🟥🟧🟨🟩🟦🟪🟫⬛⬜
        /// </summary>
        Squares
    }
}
