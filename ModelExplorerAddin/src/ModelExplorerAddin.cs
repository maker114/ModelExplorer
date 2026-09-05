using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;

namespace ModelExplorerAddin
{
    [ComVisible(true)]
    [Guid("8A5C3F2B-6D7E-4B9A-9C1D-2E4F60718293")]
    [ProgId("ModelExplorerAddin.ModelExplorerAddin")]
    public class ModelExplorerAddin : ISwAddin
    {
        private const int CommandExport = 0;
        private const int CommandSettings = 1;

        private SldWorks _swApp;
        private int _cookie;
        private ICommandManager _commandManager;
        private ICommandGroup _commandGroup;

        public bool ConnectToSW(object ThisSW, int Cookie)
        {
            _swApp = (SldWorks)ThisSW;
            _cookie = Cookie;
            _commandManager = (ICommandManager)_swApp.GetCommandManager(_cookie);

            int errors = 0;
            _commandGroup = (ICommandGroup)_commandManager.CreateCommandGroup2(
                _cookie,
                "Model Explorer",
                "Export STL to Bambu Studio",
                "Export STL and open Bambu Studio",
                -1,
                false,
                ref errors);

            if (_commandGroup == null)
            {
                return false;
            }

            _commandGroup.AddCommandItem2(
                "Export to Bambu Studio",
                -1,
                "Export STL to Bambu Studio",
                "Export current model to STL and open it in Bambu Studio",
                -1,
                "OnExportToBambu",
                "",
                CommandExport,
                0);

            _commandGroup.AddCommandItem2(
                "Model Explorer Settings",
                -1,
                "Open Model Explorer settings",
                "Configure Bambu Studio path and STL output options",
                -1,
                "OnSettings",
                "",
                CommandSettings,
                0);

            _commandGroup.HasToolbar = true;
            _commandGroup.HasMenu = true;
            _commandGroup.Activate();

            AddinSettings.Load();
            return true;
        }

        public bool DisconnectFromSW()
        {
            try
            {
                if (_commandManager != null && _cookie != 0)
                {
                    _commandManager.RemoveCommandGroup(_cookie);
                }
            }
            catch
            {
            }
            finally
            {
                if (_swApp != null)
                {
                    Marshal.ReleaseComObject(_swApp);
                }
                _swApp = null;
                _commandManager = null;
                _commandGroup = null;
            }
            return true;
        }

        public void OnExportToBambu()
        {
            if (_swApp == null)
            {
                return;
            }

            ModelDoc2 model = _swApp.ActiveDoc as ModelDoc2;
            if (model == null)
            {
                ShowWarning("请先打开零件或装配体模型。");
                return;
            }

            string modelPath = model.GetPathName();
            if (string.IsNullOrEmpty(modelPath) || !File.Exists(modelPath))
            {
                ShowWarning("请先保存模型，STL 才能导出到模型同目录。");
                return;
            }

            AddinSettings settings = AddinSettings.Load();
            string stlPath = BuildStlPath(modelPath, settings);
            if (!ExportStl(model, stlPath, settings))
            {
                return;
            }

            LaunchBambu(stlPath, settings);
        }

        public void OnSettings()
        {
            AddinSettings settings = AddinSettings.Load();
            using (SettingsForm form = new SettingsForm(settings))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    form.Result.Save();
                }
            }
        }

        private static string BuildStlPath(string modelPath, AddinSettings settings)
        {
            string folder = Path.GetDirectoryName(modelPath);
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

        private bool ExportStl(ModelDoc2 model, string stlPath, AddinSettings settings)
        {
            try
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
                    ShowWarning("STL 导出失败。错误代码：" + errors + "，警告代码：" + warnings);
                    return false;
                }

                return File.Exists(stlPath);
            }
            catch (Exception ex)
            {
                ShowWarning("STL 导出异常：" + ex.Message);
                return false;
            }
        }

        private void LaunchBambu(string stlPath, AddinSettings settings)
        {
            string exePath = settings.BambuStudioPath;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                ShowWarning("未找到 Bambu Studio，请在“Model Explorer 设置”中配置路径。\r\nSTL 已导出：" + stlPath);
                return;
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = exePath;
                startInfo.Arguments = "\"" + stlPath + "\"";
                startInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
                startInfo.UseShellExecute = false;
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                ShowWarning("启动 Bambu Studio 失败：" + ex.Message);
            }
        }

        private void ShowWarning(string message)
        {
            if (_swApp != null)
            {
                _swApp.SendMsgToUser2(
                    message,
                    (int)swMessageBoxIcon_e.swMbWarning,
                    (int)swMessageBoxBtn_e.swMbOk);
            }
        }

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
