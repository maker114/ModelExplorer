using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace ModelExplorer
{
    /// <summary>
    /// STL 导出的共享实现。
    ///
    /// 修复 v2.4.1 的三份重复实现：GUI(<c>SolidWorksConverter</c>)、
    /// CLI(<c>ModelExplorerCli.Program</c>)、插件(<c>ModelExplorerAddin</c>)
    /// 各自抄了一遍 SetUserPreference / SaveAs3 / KeepHistory 命名 / 单位质量映射，
    /// 修一处必漏两处。现在只保留此处一份。
    /// </summary>
    public static class SolidWorksStlExporter
    {
        /// <summary>按选项推导 STL 目标路径，并按需创建目标目录。</summary>
        public static string BuildTargetPath(string modelPath, StlExportOptions options)
        {
            if (options != null && !string.IsNullOrEmpty(options.ExplicitOutputPath))
            {
                string explicitDirectory = Path.GetDirectoryName(options.ExplicitOutputPath);
                if (!string.IsNullOrEmpty(explicitDirectory))
                {
                    Directory.CreateDirectory(explicitDirectory);
                }
                return options.ExplicitOutputPath;
            }

            string modelDir = Path.GetDirectoryName(modelPath);
            string baseName = Path.GetFileNameWithoutExtension(modelPath);
            string targetDir = options != null && !options.IntoClassificationFolder
                ? modelDir
                : Path.Combine(modelDir, WorkspaceNames.StlFolderName);
            Directory.CreateDirectory(targetDir);

            string stlPath = Path.Combine(targetDir, baseName + ".stl");
            if (options != null && options.KeepHistory && File.Exists(stlPath))
            {
                stlPath = AppendTimestamp(stlPath, baseName, targetDir);
            }
            return stlPath;
        }

        private static string AppendTimestamp(string existingPath, string baseName, string targetDir)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string stlPath = Path.Combine(targetDir, baseName + "_" + stamp + ".stl");
            int index = 2;
            while (File.Exists(stlPath))
            {
                stlPath = Path.Combine(targetDir, baseName + "_" + stamp + "_" + index.ToString("00") + ".stl");
                index++;
            }
            return stlPath;
        }

        /// <summary>按选项写入 SolidWorks 的 STL 导出偏好。</summary>
        public static void ApplyPreferences(SldWorks app, StlExportOptions options)
        {
            if (options == null)
            {
                options = new StlExportOptions();
            }

            app.SetUserPreferenceToggle(
                (int)swUserPreferenceToggle_e.swSTLShowInfoOnSave,
                false);
            app.SetUserPreferenceToggle(
                (int)swUserPreferenceToggle_e.swSTLBinaryFormat,
                options.BinaryStl);
            app.SetUserPreferenceIntegerValue(
                (int)swUserPreferenceIntegerValue_e.swExportStlUnits,
                StlUnitValue(options.StlUnits));
            app.SetUserPreferenceIntegerValue(
                (int)swUserPreferenceIntegerValue_e.swSTLQuality,
                StlQualityValue(options.StlQuality));
        }

        /// <summary>
        /// 应用偏好并导出。失败时返回 false 并给出可读原因，不抛异常，
        /// 便于插件的消息框与 CLI 的退出码分别处理。
        /// </summary>
        public static bool TryExportStl(SldWorks app, ModelDoc2 model, string stlPath, StlExportOptions options, out string error)
        {
            error = null;
            try
            {
                ApplyPreferences(app, options);

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
                    error = "STL 导出失败。错误代码：" + errors + "，警告代码：" + warnings;
                    return false;
                }

                if (!File.Exists(stlPath))
                {
                    error = "STL 导出后未找到文件：" + stlPath;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = "STL 导出异常：" + ex.Message;
                return false;
            }
        }

        public static int DocumentTypeValue(string modelPath)
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

        public static int StlUnitValue(string units)
        {
            string normalized = NormalizeUnits(units);
            if (normalized == "cm")
            {
                return (int)swLengthUnit_e.swCM;
            }
            if (normalized == "m")
            {
                return (int)swLengthUnit_e.swMETER;
            }
            if (normalized == "in")
            {
                return (int)swLengthUnit_e.swINCHES;
            }
            return (int)swLengthUnit_e.swMM;
        }

        public static int StlQualityValue(string quality)
        {
            string normalized = NormalizeQuality(quality);
            if (normalized == "Coarse")
            {
                return (int)swSTLQuality_e.swSTLQuality_Coarse;
            }
            if (normalized == "Custom")
            {
                return (int)swSTLQuality_e.swSTLQuality_Custom;
            }
            return (int)swSTLQuality_e.swSTLQuality_Fine;
        }

        public static string NormalizeUnits(string units)
        {
            string normalized = (units ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "cm")
            {
                return "cm";
            }
            if (normalized == "m" || normalized == "meter")
            {
                return "m";
            }
            if (normalized == "in" || normalized == "inch")
            {
                return "in";
            }
            return "mm";
        }

        public static string NormalizeQuality(string quality)
        {
            string normalized = (quality ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "coarse")
            {
                return "Coarse";
            }
            if (normalized == "custom")
            {
                return "Custom";
            }
            return "Fine";
        }
    }
}
