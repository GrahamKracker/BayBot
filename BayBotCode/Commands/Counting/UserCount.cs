using System.Xml.Serialization;

namespace BayBot.Commands.Counting {
    /// <summary>
    /// A class that represents a user's individual count
    /// </summary>
    [XmlType("UserCount")]
    public sealed class UserCount {
        /// <summary>
        /// User id
        /// </summary>
        [XmlAttribute("User")]
        public ulong User { get; set; }

        /// <summary>
        /// User count
        /// </summary>
        [XmlAttribute("Count")]
        public ulong Count { get; set; }
    }
}
