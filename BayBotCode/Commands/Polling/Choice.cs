using System.Xml.Serialization;

namespace BayBot.Commands.Polling {
    /// <summary>
    /// Used as a store for a user and their choice
    /// </summary>
    [XmlType("Choice")]
    public sealed class Choice {
        /// <summary>
        /// The id of the user who made this choice
        /// </summary>
        [XmlAttribute("User")]
        public ulong User { get; set; }

        /// <summary>
        /// The index of the option chosen
        /// </summary>
        [XmlAttribute("Option")]
        public int Option { get; set; }
    }
}
