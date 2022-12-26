using System;
using System.Diagnostics;

namespace BayBot.Core;

/// <summary>
/// Used to put messages to the screen, used in both the Android app and BayBotCode
/// </summary>
public static class Logger {
    public static string Output { get; set; }

    /// <summary>
    /// Invoked when the output is changed. Passes the whole output as a parameter.
    /// </summary>
    public static event OnLog OnLog;

    /// <summary>
    /// Writes a string to the output
    /// </summary>
    /// <param name="message">The string to be put</param>
    public static void Write(string message) {
        if (message is not null) {
            Output += message;
#if DEBUG
            Debug.Write(message);
#endif
            OnLog?.Invoke(Output);
        }
    }

    /// <summary>
    /// Writes a string to the output with a newline after
    /// </summary>
    /// <param name="message">The string to be put</param>
    public static void WriteLine(string message) => Write(message + Environment.NewLine);

    /// <summary>
    /// Writes an object converted to a string to the output
    /// </summary>
    /// <param name="o">The object to be put</param>
    public static void Write(object o) => Write(o.ToString());

    /// <summary>
    /// Writes an object converted to a string to the output with a newline after
    /// </summary>
    /// <param name="o">The object to be put</param>
    public static void WriteLine(object o) => WriteLine(o.ToString());

    /// <summary>
    /// Clears the output
    /// </summary>
    public static void Clear() {
        Output = "";
        OnLog?.Invoke(Output);
    }
}

public delegate void OnLog(string output);
