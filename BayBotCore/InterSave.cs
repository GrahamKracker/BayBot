using System.Collections.Generic;

namespace BayBot.Core {
    // Used to save BayBotCode variables while a new version of it is being loaded
    public static class InterSave {
        private static Dictionary<string, object> Contents { get; } = new();

        /// <summary>
        /// Gets the saved variable of the given name
        /// </summary>
        /// <param name="name">The name of the variable</param>
        /// <returns>The value of the variable</returns>
        public static object Get(string name) {
            if (Contents.TryGetValue(name, out object value))
                return value;
            return null;
        }

        /// <summary>
        /// Sets the given name to the given value
        /// </summary>
        /// <param name="name">The name of the variable</param>
        /// <param name="value">The value of the variable</param>
        public static void Set(string name, object value) {
            if (Contents.ContainsKey(name))
                Contents[name] = value;
            else
                Contents.Add(name, value);
        }

        /// <summary>
        /// Whether or not the a variable of the given name exists
        /// </summary>
        /// <param name="name">The name of the variable</param>
        /// <returns>True if the variable exists</returns>
        public static bool Contains(string name) => Contents.ContainsKey(name);
    }
}
