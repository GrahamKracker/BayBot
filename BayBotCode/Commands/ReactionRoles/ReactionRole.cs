using System.Xml.Serialization;

namespace BayBot.Commands.ReactionRoles {
    /// <summary>
    /// Represents a reaction role message
    /// </summary>
    [XmlType("ReactionRole")]
    public sealed class ReactionRole {
        /// <summary>
        /// The message that the reaction role is attached to
        /// </summary>
        [XmlAttribute("Message")]
        public ulong Message { get; set; }
    }
}
