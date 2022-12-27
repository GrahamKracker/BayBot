using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace BayBot.Commands.ForumUpdates {
    /// <summary>
    /// List of forum groups, grouped by guild and shared update channel
    /// </summary>
    [XmlType("ForumUpdateList")]
    [XmlInclude(typeof(ForumsGuild))]
    public sealed class ForumsList {
        /// <summary>
        /// All the guilds
        /// </summary>
        [XmlArray("Guilds")]
        [XmlArrayItem("Guild")]
        public List<ForumsGuild> Guilds { get; set; } = new();

        /// <summary>
        /// Gets a <see cref="ForumsGuild"/> that matches the guild id
        /// </summary>
        /// <param name="guild">The id of the guild</param>
        /// <returns>The <see cref="ForumsGuild"/></returns>
        public ForumsGuild GetGuildById(ulong guild) {
            ForumsGuild g = Guilds.FirstOrDefault(g => g.Guild == guild);
            if (g is null) {
                g = new() { Guild = guild };
                Guilds.Add(g);
            }
            return g;
        }

        /// <summary>
        /// Gets a <see cref="ForumsGuild"/> that matches the guild id
        /// </summary>
        /// <param name="guild">The id of the guild</param>
        /// <returns>The <see cref="ForumsGuild"/> or null if not found</returns>
        public ForumsGuild GetGuildByIdOrDefault(ulong guild) => Guilds.FirstOrDefault(g => g.Guild == guild);

        // To be able to use foreach loops
        public IEnumerator<ForumsGuild> GetEnumerator() => Guilds.GetEnumerator();
    }
}
