using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace BayBot.Commands.Polling {
    /// <summary>
    /// A class that conatians a guild's polls
    /// </summary>
    [XmlType("PollGuild")]
    public class PollGuild {
        /// <summary>
        /// The highest id that has been used for a poll
        /// </summary>
        [XmlAttribute("CurrentId")]
        public ulong CurrentId { get; set; }

        /// <summary>
        /// The id of the guild
        /// </summary>
        [XmlAttribute("Guild")]
        public ulong Guild { get; set; }

        /// <summary>
        /// List of polls active in this guild
        /// </summary>
        [XmlArray("Polls")]
        [XmlArrayItem("Poll")]
        public List<Poll> Polls { get; set; } = new();

        /// <summary>
        /// Gets a <see cref="Poll"/> based on its id
        /// </summary>
        /// <param name="id">The if of the <see cref="Poll"/></param>
        /// <returns>The <see cref="Poll"/> or null if not found</returns>
        public Poll GetByIdOrDefault(ulong id) => Polls.FirstOrDefault(p => p.Id == id);

        /// <summary>
        /// Adds the <see cref="Poll"/> to the list and assigns its id
        /// </summary>
        /// <param name="poll">The <see cref="Poll"/> to be added</param>
        public void RegisterNew(Poll poll) {
            poll.Id = CurrentId++;
            Polls.Add(poll);
        }
    }
}
