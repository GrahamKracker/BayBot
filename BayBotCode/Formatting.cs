using System.Collections.Generic;

namespace BayBot {
    /// <summary>
    /// A class for formatting strings
    /// </summary>
    internal class Formatting {
        /// <summary>
        /// Makes a word plural or not
        /// </summary>
        /// <param name="word">The word to make plural</param>
        /// <param name="number">The amount of the word</param>
        /// <returns>The word but plural if it needs to be, or just the word otherwise</returns>
        public static string MatchPlurality(string word, int number) => number == 1 ? word : word + 's';

        /// <summary>
        /// Lists items with the correct listing grammar
        /// </summary>
        /// <param name="items">The items to list out</param>
        /// <returns>The items in a list as a string</returns>
        public static string ListItems(IList<string> items) {
            if (items.Count == 0)
                return "";
            else if (items.Count == 1)
                return items[0];
            else if (items.Count == 2)
                return $"{items[0]} and {items[1]}";
            else {
                string list = "";
                for (int i = 0; i < items.Count - 1; i++)
                    list += $"{items[i]}, ";
                list += $"and {items[^1]}";
                return list;
            }
        }
    }
}
