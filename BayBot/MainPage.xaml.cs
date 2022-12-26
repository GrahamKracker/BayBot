using BayBot.Core;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace BayBot;

public partial class MainPage : ContentPage {
    public MainPage() {
        InitializeComponent();

        // Receive text from Logger and scrolls to the bottom
        Output.Text = Logger.Output;
        Logger.OnLog += output => Output.Dispatcher.Dispatch(async () => {
            Output.Text = output;
            await Task.Delay(100);
            await Scroller.ScrollToAsync(Scroller.Content, ScrollToPosition.End, false);
        });
    }

    private void LoadCodeButtonClicked(object sender, EventArgs e) => BayBot.LoadCode();

    private void SleepButtonClicked(object sender, EventArgs e) => SleepCover.IsVisible = true;

    private void WakeButtonClicked(object sender, EventArgs e) => SleepCover.IsVisible = false;

    private void ClearButtonClicked(object sender, EventArgs e) => Logger.Clear();
}