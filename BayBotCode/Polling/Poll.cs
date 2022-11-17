using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace BayBot.Polling {
    /// <summary>
    /// Represents a poll
    /// </summary>
    [XmlType("Poll")]
    public sealed class Poll {
        private static readonly Dictionary<PollEmojiTypes, string[]> EmojiTypes = new() {
            [PollEmojiTypes.Letters] = new string[] { "🇦", "🇧", "🇨", "🇩", "🇪", "🇫", "🇬", "🇭", "🇮", "🇯", "🇰", "🇱", "🇲", "🇳", "🇴", "🇵", "🇶", "🇷", "🇸", "🇹", "🇺", "🇻", "🇼", "🇽", "🇾", "🇿" },
            [PollEmojiTypes.Numbers] = new string[] { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟" },
            [PollEmojiTypes.Circles] = new string[] { "🔴", "🟠", "🟡", "🟢", "🔵", "🟣", "🟤", "⚫", "⚪" },
            [PollEmojiTypes.Squares] = new string[] { "🟥", "🟧", "🟨", "🟩", "🟦", "🟪", "🟫", "⬛", "⬜" },
            [PollEmojiTypes.Thumbs] = new string[] { "👍", "👎" }
        };

        /// <summary>
        /// The poll's id
        /// </summary>
        [XmlAttribute("Id")]
        public ulong Id { get; set; }

        /// <summary>
        /// The poll's author's id
        /// </summary>
        [XmlAttribute("Author")]
        public ulong Author { get; set; }

        /// <summary>
        /// The id of the message of the poll
        /// </summary>
        [XmlAttribute("OriginalMessage")]
        public ulong OriginalMessage { get; set; }

        /// <summary>
        /// The id of the channel that the poll was sent in
        /// </summary>
        [XmlAttribute("Channel")]
        public ulong Channel { get; set; }

        /// <summary>
        /// The title of the poll
        /// </summary>
        [XmlAttribute("Title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The question of the poll
        /// </summary>
        [XmlAttribute("Question")]
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// The type of emojis that are used for each option
        /// </summary>
        [XmlAttribute("EmojiType")]
        public PollEmojiTypes EmojiType { get; set; } = PollEmojiTypes.Thumbs;

        /// <summary>
        /// Each option
        /// </summary>
        [XmlArray("Options")]
        [XmlArrayItem("Option")]
        public string[] Options { get; set; } = null;

        /// <summary>
        /// Each choice a user has made
        /// </summary>
        [XmlArray("Choices")]
        [XmlArrayItem("Choice")]
        public List<Choice> Choices { get; set; } = new();

        /// <summary>
        /// The date and time that the poll ends
        /// </summary>
        [XmlAttribute("EndTime")]
        public DateTime EndTime { get; set; } = DateTime.MaxValue;

        /// <summary>
        /// Gets the choice made by the given user
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <returns>The choice made, or null if not found</returns>
        public Choice GetChoiceByUserOrDefault(ulong userId) => Choices.FirstOrDefault(c => c.User == userId);

        /// <summary>
        /// The final amount of options, taking into account options inputted and amount of emojis able to be used
        /// </summary>
        [XmlIgnore]
        public int OptionCount => Math.Min(Options.Length, EmojiTypes[EmojiType].Length);

        /// <summary>
        /// Gets the emojis being used
        /// </summary>
        [XmlIgnore]
        public string[] Emojis => EmojiTypes[EmojiType];

        /// <summary>
        /// Creates a poll embed
        /// </summary>
        /// <param name="isActive">Whether the poll is running or not</param>
        /// <param name="showResults">If the winner should be shown.</param>
        /// <returns>The poll embed</returns>
        public Embed BuildEmbed(bool isActive, bool showResults) {
            // Create an embed builder
            EmbedBuilder pollEmbed = new EmbedBuilder().WithColor(Color.Purple)
                .WithFooter($"Poll ID: {Id}");

            // Add title if exists
            if (!string.IsNullOrEmpty(Title))
                pollEmbed.WithTitle(Title);

            // Add question if exists
            if (!string.IsNullOrEmpty(Question))
                pollEmbed.AddField("Question:", Question);

            // Set up votes
            int[] optionVotes = new int[OptionCount];
            for (int i = 0; i < Choices.Count; i++)
                optionVotes[Choices[i].Option]++;

            // Find winner if showResults is true
            int winner = -1;
            if (showResults) {
                // Keep track of highest votes and options that meet it
                int maxVotes = -1;
                List<int> winning = new();

                // Iterate through options
                for (int i = 0; i < optionVotes.Length; i++) {
                    int votes = optionVotes[i];

                    // Change highest votes if found higher
                    if (votes > maxVotes) {
                        winning.Clear();
                        maxVotes = votes;
                    }

                    // If votes matches highest, then add to current winners
                    if (votes == maxVotes)
                        winning.Add(i);
                }

                // Test if there are multiple winners and use randomization if there are
                if (winning.Count > 1) {
                    winner = winning[BayBotCode.Random.Next(winning.Count)];
                    pollEmbed.Description += "\n*RNG broke this tie.*";
                } else
                    winner = winning[0];
            }

            // Get emojis used
            string[] emojis = EmojiTypes[EmojiType];

            // Add each option and the amount of votes
            for (int i = 0; i < optionVotes.Length; i++)
                pollEmbed.AddField($"{Options[i]}: {optionVotes[i]} {Formatting.MatchPlurality("vote", optionVotes[i])} {(winner == i ? "✅" : "")}", emojis[i], true);

            if (showResults) {
                // Add the winner to the end of the embed
                pollEmbed.AddField("Winner:", $"{Options[winner]} {emojis[winner]}");
            } else if (EndTime != DateTime.MaxValue) {
                // Add the end time to the end of the embed
                string name = isActive ? "Ends at:" : "Ended at:";
                TimestampTag endtag = TimestampTag.FromDateTimeOffset(EndTime, TimestampTagStyles.ShortDateTime);
                pollEmbed.AddField(name, endtag);
            }

            return pollEmbed.Build();
        }
    }
}
