using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace BayBot.Commands.Counting {
    /// <summary>
    /// A class that represents a guild's count
    /// </summary>
    [XmlType("CountInfo")]
    [XmlInclude(typeof(UserCount))]
    public sealed class CountInfo {
        /// <summary>
        /// The id of the guild
        /// </summary>
        [XmlAttribute("Guild")]
        public ulong Guild { get; set; }

        /// <summary>
        /// The id of the channel that is counted in
        /// </summary>
        [XmlAttribute("Channel")]
        public ulong Channel { get; set; }

        /// <summary>
        /// The current count
        /// </summary>
        [XmlAttribute("Count")]
        public ulong Count { get; set; }

        /// <summary>
        /// The if of the role to give to the first place counter
        /// </summary>
        [XmlAttribute("ChampRole")]
        public ulong ChampRole { get; set; }

        /// <summary>
        /// The id of the last message that was recorded by BayBot for recovery
        /// </summary>
        [XmlAttribute("LastMessage")]
        public ulong LastMessage { get; set; }

        /// <summary>
        /// All the counts of the individual users of the server
        /// </summary>
        [XmlArray("UserCountArray")]
        [XmlArrayItem("UserCountItem")]
        public List<UserCount> UserCounts { get; } = new();

        /// <summary>
        /// The number of users who have counted in the server
        /// </summary>
        public int UsersCount => UserCounts.Count;

        /// <summary>
        /// Gets the <see cref="UserCount"/> at the specified index
        /// </summary>
        /// <param name="i">The index</param>
        /// <returns>The <see cref="UserCount"/></returns>
        public UserCount this[int i] {
            get => UserCounts[i];
            set => UserCounts[i] = value;
        }

        /// <summary>
        /// Removes all users from the counts
        /// </summary>
        public void Clear() => UserCounts.Clear();

        /// <summary>
        /// Gets a <see cref="UserCount"/> that matches the user id
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <returns>The <see cref="UserCount"/></returns>
        public UserCount GetByUser(ulong userId) {
            UserCount userCount = UserCounts.FirstOrDefault(uc => uc.User == userId);
            if (userCount is null) {
                userCount = new() { User = userId };
                UserCounts.Add(userCount);
            }
            return userCount;
        }

        /// <summary>
        /// Finds the position of the user
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <returns>The position</returns>
        public int IndexByUser(ulong userId) => UserCounts.FindIndex(uc => uc.User == userId);

        /// <summary>
        /// Sorts the users by largest count to smallest
        /// </summary>
        public void Sort() => UserCounts.Sort((uc1, uc2) => -uc1.Count.CompareTo(uc2.Count));

        /// <summary>
        /// Swaps two positions
        /// </summary>
        /// <param name="i1">Position 1</param>
        /// <param name="i2">Position 2</param>
        public void Swap(int i1, int i2) => (UserCounts[i2], UserCounts[i1]) = (UserCounts[i1], UserCounts[i2]);

        // To be able to use foreach loops
        public IEnumerator<UserCount> GetEnumerator() => UserCounts.GetEnumerator();
    }
}
