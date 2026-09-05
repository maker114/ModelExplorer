using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace ModelExplorer
{
    public class ConvertOptions
    {
        public bool KeepHistory { get; set; }
        public bool OpenBambu { get; set; }
    }

    public class ConvertResult
    {
        public string StlPath { get; set; }
        public string ThreeMfPath { get; set; }
    }

    public static class SolidWorksConverter
    {
        public static event Action<string> Log;

        private static SldWorks _swApp;
        private static bool _launchedSw;

        public static ConvertResult Convert(string modelPath, ConvertOptions options)
        {
            ConvertResult result = new ConvertResult();
            string modelDir = Path.GetDirectoryName(modelPath);
            string baseName = Path.GetFileNameWithoutExtension(modelPath);
            string stlDir = Path.Combine(modelDir, "STL文件夹");
            Directory.CreateDirectory(stlDir);

            string stlPath = Path.Combine(stlDir, baseName + ".stl");
            if (options.KeepHistory && File.Exists(stlPath))
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                stlPath = Path.Combine(stlDir, baseName + "_" + stamp + ".stl");
                int index = 2;
                while (File.Exists(stlPath))
                {
                    stlPath = Path.Combine(stlDir, baseName + "_" + stamp + "_" + index.ToString("00") + ".stl");
                    index++;
                }
            }

            string threeMfPath = Path.Combine(
                modelDir,
                Path.GetFileNameWithoutExtension(stlPath) + ".3mf");

            EmitLog("正在启动 SolidWorks");
            ConnectToSolidWorks();
            EmitLog("SolidWorks 已就绪");
            try
            {
                int docType = GetDocumentType(modelPath);
                int openErrors = 0;
                int openWarnings = 0;
                ModelDoc2 model = _swApp.OpenDoc6(
                    modelPath,
                    docType,
                    (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                    "",
                    ref openErrors,
                    ref openWarnings);

                if (model == null)
                {
                    throw new InvalidOperationException(
                        "SolidWorks 打开模型失败，错误代码：" + openErrors + "，警告代码：" + openWarnings);
                }

                try
                {
                    ExportStl(model, stlPath);
                }
                finally
                {
                    string title = model.GetTitle();
                    _swApp.CloseDoc(title);
                }
            }
            finally
            {
                DisconnectFromSolidWorks();
            }

            if (options.OpenBambu)
            {
                EmitLog("正在启动 Bambu Studio");
                LaunchBambu(stlPath);
            }

            result.StlPath = stlPath;
            result.ThreeMfPath = threeMfPath;
            return result;
        }

        public static void SaveBambuProject(string threeMfPath)
        {
            EmitLog("正在向 Bambu Studio 发送保存 3MF 指令");
            IntPtr window = IntPtr.Zero;
            for (int i = 0; i < 30; i++)
            {
                window = FindBambuWindow();
                if (window != IntPtr.Zero)
                {
                    break;
                }
                Thread.Sleep(1000);
            }

            if (window == IntPtr.Zero)
            {
                throw new InvalidOperationException("找不到 Bambu Studio 窗口，请手动另存为：" + threeMfPath);
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
        }

        private static void ConnectToSolidWorks()
        {
            try
            {
                _swApp = (SldWorks)Marshal.GetActiveObject("SldWorks.Application");
            }
            catch
            {
            }

            if (_swApp == null)
            {
                string configuredPath = ConfigService.Load().SolidWorksPath;
                if (string.IsNullOrEmpty(configuredPath) || !File.Exists(configuredPath))
                {
                    configuredPath = DefaultSolidWorksPath();
                }

                Type swType = Type.GetTypeFromProgID("SldWorks.Application");
                if (swType == null && !string.IsNullOrEmpty(configuredPath) && File.Exists(configuredPath))
                {
                    EmitLog("通过设置路径启动 SolidWorks：" + configuredPath);
                    try
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = configuredPath;
                        startInfo.WorkingDirectory = Path.GetDirectoryName(configuredPath);
                        startInfo.UseShellExecute = false;
                        Process.Start(startInfo);

                        for (int i = 0; i < 30 && swType == null; i++)
                        {
                            Thread.Sleep(1000);
                            swType = Type.GetTypeFromProgID("SldWorks.Application");
                        }
                    }
                    catch (Exception ex)
                    {
                        EmitLog("启动 SolidWorks 失败：" + ex.Message);
                    }
                }

                if (swType == null)
                {
                    throw new InvalidOperationException(
                        string.IsNullOrEmpty(configuredPath)
                            ? "未找到 SolidWorks COM 注册，请确认已安装 SolidWorks 2022。"
                            : "未找到 SolidWorks COM 注册，请确认路径正确并已安装 SolidWorks：" + configuredPath);
                }
                _swApp = (SldWorks)Activator.CreateInstance(swType);
                _launchedSw = true;
                Thread.Sleep(3000);
            }

            if (_launchedSw)
            {
                _swApp.Visible = false;
            }
        }

        private static string DefaultSolidWorksPath()
        {
            string[] candidates =
            {
                @"D:\SW2022\SOLIDWORKS\SLDWORKS.exe",
                @"C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\SLDWORKS.exe",
                @"C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\sldworks.exe",
                @"C:\Program Files (x86)\SOLIDWORKS Corp\SOLIDWORKS\SLDWORKS.exe"
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

        private static void DisconnectFromSolidWorks()
        {
            if (_swApp == null)
            {
                return;
            }

            try
            {
                if (_launchedSw)
                {
                    _swApp.ExitApp();
                }
            }
            catch
            {
            }
            finally
            {
                Marshal.ReleaseComObject(_swApp);
                _swApp = null;
                _launchedSw = false;
            }
        }

        private static int GetDocumentType(string modelPath)
        {
            string ext = Path.GetExtension(modelPath).ToLowerInvariant();
            if (ext == ".sldprt")
            {
                return (int)swDocumentTypes_e.swDocPART;
            }
            if (ext == ".sldasm")
            {
                return (int)swDocumentTypes_e.swDocASSEMBLY;
            }
            throw new NotSupportedException("仅支持 .sldprt 和 .sldasm 文件。");
        }

        private static void ExportStl(ModelDoc2 model, string stlPath)
        {
            _swApp.SetUserPreferenceToggle(
                (int)swUserPreferenceToggle_e.swSTLShowInfoOnSave,
                false);
            _swApp.SetUserPreferenceToggle(
                (int)swUserPreferenceToggle_e.swSTLBinaryFormat,
                true);
            _swApp.SetUserPreferenceIntegerValue(
                (int)swUserPreferenceIntegerValue_e.swExportStlUnits,
                (int)swLengthUnit_e.swMM);
            _swApp.SetUserPreferenceIntegerValue(
                (int)swUserPreferenceIntegerValue_e.swSTLQuality,
                (int)swSTLQuality_e.swSTLQuality_Fine);

            int errors = 0;
            int warnings = 0;
            bool ok = model.Extension.SaveAs3(
                stlPath,
                (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
                (int)swSaveAsOptions_e.swSaveAsOptions_Silent,
                null,
                null,
                ref errors,
                ref warnings);

            if (!ok || errors != 0)
            {
                throw new InvalidOperationException(
                    "STL 导出失败。错误代码：" + errors + "，警告代码：" + warnings);
            }

            if (!File.Exists(stlPath))
            {
                throw new InvalidOperationException("STL 导出后未找到文件：" + stlPath);
            }
        }

        private static void LaunchBambu(string stlPath)
        {
            string configuredPath = ConfigService.Load().BambuPath;
            string exePath = !string.IsNullOrEmpty(configuredPath) && File.Exists(configuredPath)
                ? configuredPath
                : DefaultBambuPath();
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                throw new InvalidOperationException("未找到 Bambu Studio。STL 已生成：" + stlPath);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = exePath;
            startInfo.Arguments = "\"" + stlPath + "\"";
            startInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
            startInfo.UseShellExecute = false;
            Process.Start(startInfo);
        }

        private static string DefaultBambuPath()
        {
            string[] candidates =
            {
                @"E:\Bambu Studio\bambu-studio.exe",
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "Bambu Studio", "bambu-studio.exe"),
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

        private static IntPtr FindBambuWindow()
        {
            Process[] processes = Process.GetProcessesByName("bambu-studio");
            foreach (Process process in processes)
            {
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    return process.MainWindowHandle;
                }
            }
            return IntPtr.Zero;
        }

        private static string EscapeSendKeys(string text)
        {
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

        private static void EmitLog(string message)
        {
            Action<string> handler = Log;
            if (handler != null)
            {
                handler(message);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
