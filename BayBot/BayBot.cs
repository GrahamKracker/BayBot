using BayBot.Core;
using Discord;
using Discord.WebSocket;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace BayBot;

//NOTE: Must be in debug mode to run on android
public static class BayBot {
    private static string _dataFolder;
    /// <summary>
    /// The folder where all the saved and loaded files exist
    /// </summary>
    public static string DataFolder {
        get => _dataFolder;
        set {
            _dataFolder = value;
            Logger.WriteLine("DataFolder = " + value);
        }
    }
    /// <summary>
    /// Whether the data folder has been found yet
    /// </summary>
    public static bool DataFolderSet { get; set; } = false;

    public static MauiApp CreateMauiApp() {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Run the Bot in a seperate task
        Task.Run(BotStartup);

        return builder.Build();
    }

    // The update function from BayBotCode
    private static Func<Task> Update { get; set; } = null;

    // The exit function from BayBotCode
    private static Action Exit { get; set; } = null;

    // BayBot
    private static DiscordSocketClient Bot { get; set; }

    // A timer that ticks every 1 second that will can Update from BayBotCode
    private static Timer UpdateTimer { get; } = new(1000);

    // Whether an update is currently running
    private static bool RunningUpdate { get; set; } = false;

    private static async Task BotStartup() {
        // Wait until the folder where all the files are is found
        while (!DataFolderSet) ;

        // Set up bot intents
        Bot = new(new DiscordSocketConfig() {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions | GatewayIntents.MessageContent
        });

        // Send bot logs to logger
        Bot.Log += msg => {
            Logger.WriteLine(msg.ToString());
            return Task.CompletedTask;
        };

        // Load the code when the bot is ready
        Bot.Ready += () => {
            LoadCode();
            Logger.WriteLine("Ready");
            return Task.CompletedTask;
        };

        // Log why the bot disconnected
        Bot.Disconnected += e => {
            Logger.WriteLine(e.Message);
            if (e.InnerException is not null)
                Logger.WriteLine(e.InnerException.Message);
            Logger.WriteLine(DateTime.Now.ToString());
            return Task.CompletedTask;
        };

        // Login and start bot
        await Bot.LoginAsync(TokenType.Bot, Sensitive.token);
        await Bot.StartAsync();

        // Set the update loop
        UpdateTimer.Elapsed += async (_, _) => {
            RunningUpdate = true;
            if (Update is not null)
                await Update.Invoke();
            RunningUpdate = false;
        };

        // This is necessary so that the bot doesn't stop
        await Task.Delay(-1);
    }

    /// <summary>
    /// Loads BayBotCode and gets it's functions through reflection
    /// </summary>
    public static void LoadCode() {
        try {
            // Lets BayBotCode exit and stop updates
            Exit?.Invoke();
            UpdateTimer.Stop();
            while (RunningUpdate) ; // Wait until currently running update is finished

            // Load the new BayBotCode
            Assembly codeAssemby = Assembly.Load(File.ReadAllBytes(DataFolder + "/BayBotCode.dll"));
            Type code = codeAssemby?.GetType("BayBot.BayBotCode");

            // Let BayBotCode initialize
            code?.GetMethod("Init")?.Invoke(null, new object[] { Bot, DataFolder });

            // Load the new exit function
            MethodInfo exit = code?.GetMethod("Exit");
            if (exit is not null)
                Exit = exit?.CreateDelegate<Action>();

            // Load the new update function
            MethodInfo update = code?.GetMethod("Update");
            if (update is not null)
                Update = update.CreateDelegate<Func<Task>>();

            // Start updates again
            UpdateTimer.Start();

            Logger.WriteLine("Loaded Code");
        } catch (Exception e) {
            Logger.WriteLine(e.ToString());
            if (e.InnerException is not null)
                Logger.WriteLine(e.InnerException.ToString());
        }
    }
}
