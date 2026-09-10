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

            // v3.0.0 新增：构造一次主窗口即可验证 XAML、主题资源与类型颜色转换器可用，
            // 供发布前的冒烟检查使用。
            if (e.Args.Length > 0 && e.Args[0] == "--smoke-main")
            {
                MainWindow smokeMain = new MainWindow();
                smokeMain.Close();
                Shutdown();
                return;
            }

            MainWindow window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
    }
}
