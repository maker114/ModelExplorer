using System.IO;
using System.Windows;

namespace ModelExplorer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length > 0 && e.Args[0] == "--smoke-settings")
            {
                SettingsWindow smoke = new SettingsWindow(ConfigService.Load());
                smoke.Close();
                Shutdown();
                return;
            }

            MainWindow window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
    }
}
