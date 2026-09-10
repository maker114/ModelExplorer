using System;
using System.IO;
using SolidWorks.Interop.sldworks;

namespace ModelExplorer
{
    public enum StlExportFailure
    {
        None,
        Connect,
        OpenDocument,
        Export
    }

    /// <summary>一次 STL 导出的结果，含失败阶段，便于 CLI 映射为不同退出码。</summary>
    public sealed class StlExportOutcome
    {
        public bool Success { get; set; }
        public string StlPath { get; set; }
        public string Error { get; set; }
        public StlExportFailure Failure { get; set; }
    }

    /// <summary>
    /// GUI 与 CLI 共用的“连接 SolidWorks → 打开模型 → 导出 STL → 关闭”流程。
    /// 名称与公开方法保持与 v2.4.1 兼容（<see cref="Convert"/>、<see cref="SaveBambuProject"/>）。
    /// </summary>
    public static class SolidWorksConverter
    {
        public static event Action<string> Log;

        /// <summary>
        /// 唯一的导出实现：CLI 用它拿细分失败原因，GUI 用 <see cref="Convert"/> 包一层。
        /// </summary>
        public static StlExportOutcome TryExportStl(string modelPath, StlExportOptions options)
        {
            StlExportOutcome outcome = new StlExportOutcome();
            try
            {
                string stlPath = SolidWorksStlExporter.BuildTargetPath(modelPath, options);
                outcome.StlPath = stlPath;

                EmitLog("正在启动 SolidWorks");
                using (SolidWorksSession session = SolidWorksSession.Connect(
                    ConfigService.Load().SolidWorksPath,
                    EmitLog))
                {
                    EmitLog("SolidWorks 已就绪");

                    string error;
                    ModelDoc2 model = session.OpenDocument(modelPath, out error);
                    if (model == null)
                    {
                        outcome.Failure = StlExportFailure.OpenDocument;
                        outcome.Error = error;
                        return outcome;
                    }

                    try
                    {
                        if (!SolidWorksStlExporter.TryExportStl(session.App, model, stlPath, options, out error))
                        {
                            outcome.Failure = StlExportFailure.Export;
                            outcome.Error = error;
                            return outcome;
                        }
                    }
                    finally
                    {
                        session.CloseDocument(model);
                    }
                }

                outcome.Success = true;
                return outcome;
            }
            catch (Exception ex)
            {
                outcome.Failure = StlExportFailure.Connect;
                outcome.Error = ex.Message;
                return outcome;
            }
        }

        /// <summary>导出 STL，可选自动打开 Bambu Studio；失败时抛出异常（GUI 行为）。</summary>
        public static ConvertResult Convert(string modelPath, ConvertOptions options)
        {
            if (options == null)
            {
                options = new ConvertOptions();
            }

            StlExportOutcome outcome = TryExportStl(modelPath, options);
            if (!outcome.Success)
            {
                throw new InvalidOperationException(outcome.Error);
            }

            string modelDir = Path.GetDirectoryName(modelPath);
            string threeMfPath = Path.Combine(
                modelDir,
                Path.GetFileNameWithoutExtension(outcome.StlPath) + ".3mf");

            if (options.OpenBambu)
            {
                EmitLog("正在启动 Bambu Studio");
                BambuStudioLauncher.LaunchWithStl(ConfigService.Load().BambuPath, outcome.StlPath);
            }

            return new ConvertResult
            {
                StlPath = outcome.StlPath,
                ThreeMfPath = threeMfPath
            };
        }

        /// <summary>沿用 v2.4.1 的公开方法名，内部转发到共享实现；失败时抛异常。</summary>
        public static void SaveBambuProject(string threeMfPath)
        {
            string error;
            if (!BambuStudioLauncher.TrySaveProject(threeMfPath, out error))
            {
                throw new InvalidOperationException(error);
            }
        }

        private static void EmitLog(string message)
        {
            Action<string> handler = Log;
            if (handler != null)
            {
                handler(message);
            }
        }
    }
}
