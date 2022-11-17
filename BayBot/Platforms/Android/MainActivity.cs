using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui;

namespace BayBot;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public class MainActivity : MauiAppCompatActivity {

    protected override void OnCreate(Bundle savedInstanceState) {
        base.OnCreate(savedInstanceState);

        // Set the data folder to the default files directory for the application
        BayBot.DataFolder = GetExternalFilesDir(null).AbsolutePath;
        BayBot.DataFolderSet = true;

        // Keep the screen on so that the application does not stop
        Window.AddFlags(WindowManagerFlags.KeepScreenOn);

        // Hide the system bars so that the application is effectively fullscreen
        HideSystemBars();
    }

    private void HideSystemBars() {
        WindowInsetsControllerCompat windowInsetsController = ViewCompat.GetWindowInsetsController(Window.DecorView);

        if (windowInsetsController is null)
            return;

        windowInsetsController.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
        windowInsetsController.Hide(WindowInsetsCompat.Type.SystemBars());
    }
}
