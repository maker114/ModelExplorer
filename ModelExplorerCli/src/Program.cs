using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using ModelExplorerAddin;

namespace ModelExplorerCli
{
    internal static class Program
    {
        private const string StlFolderName = "STL文件";

        private static SldWorks _swApp;
        private static bool _launchedSw;

        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length >= 2 && args[0] == "--save-only")
                {
                    SaveBambuProject(args[1]);
                    return 0;
                }

                string modelPath = null;
                string outputPath = null;
                string threeMfOutputPath = null;
                bool keepHistory = false;
                bool noBambu = false;
                bool noThreeMf = false;
                bool autoThreeMf = false;

                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    if (arg == "--keep-history")
                    {
                        keepHistory = true;
                    }
                    else if (arg == "--no-bambu")
                    {
                        noBambu = true;
                    }
                    else if (arg == "--no-3mf")
                    {
                        noThreeMf = true;
                    }
                    else if (arg == "--auto-3mf")
                    {
                        autoThreeMf = true;
                    }
                    else if (arg == "--3mf-output")
                    {
                        if (i + 1 >= args.Length)
                        {
                            throw new ArgumentException("--3mf-output 后面需要路径");
                        }
                        i++;
                        threeMfOutputPath = args[i];
                    }
                    else if (arg == "--output")
                    {
                        if (i + 1 >= args.Length)
                        {
                            throw new ArgumentException("--output 后面需要路径");
                        }
                        i++;
                        outputPath = args[i];
                    }
                    else if (arg.StartsWith("--"))
                    {
                        throw new ArgumentException("未知参数：" + arg);
                    }
                    else if (modelPath == null)
                    {
                        modelPath = arg;
                    }
                    else
                    {
                        throw new ArgumentException("多余的参数：" + arg);
                    }
                }

                if (string.IsNullOrEmpty(modelPath) || !File.Exists(modelPath))
                {
                    Console.Error.WriteLine("请传入已存在的模型路径，例如：ModelExplorerCli.exe D:\\model.sldprt");
                    return 2;
                }

                AddinSettings settings = AddinSettings.Load();
                if (keepHistory)
                {
                    settings.KeepHistory = true;
                }

                ConnectToSolidWorks();
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
                        Console.Error.WriteLine("SolidWorks 打开模型失败，错误代码：" + openErrors + "，警告代码：" + openWarnings);
                        return 3;
                    }

                    try
                    {
                        string stlPath = outputPath;
                        if (string.IsNullOrEmpty(stlPath))
                        {
                            stlPath = BuildStlPath(modelPath, settings);
                        }

                        if (!ExportStl(model, stlPath, settings))
                        {
                            return 4;
                        }

                        Console.WriteLine(stlPath);

                        if (!noBambu)
                        {
                            string threeMfPath = BuildThreeMfPath(stlPath, modelPath, threeMfOutputPath);
                            if (autoThreeMf)
                            {
                                LaunchBambu(stlPath, threeMfPath, false, settings);
                                Console.WriteLine(threeMfPath);
                            }
                            else if (!noThreeMf)
                            {
                                LaunchBambu(stlPath, threeMfPath, true, settings);
                                Console.WriteLine("Bambu Studio 已打开。请选择打印机和耗材，然后回到本窗口按 Enter 保存 3MF。");
                                if (!Console.IsInputRedirected)
                                {
                                    Console.ReadLine();
                                }
                                SaveBambuProject(threeMfPath);
                            }
                            else
                            {
                                LaunchBambu(stlPath, threeMfPath, true, settings);
                            }
                        }

                        return 0;
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
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("转换失败：" + ex.Message);
                return 1;
            }
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
                Type swType = Type.GetTypeFromProgID("SldWorks.Application");
                if (swType == null)
                {
                    throw new InvalidOperationException("未找到 SolidWorks COM 注册，请确认已安装 SolidWorks 2022。");
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
            }
        }

        private static int GetDocumentType(string modelPath)
        {
            string extension = Path.GetExtension(modelPath).ToLowerInvariant();
            if (extension == ".sldprt")
            {
                return (int)swDocumentTypes_e.swDocPART;
            }
            if (extension == ".sldasm")
            {
                return (int)swDocumentTypes_e.swDocASSEMBLY;
            }

            throw new NotSupportedException("仅支持 .sldprt 和 .sldasm 文件。");
        }

        private static string BuildStlPath(string modelPath, AddinSettings settings)
        {
            string modelFolder = Path.GetDirectoryName(modelPath);
            string folder = Path.Combine(modelFolder, StlFolderName);
            Directory.CreateDirectory(folder);
            string baseName = Path.GetFileNameWithoutExtension(modelPath);
            string stlPath = Path.Combine(folder, baseName + ".stl");

            if (settings.KeepHistory && File.Exists(stlPath))
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string candidateName = baseName + "_" + stamp + ".stl";
                stlPath = Path.Combine(folder, candidateName);

                int index = 2;
                while (File.Exists(stlPath))
                {
                    candidateName = baseName + "_" + stamp + "_" + index.ToString("00") + ".stl";
                    stlPath = Path.Combine(folder, candidateName);
                    index++;
                }
            }

            return stlPath;
        }

        private static bool ExportStl(ModelDoc2 model, string stlPath, AddinSettings settings)
        {
            _swApp.SetUserPreferenceToggle(
                (int)swUserPreferenceToggle_e.swSTLShowInfoOnSave,
                false);
            _swApp.SetUserPreferenceToggle(
                (int)swUserPreferenceToggle_e.swSTLBinaryFormat,
                settings.BinaryStl);
            _swApp.SetUserPreferenceIntegerValue(
                (int)swUserPreferenceIntegerValue_e.swExportStlUnits,
                StlUnitValue(settings.StlUnits));
            _swApp.SetUserPreferenceIntegerValue(
                (int)swUserPreferenceIntegerValue_e.swSTLQuality,
                StlQualityValue(settings.StlQuality));

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
                Console.Error.WriteLine("STL 导出失败。错误代码：" + errors + "，警告代码：" + warnings);
                return false;
            }

            if (!File.Exists(stlPath))
            {
                Console.Error.WriteLine("STL 导出后未找到文件：" + stlPath);
                return false;
            }

            return true;
        }

        private static string BuildThreeMfPath(string stlPath, string modelPath, string threeMfOutputPath)
        {
            if (!string.IsNullOrEmpty(threeMfOutputPath))
            {
                return threeMfOutputPath;
            }

            string folder = Path.GetDirectoryName(modelPath);
            string baseName = Path.GetFileNameWithoutExtension(stlPath);
            return Path.Combine(folder, baseName + ".3mf");
        }

        private static void LaunchBambu(string stlPath, string threeMfPath, bool noThreeMf, AddinSettings settings)
        {
            string exePath = settings.BambuStudioPath;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                Console.Error.WriteLine("未找到 Bambu Studio，请在 %APPDATA%\\ModelExplorerAddin\\ModelExplorerAddin.config 中配置路径。STL 已生成：" + stlPath);
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = exePath;
            if (noThreeMf)
            {
                startInfo.Arguments = "\"" + stlPath + "\"";
            }
            else
            {
                startInfo.Arguments = "--export-3mf=\"" + threeMfPath + "\" \"" + stlPath + "\"";
            }
            startInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
            startInfo.UseShellExecute = false;
            Process.Start(startInfo);
        }

        private static void SaveBambuProject(string threeMfPath)
        {
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
                Console.Error.WriteLine("找不到 Bambu Studio 窗口，请手动另存为：" + threeMfPath);
                return;
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

            Console.WriteLine("已尝试保存 3MF：" + threeMfPath);
            Console.WriteLine("如果 Bambu Studio 弹出了保存窗口，请手动确认保存位置。");
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

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static int StlUnitValue(string units)
        {
            string normalized = (units ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "cm")
            {
                return (int)swLengthUnit_e.swCM;
            }
            if (normalized == "m" || normalized == "meter")
            {
                return (int)swLengthUnit_e.swMETER;
            }
            if (normalized == "in" || normalized == "inch")
            {
                return (int)swLengthUnit_e.swINCHES;
            }
            return (int)swLengthUnit_e.swMM;
        }

        private static int StlQualityValue(string quality)
        {
            string normalized = (quality ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "coarse")
            {
                return (int)swSTLQuality_e.swSTLQuality_Coarse;
            }
            if (normalized == "custom")
            {
                return (int)swSTLQuality_e.swSTLQuality_Custom;
            }
            return (int)swSTLQuality_e.swSTLQuality_Fine;
        }
    }
}
