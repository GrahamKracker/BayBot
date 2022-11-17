using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace BayBot.Counting {
    /// <summary>
    /// A list of all counts in a list of servers
    /// </summary>
    [XmlRoot("CountInfoList")]
    [XmlInclude(typeof(CountInfo))]
    public sealed class CountInfoList {
        /// <summary>
        /// List of server counts
        /// </summary>
        [XmlArray("CountInfoArray")]
        [XmlArrayItem("CountInfoItem")]
        public List<CountInfo> List { get; set; } = new();

        /// <summary>
        /// Gets a <see cref="CountInfo"/> that matches the guild id
        /// </summary>
        /// <param name="guildId">The id of the guild</param>
        /// <returns>The <see cref="CountInfo"/></returns>
        public CountInfo GetByGuild(ulong guildId) {
            CountInfo countInfo = List.FirstOrDefault(ci => ci.Guild == guildId);
            if (countInfo is null) {
                countInfo = new() { Guild = guildId };
                List.Add(countInfo);
            }
            return countInfo;
        }

        /// <summary>
        /// Gets a <see cref="CountInfo"/> that matches the guild id
        /// </summary>
        /// <param name="guildId">The id of the guild</param>
        /// <returns>The <see cref="CountInfo"/> or null if not found</returns>
        public CountInfo GetByGuildOrDefault(ulong guildId) => List.FirstOrDefault(ci => ci.Guild == guildId);

        // To be able to use foreach loops
        public IEnumerator<CountInfo> GetEnumerator() => List.GetEnumerator();
    }
}
