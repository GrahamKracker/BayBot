using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace BayBot.Commands.Echo {
    // TODO: make generic superclass shared with PollList
    /// <summary>
    /// Contains all the MessageGuilds which all contain Messages
    /// </summary>
    [XmlType("MessageList")]
    [XmlInclude(typeof(MessageGuild))]
    public sealed class MessageList {
        /// <summary>
        /// All the guilds that have created polls
        /// </summary>
        [XmlArray("Guilds")]
        [XmlArrayItem("Guild")]
        public List<MessageGuild> Guilds { get; set; } = new();

        /// <summary>
        /// Gets a <see cref="MessageGuild"/> that matches the guild id
        /// </summary>
        /// <param name="guild">The id of the guild</param>
        /// <returns>The <see cref="MessageGuild"/></returns>
        public MessageGuild GetGuildById(ulong guild) {
            MessageGuild g = Guilds.FirstOrDefault(g => g.Guild == guild);
            if (g is null) {
                g = new() { Guild = guild };
                Guilds.Add(g);
            }
            return g;
        }

        /// <summary>
        /// Gets a <see cref="MessageGuild"/> that matches the guild id
        /// </summary>
        /// <param name="guild">The id of the guild</param>
        /// <returns>The <see cref="MessageGuild"/> or null if not found</returns>
        public MessageGuild GetGuildByIdOrDefault(ulong guild) => Guilds.FirstOrDefault(g => g.Guild == guild);

        // To be able to use foreach loops
        public IEnumerator<MessageGuild> GetEnumerator() => Guilds.GetEnumerator();
    }
}
