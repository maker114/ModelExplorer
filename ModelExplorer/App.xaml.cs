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
                // 冒烟也按真实配置应用主题与毛玻璃，否则测不到用户实际会看到的界面
                AppConfig smokeConfig = ConfigService.Load();
                ThemeManager.Apply(smokeConfig);
                SettingsWindow smoke = new SettingsWindow(smokeConfig);
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
