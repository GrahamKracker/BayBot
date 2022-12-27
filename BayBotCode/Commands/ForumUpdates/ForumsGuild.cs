using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace BayBot.Commands.ForumUpdates {
    /// <summary>
    /// Represents a list of forums seperated by guild and by command use
    /// </summary>
    [XmlType("ForumGuild")]
    [XmlInclude(typeof(Forums))]
    public sealed class ForumsGuild {
        /// <summary>
        /// The id of the guild
        /// </summary>
        [XmlAttribute("Guild")]
        public ulong Guild { get; set; }

        /// <summary>
        /// All the forums in the guild to check for updates
        /// </summary>
        [XmlArray("ForumsList")]
        [XmlArrayItem("ForumsGroup")]
        public List<Forums> Forums { get; set; } = new();

        /// <summary>
        /// Gets a Forums group by the update channel it sends to
        /// </summary>
        /// <param name="channel">The update channel</param>
        /// <returns>The Forums group</returns>
        public Forums GetByUpdateChannelOrDefault(ulong channel) => Forums.FirstOrDefault(f => f.UpdateChannel == channel);

        /// <summary>
        /// Gets a Forums group by a forum channel it reads from
        /// </summary>
        /// <param name="forumChannel">The forum channel</param>
        /// <returns>The Forums group</returns>
        public Forums GetByForumChannelOrDefault(ulong forumChannel) => Forums.FirstOrDefault(f => f.Ids.Contains(forumChannel));
    }
}
