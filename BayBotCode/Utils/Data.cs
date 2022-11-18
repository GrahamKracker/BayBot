using BayBot.Core;
using System.IO;

namespace BayBot.Utils {
    /// <summary>
    /// Handles the saving a loading of files
    /// </summary>
    internal static class Data {
        /// <summary>
        /// The folder where all the saved and loaded files exist
        /// </summary>
        public static string Folder { get; set; }

        private static void LogError(string file) => Logger.WriteLine($"Could not access file {file}");

        /// <summary>
        /// Gets the full path of a file
        /// </summary>
        /// <param name="file">The file</param>
        /// <returns>The full path</returns>
        public static string GetFilePath(string file) => $"{Folder}/{file}";

        /// <summary>
        /// Loads a file as text
        /// </summary>
        /// <param name="file">The file</param>
        /// <returns>The text</returns>
        public static string ReadAllText(string file) {
            try {
                return File.ReadAllText($"{Folder}/{file}");
            } catch {
                LogError(file);
                return null;
            }
        }

        /// <summary>
        /// Saves a file as text
        /// </summary>
        /// <param name="file">The file</param>
        /// <param name="contents">The text</param>
        public static void WriteAllText(string file, string contents) {
            try {
                File.WriteAllText($"{Folder}/{file}", contents);
            } catch {
                LogError(file);
            }
        }

        /// <summary>
        /// Saves a file with a single ulong
        /// </summary>
        /// <param name="file">The file</param>
        /// <param name="content">The ulong</param>
        public static void WriteUlong(string file, ulong content) {
            try {
                using BinaryWriter bw = new(Open(file, FileMode.OpenOrCreate, FileAccess.Write));
                bw.Write(content);
            } catch {
                LogError(file);
            }
        }

        /// <summary>
        /// Loads a file with a single ulong
        /// </summary>
        /// <param name="file">The file</param>
        /// <param name="content">The ulong</param>
        /// <returns>True if successfully loaded, false otherwise</returns>
        public static bool TryReadUlong(string file, out ulong content) {
            try {
                if (!File.Exists($"{Folder}/{file}")) {
                    content = 0;
                    return true;
                }
                using BinaryReader br = new(Open(file, FileMode.Open, FileAccess.Read));
                content = br.ReadUInt64();
                return true;
            } catch {
                LogError(file);
                content = 0;
                return false;
            }
        }

        /// <summary>
        /// Deletes the specified file
        /// </summary>
        /// <param name="file">The file</param>
        public static void DeleteFile(string file) {
            try {
                File.Delete(file);
            } catch {
                LogError(file);
            }
        }

        /// <summary>
        /// Loads a file as a <see cref="FileStream"/>
        /// </summary>
        /// <param name="file">The file</param>
        /// <param name="fileMode">The mode to open the file in</param>
        /// <param name="fileAccess">Whether the file is going to be read or written</param>
        /// <returns>The <see cref="FileStream"/></returns>
        public static FileStream Open(string file, FileMode fileMode, FileAccess fileAccess) {
            try {
                return File.Open($"{Folder}/{file}", fileMode, fileAccess);
            } catch {
                LogError(file);
                return null;
            }
        }
    }
}
