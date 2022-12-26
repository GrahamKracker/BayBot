using System;
using System.Diagnostics;
using System.IO;

namespace BayBot.Updater {
    /// <summary>
    /// Used to send the new BayBotCode.dll, and resources that may exit, to the Android device. Harcoded because why not?
    /// </summary>
    public class BayBotUpdater {
        /// <summary>
        /// Sends the file to Android
        /// </summary>
        /// <param name="path">The path to the file to send</param>
        private static void SendData(string path) {
            // Gets the file name
            string fileName = Path.GetFileName(path);

            // Send to Android
            Process.Start("adb", $"-s PM1LHMA790101447 push {path} /storage/emulated/0/Android/data/com.baybot/files/{fileName}").WaitForExit();
        }

        public static void Main(string[] args) {
            // Send BayBotCode.dll
            SendData(@"D:\Documents\Programs\BayBot\BayBotCode\bin\Debug\net7.0\BayBotCode.dll");
            Console.WriteLine("Sent Code");

            // Send every resource
            if (Directory.Exists(@"D:\Documents\Programs\BayBot\BayBotCode\bin\Debug\net7.0\Resources")) {
                foreach (string path in Directory.GetFiles(@"D:\Documents\Programs\BayBot\BayBotCode\bin\Debug\net7.0\Resources", ".")) {
                    SendData(path);
                    Console.WriteLine($"Sent {Path.GetFileName(path)}");
                }
            }
        }
    }
}
