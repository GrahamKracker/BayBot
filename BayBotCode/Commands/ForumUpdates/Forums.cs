using System.Xml.Serialization;

namespace BayBot.Commands.ForumUpdates {
    /// <summary>
    /// Represents multiple forum channels to monitor
    /// </summary>
    [XmlType("Forums")]
    public sealed class Forums {
        /// <summary>
        /// The ids of the forum channels
        /// </summary>
        [XmlArray("Ids")]
        [XmlArrayItem("Id")]
        public ulong[] Ids { get; set; }

        /// <summary>
        /// The id of the channel to send updates to
        /// </summary>
        [XmlAttribute("UpdateChannel")]
        public ulong UpdateChannel { get; set; }

        /// <summary>
        /// The message to prepend to the update message
        /// </summary>
        public string Message { get; set; }
    }
}
