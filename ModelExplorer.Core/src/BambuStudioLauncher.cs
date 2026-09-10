using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ModelExplorer
{
    /// <summary>
    /// Bambu Studio 启动与 3MF 保存。
    ///
    /// 修复 v2.4.1 的重复实现：EscapeSendKeys / FindBambuWindow / 默认路径探测
    /// 原先在 GUI 与 CLI 中各有一份逐字相同的副本。
    /// </summary>
    public static class BambuStudioLauncher
    {
        private const string ProcessName = "bambu-studio";
        private const int WindowWaitSeconds = 30;

        public static string DefaultExecutablePath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] candidates =
            {
                @"E:\Bambu Studio\bambu-studio.exe",
                Path.Combine(localAppData, "Programs", "Bambu Studio", "bambu-studio.exe"),
                @"C:\Program Files\Bambu Studio\bambu-studio.exe",
                @"C:\Program Files (x86)\Bambu Studio\bambu-studio.exe"
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            return candidates[0];
        }

        /// <summary>配置路径有效则使用配置，否则回退到候选路径。</summary>
        public static string ResolveExecutable(string configuredPath)
        {
            if (!string.IsNullOrEmpty(configuredPath) && File.Exists(configuredPath))
            {
                return configuredPath;
            }
            return DefaultExecutablePath();
        }

        /// <summary>仅打开 STL。找不到可执行文件时抛出，保留 GUI 原有语义。</summary>
        public static void LaunchWithStl(string configuredPath, string stlPath)
        {
            string exePath = ResolveExecutable(configuredPath);
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                throw new InvalidOperationException("未找到 Bambu Studio。STL 已生成：" + stlPath);
            }

            StartProcess(exePath, "\"" + stlPath + "\"");
        }

        /// <summary>
        /// 打开 STL 并可直接导出 3MF（CLI --auto-3mf）。失败时返回 false 并给出原因。
        /// </summary>
        public static bool TryLaunch(string configuredPath, string stlPath, string threeMfPath, out string error)
        {
            error = null;
            string exePath = ResolveExecutable(configuredPath);
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                error = "未找到 Bambu Studio，请在设置中配置路径。STL 已生成：" + stlPath;
                return false;
            }

            string arguments = string.IsNullOrEmpty(threeMfPath)
                ? "\"" + stlPath + "\""
                : "--export-3mf=\"" + threeMfPath + "\" \"" + stlPath + "\"";
            StartProcess(exePath, arguments);
            return true;
        }

        private static void StartProcess(string exePath, string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = exePath;
            startInfo.Arguments = arguments;
            startInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
            startInfo.UseShellExecute = false;
            Process.Start(startInfo);
        }

        /// <summary>
        /// 通过前台窗口 + SendKeys 触发 Bambu Studio 的另存为流程。
        /// 这是已知脆弱的自动化手段（README 亦有说明），集中在此处便于将来替换。
        /// </summary>
        public static bool TrySaveProject(string threeMfPath, out string error)
        {
            error = null;
            IntPtr window = IntPtr.Zero;
            for (int i = 0; i < WindowWaitSeconds; i++)
            {
                window = FindWindow();
                if (window != IntPtr.Zero)
                {
                    break;
                }
                Thread.Sleep(1000);
            }

            if (window == IntPtr.Zero)
            {
                error = "找不到 Bambu Studio 窗口，请手动另存为：" + threeMfPath;
                return false;
            }

            SetForegroundWindow(window);
            Thread.Sleep(800);
            SendKeys.SendWait("^s");
            Thread.Sleep(1500);
            SendKeys.SendWait("^a");
            SendKeys.SendWait(EscapeSendKeys(threeMfPath));
            SendKeys.SendWait("{ENTER}");
            Thread.Sleep(1200);
            SendKeys.SendWait("{ENTER}");
            return true;
        }

        public static IntPtr FindWindow()
        {
            Process[] processes = Process.GetProcessesByName(ProcessName);
            foreach (Process process in processes)
            {
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    return process.MainWindowHandle;
                }
            }
            return IntPtr.Zero;
        }

        public static string EscapeSendKeys(string text)
        {
            if (text == null)
            {
                return "";
            }
            text = text.Replace("{", "{{}");
            text = text.Replace("}", "{}}");
            text = text.Replace("+", "{+}");
            text = text.Replace("^", "{^}");
            text = text.Replace("%", "{%}");
            text = text.Replace("~", "{~}");
            text = text.Replace("(", "{(}");
            text = text.Replace(")", "{)}");
            return text;
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
