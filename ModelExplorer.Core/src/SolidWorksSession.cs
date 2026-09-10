using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace ModelExplorer
{
    /// <summary>
    /// SolidWorks COM 会话。原先 GUI / CLI / 插件各写一份连接、断开与打开文档逻辑，
    /// 唯一差异是“是否使用设置中的 SLDWORKS.exe 路径”，这里统一处理。
    /// </summary>
    public sealed class SolidWorksSession : IDisposable
    {
        private const int LaunchRetrySeconds = 30;
        private const int LaunchGraceMilliseconds = 3000;

        private SldWorks _app;
        private bool _launchedByUs;
        private bool _disposed;
        private readonly Action<string> _log;

        private SolidWorksSession(SldWorks app, bool launchedByUs, Action<string> log)
        {
            _app = app;
            _launchedByUs = launchedByUs;
            _log = log;
        }

        public SldWorks App
        {
            get { return _app; }
        }

        public bool LaunchedByUs
        {
            get { return _launchedByUs; }
        }

        /// <summary>
        /// 优先接管已运行的 SolidWorks 实例；否则按 preferredExePath、内置候选路径
        /// 启动，最后回退到 ProgID 注册表项。日志通过 log 回调上报，避免静态事件泄漏。
        /// </summary>
        public static SolidWorksSession Connect(string preferredExePath, Action<string> log)
        {
            SldWorks app = TryGetActiveInstance();
            if (app != null)
            {
                return new SolidWorksSession(app, false, log);
            }

            string configuredPath = preferredExePath;
            if (string.IsNullOrEmpty(configuredPath) || !File.Exists(configuredPath))
            {
                configuredPath = DefaultExecutablePath();
            }

            Type swType = Type.GetTypeFromProgID("SldWorks.Application");
            if (swType == null && !string.IsNullOrEmpty(configuredPath) && File.Exists(configuredPath))
            {
                Emit(log, "通过设置路径启动 SolidWorks：" + configuredPath);
                TryStartSolidWorksProcess(configuredPath, log, ref swType);
            }

            if (swType == null)
            {
                throw new InvalidOperationException(
                    string.IsNullOrEmpty(configuredPath)
                        ? "未找到 SolidWorks COM 注册，请确认已安装 SolidWorks 2022。"
                        : "未找到 SolidWorks COM 注册，请确认路径正确并已安装 SolidWorks：" + configuredPath);
            }

            app = (SldWorks)Activator.CreateInstance(swType);
            Thread.Sleep(LaunchGraceMilliseconds);
            app.Visible = false;
            return new SolidWorksSession(app, true, log);
        }

        private static SldWorks TryGetActiveInstance()
        {
            try
            {
                return (SldWorks)Marshal.GetActiveObject("SldWorks.Application");
            }
            catch
            {
                return null;
            }
        }

        private static void TryStartSolidWorksProcess(string executablePath, Action<string> log, ref Type swType)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = executablePath;
                startInfo.WorkingDirectory = Path.GetDirectoryName(executablePath);
                startInfo.UseShellExecute = false;
                Process.Start(startInfo);

                for (int i = 0; i < LaunchRetrySeconds && swType == null; i++)
                {
                    Thread.Sleep(1000);
                    swType = Type.GetTypeFromProgID("SldWorks.Application");
                }
            }
            catch (Exception ex)
            {
                Emit(log, "启动 SolidWorks 失败：" + ex.Message);
            }
        }

        /// <summary>打开模型；失败时返回 null 并给出错误说明。</summary>
        public ModelDoc2 OpenDocument(string modelPath, out string error)
        {
            error = null;
            int docType = SolidWorksStlExporter.DocumentTypeValue(modelPath);
            int openErrors = 0;
            int openWarnings = 0;
            ModelDoc2 model = _app.OpenDoc6(
                modelPath,
                docType,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                "",
                ref openErrors,
                ref openWarnings);

            if (model == null)
            {
                error = "SolidWorks 打开模型失败，错误代码：" + openErrors + "，警告代码：" + openWarnings;
            }
            return model;
        }

        public void CloseDocument(ModelDoc2 model)
        {
            if (model == null || _app == null)
            {
                return;
            }

            try
            {
                string title = model.GetTitle();
                _app.CloseDoc(title);
            }
            catch
            {
            }
        }

        public static string DefaultExecutablePath()
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

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            if (_app == null)
            {
                return;
            }

            try
            {
                if (_launchedByUs)
                {
                    _app.ExitApp();
                }
            }
            catch
            {
            }
            finally
            {
                try
                {
                    Marshal.ReleaseComObject(_app);
                }
                catch
                {
                }
                _app = null;
                _launchedByUs = false;
            }
        }

        private static void Emit(Action<string> log, string message)
        {
            if (log != null)
            {
                log(message);
            }
        }
    }
}
