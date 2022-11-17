using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace BayBot.Polling {
    [XmlType("PollGuild")]
    public class PollGuild {
        [XmlAttribute("CurrentId")]
        public ulong CurrentId { get; set; }

        [XmlAttribute("Guild")]
        public ulong Guild {  get; set; }

        [XmlArray("Polls")]
        [XmlArrayItem("Poll")]
        public List<Poll> Polls { get; set; } = new();

        public Poll GetById(ulong id) => Polls.FirstOrDefault(p => p.Id == id);

        public void RegisterNew(Poll poll) {
            poll.Id = CurrentId++;
            Polls.Add(poll);
        }
    }
}
