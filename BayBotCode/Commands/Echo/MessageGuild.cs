using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace BayBot.Commands.Echo {
    /// <summary>
    /// A container for messages that are grouped by guild
    /// </summary>
    [XmlType("MessageGuild")]
    [XmlInclude(typeof(Message))]
    public sealed class MessageGuild {
        /// <summary>
        /// The id of the guild
        /// </summary>
        [XmlAttribute("Guild")]
        public ulong Guild { get; set; }

        /// <summary>
        /// The messages contained in the guild
        /// </summary>
        [XmlArray("Messages")]
        [XmlArrayItem("Message")]
        public List<Message> Messages { get; set; } = new();

        /// <summary>
        /// Gets a <see cref="Message"/> based on its name
        /// </summary>
        /// <param name="name">The name of the <see cref="Message"/></param>
        /// <returns>The <see cref="Message"/> or null if not found</returns>
        public Message GetByNameOrDefault(string name) => Messages.FirstOrDefault(m => name.Equals(m.Name));
    }
}
