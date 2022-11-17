using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace BayBot.Polling {
    /// <summary>
    /// A list of all polls in a list of guilds
    /// </summary>
    [XmlRoot("PollList")]
    [XmlInclude(typeof(PollGuild))]
    public sealed class PollList {
        /// <summary>
        /// All the guilds that have created polls
        /// </summary>
        [XmlArray("Guilds")]
        [XmlArrayItem("Guild")]
        public List<PollGuild> Guilds { get; set; } = new();

        /// <summary>
        /// Gets a <see cref="PollGuild"/> that matches the guild id
        /// </summary>
        /// <param name="guild">The id of the guild</param>
        /// <returns>The <see cref="PollGuild"/></returns>
        public PollGuild GetGuildById(ulong guild) {
            PollGuild g = Guilds.FirstOrDefault(g => g.Guild == guild);
            if (g is null) {
                g = new() { Guild = guild };
                Guilds.Add(g);
            }
            return g;
        }

        /// <summary>
        /// Gets a <see cref="PollGuild"/> that matches the guild id
        /// </summary>
        /// <param name="guild">The id of the guild</param>
        /// <returns>The <see cref="PollGuild"/> or null if not found</returns>
        public PollGuild GetGuildByIdOrDefault(ulong guild) => Guilds.FirstOrDefault(g => g.Guild == guild);

        // To be able to use foreach loops
        public IEnumerator<PollGuild> GetEnumerator() => Guilds.GetEnumerator();
    }
}
