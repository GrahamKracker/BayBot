using System.Xml.Serialization;

namespace BayBot.Commands.Echo {
    /// <summary>
    /// Contains a message paired with a name
    /// </summary>
    [XmlType("Message")]
    public sealed class Message {
        /// <summary>
        /// The name of the message
        /// </summary>
        [XmlAttribute("Name")]
        public string Name { get; set; }

        /// <summary>
        /// The actual message
        /// </summary>
        [XmlAttribute("Content")]
        public string Content { get; set; }
    }
}
