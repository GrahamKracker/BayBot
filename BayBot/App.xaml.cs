using Microsoft.Maui.Controls;

namespace BayBot {
    public partial class App : Application {
        public App() {
            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}