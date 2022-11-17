using System;
using System.Xml.Serialization;

namespace BayBot.Polling {
    [XmlType("Poll")]
    public sealed class Poll {

        [XmlAttribute("Id")]
        public ulong Id { get; set; }

        [XmlAttribute("Author")]
        public ulong Author { get; set; }

        [XmlAttribute("OriginalMessage")]
        public ulong OriginalMessage { get; set; }

        [XmlAttribute("Channel")]
        public ulong Channel { get; set; }

        [XmlAttribute("Title")]
        public string Title { get; set; } = string.Empty;

        [XmlAttribute("Question")]
        public string Question { get; set; } = string.Empty;

        [XmlAttribute("EmojiType")]
        public PollEmojiTypes EmojiType { get; set; } = PollEmojiTypes.Thumbs;

        [XmlArray("Options")]
        [XmlArrayItem("Options")]
        public string[] Options { get; set; } = null;

        [XmlAttribute("EndTime")]
        public DateTime EndTime { get; set; } = DateTime.MaxValue;
    }
}
