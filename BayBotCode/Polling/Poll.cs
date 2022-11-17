using System;
using System.Xml.Serialization;

namespace BayBot.Polling {
    /// <summary>
    /// Represents a poll
    /// </summary>
    [XmlType("Poll")]
    public sealed class Poll {
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
        [XmlArrayItem("Options")]
        public string[] Options { get; set; } = null;

        /// <summary>
        /// The date and time that the poll ends
        /// </summary>
        [XmlAttribute("EndTime")]
        public DateTime EndTime { get; set; } = DateTime.MaxValue;
    }
}
