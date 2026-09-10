using System;
using System.IO;
using System.Runtime.InteropServices;
using ModelExplorer;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;

namespace ModelExplorerAddin
{
    /// <summary>
    /// SolidWorks 插件：把当前打开的模型一键导出为 STL，并交给 Bambu Studio 打开。
    ///
    /// v3.0.0 起导出流程与配置全部复用 ModelExplorer.Core，与 GUI / CLI 保持一致；
    /// 原先本文件内自带的一份 SetUserPreference + SaveAs3 + 单位质量映射已删除。
    /// </summary>
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

            AppConfig config = ConfigService.Load();
            StlExportOptions options = StlExportOptions.FromConfig(config);
            // 插件保持 v2.4.1 行为：STL 直接放在模型同目录，不建分类文件夹。
            options.IntoClassificationFolder = false;

            string stlPath = SolidWorksStlExporter.BuildTargetPath(modelPath, options);
            string error;
            if (!SolidWorksStlExporter.TryExportStl(_swApp, model, stlPath, options, out error))
            {
                ShowWarning(error);
                return;
            }

            LaunchBambu(stlPath, config);
        }

        public void OnSettings()
        {
            AppConfig config = ConfigService.Load();
            using (SettingsForm form = new SettingsForm(config))
            {
                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ConfigService.Save(form.Result);
                }
            }
        }

        private void LaunchBambu(string stlPath, AppConfig config)
        {
            string error;
            if (!BambuStudioLauncher.TryLaunch(config.BambuPath, stlPath, null, out error))
            {
                ShowWarning("未找到 Bambu Studio，请在“Model Explorer 设置”中配置路径。\r\n" +
                            "STL 已导出：" + stlPath);
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
    }
}
