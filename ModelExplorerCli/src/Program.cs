using System;
using System.IO;
using ModelExplorer;

namespace ModelExplorerCli
{
    /// <summary>
    /// 命令行转换工具。
    ///
    /// v3.0.0 起所有业务逻辑（配置、STL 导出、Bambu Studio 启动、分类文件夹命名）
    /// 统一复用 ModelExplorer.Core，与 GUI、SolidWorks 插件共用同一份实现与同一份配置。
    ///
    /// 退出码保持与 v2.4.1 一致：
    ///   0 成功 / 1 异常 / 2 参数或模型路径无效 / 3 打开模型失败 / 4 导出 STL 失败
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length >= 2 && args[0] == "--save-only")
                {
                    return SaveBambuProject(args[1]);
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

                AppConfig config = ConfigService.Load();
                StlExportOptions options = StlExportOptions.FromConfig(config);
                options.IntoClassificationFolder = true; // 与 GUI 一致：模型目录\STL文件夹
                options.ExplicitOutputPath = outputPath;
                if (keepHistory)
                {
                    options.KeepHistory = true;
                }

                StlExportOutcome outcome = SolidWorksConverter.TryExportStl(modelPath, options);
                if (!outcome.Success)
                {
                    Console.Error.WriteLine(outcome.Error);
                    return outcome.Failure == StlExportFailure.OpenDocument ? 3 : 4;
                }

                Console.WriteLine(outcome.StlPath);

                if (noBambu)
                {
                    return 0;
                }

                string threeMfPath = ResolveThreeMfPath(modelPath, outcome.StlPath, threeMfOutputPath);
                string launchError;

                if (autoThreeMf)
                {
                    if (!BambuStudioLauncher.TryLaunch(config.BambuPath, outcome.StlPath, threeMfPath, out launchError))
                    {
                        Console.Error.WriteLine(launchError);
                    }
                    Console.WriteLine(threeMfPath);
                    return 0;
                }

                if (noThreeMf)
                {
                    if (!BambuStudioLauncher.TryLaunch(config.BambuPath, outcome.StlPath, null, out launchError))
                    {
                        Console.Error.WriteLine(launchError);
                    }
                    return 0;
                }

                if (!BambuStudioLauncher.TryLaunch(config.BambuPath, outcome.StlPath, null, out launchError))
                {
                    Console.Error.WriteLine(launchError);
                }
                Console.WriteLine("Bambu Studio 已打开。请选择打印机和耗材，然后回到本窗口按 Enter 保存 3MF。");
                if (!Console.IsInputRedirected)
                {
                    Console.ReadLine();
                }
                return SaveBambuProject(threeMfPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("转换失败：" + ex.Message);
                return 1;
            }
        }

        /// <summary>3MF 默认放在模型同级目录，文件名与 STL 同名（沿用 v2.4.1 规则）。</summary>
        private static string ResolveThreeMfPath(string modelPath, string stlPath, string explicitPath)
        {
            if (!string.IsNullOrEmpty(explicitPath))
            {
                return explicitPath;
            }
            string folder = Path.GetDirectoryName(modelPath);
            string baseName = Path.GetFileNameWithoutExtension(stlPath);
            return Path.Combine(folder, baseName + ".3mf");
        }

        private static int SaveBambuProject(string threeMfPath)
        {
            string error;
            if (!BambuStudioLauncher.TrySaveProject(threeMfPath, out error))
            {
                Console.Error.WriteLine(error);
                return 0; // 保持 v2.4.1 语义：自动保存失败不改变退出码
            }

            Console.WriteLine("已尝试保存 3MF：" + threeMfPath);
            Console.WriteLine("如果 Bambu Studio 弹出了保存窗口，请手动确认保存位置。");
            return 0;
        }
    }
}
