using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace BayBot.Polling {
    [XmlRoot("PollList")]
    [XmlInclude(typeof(PollGuild))]
    public sealed class PollList {
        [XmlArray("Guilds")]
        [XmlArrayItem("Guild")]
        public List<PollGuild> Guilds { get; set; } = new();

        public PollGuild GetGuildById(ulong guild) {
            PollGuild g = Guilds.FirstOrDefault(g => g.Guild == guild);
            if (g is null) {
                g = new PollGuild();
                g.Guild = guild;
                Guilds.Add(g);
            }
            return g;
        }

        public IEnumerator<PollGuild> GetEnumerator() => Guilds.GetEnumerator();
    }
}
