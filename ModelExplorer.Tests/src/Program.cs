using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using ModelExplorer;

namespace ModelExplorer.Tests
{
    /// <summary>
    /// 最小测试运行器（无外部依赖，离线可跑）。
    ///
    /// 覆盖 v3.0.0 重构中风险最高的部分：
    ///  1. AssemblyExportRule —— “装配体导出”两套语义
    ///  2. WorkspaceNames —— 分类文件夹命名的唯一来源
    ///  3. ProjectNamePlanner —— 工程名整理规则（README 中最复杂的一段）
    ///  4. ProjectScanner —— [未整理] / [未对应] 判定
    ///  5. FileOrganizer —— 归类整理与同名避让
    /// 全部用例在临时目录中进行，不写用户配置、不调用 SolidWorks。
    /// </summary>
    internal static class Program
    {
        private static int _passed;
        private static int _failed;
        private static readonly List<string> Failures = new List<string>();

        private static int Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            AssemblyExportRuleTests();
            WorkspaceNamesTests();
            ProjectNamePlannerTests();
            ProjectScannerTests();
            FileOrganizerTests();
            PathHelpersTests();
            ConfigDefaultTests();

            Console.WriteLine();
            Console.WriteLine("通过 " + _passed + " 项，失败 " + _failed + " 项。");
            foreach (string failure in Failures)
            {
                Console.WriteLine("  失败：" + failure);
            }
            return _failed == 0 ? 0 : 1;
        }

        // ---------------------------------------------------------------- 装配体导出判定

        private static void AssemblyExportRuleTests()
        {
            CheckTrue("含 ' - ' 视为装配体导出", AssemblyExportRule.IsAssemblyExport("Asm - Old_Part.stl"));
            CheckTrue("含 [装配体导出] 视为装配体导出", AssemblyExportRule.IsAssemblyExport("Model_Part[装配体导出].stl"));
            CheckFalse("普通 STL 不是装配体导出", AssemblyExportRule.IsAssemblyExport("Model_Part.stl"));

            CheckTrue("重命名口径只认 ' - '", AssemblyExportRule.HasRenameSeparator("Asm - Old_Part.stl"));
            CheckFalse(
                "已有标记但无 ' - ' 时不走重命名分支（防止剥掉标记）",
                AssemblyExportRule.HasRenameSeparator("Model_Part[装配体导出].stl"));

            CheckEqual("去除标记", "Model_Part.stl", AssemblyExportRule.RemoveMarker("Model_Part[装配体导出].stl"));
            CheckEqual("追加标记", "Model_Part[装配体导出].stl", AssemblyExportRule.AddMarker("Model_Part.stl"));
            CheckEqual("标记已存在时不重复追加", "Model_Part[装配体导出].stl",
                AssemblyExportRule.AddMarker("Model_Part[装配体导出].stl"));
        }

        // ---------------------------------------------------------------- 分类文件夹命名

        private static void WorkspaceNamesTests()
        {
            CheckEqual("STL 归类文件夹", "STL文件夹", WorkspaceNames.ClassificationFolderFor(".stl"));
            CheckEqual("3MF 归类文件夹", "3MF文件夹", WorkspaceNames.ClassificationFolderFor(".3MF"));
            CheckEqual("其它扩展名不归类", null, WorkspaceNames.ClassificationFolderFor(".sldprt"));

            CheckTrue("按目录名识别分类文件夹", WorkspaceNames.IsClassificationFolder("STL文件夹"));
            CheckTrue("按目录名识别分类文件夹(3MF)", WorkspaceNames.IsClassificationFolder("3MF文件夹"));
            CheckFalse("普通目录不是分类文件夹", WorkspaceNames.IsClassificationFolder("零件"));

            CheckTrue("路径中存在分类层级", WorkspaceNames.ContainsClassificationSegment(@"Model\STL文件夹"));
            CheckFalse("路径中无分类层级", WorkspaceNames.ContainsClassificationSegment(@"Model\零件"));

            // v2.4.1 契约缺陷的回归保护：CLI 曾使用 "STL文件"
            CheckFalse("CLI 旧名 'STL文件' 不再被当作分类文件夹",
                WorkspaceNames.IsClassificationFolder("STL文件"));
        }

        // ---------------------------------------------------------------- 工程名整理

        private static void ProjectNamePlannerTests()
        {
            // 1. 无工程名 → 添加名称
            WithProject(new[] { "零件A.sldprt" }, delegate(string root)
            {
                ProjectNameChange change = SinglePlan(root, "零件A.sldprt");
                CheckEqual("无前缀 → 补工程名", "Model_零件A.sldprt", change.TargetName);
                CheckEqual("归类为添加名称", "添加名称", change.Category);
                CheckEqual("类型为零件", "零件", change.FileType);
            });

            // 2. 旧工程名 → 修改
            WithProject(new[] { "旧工程_零件A.sldprt" }, delegate(string root)
            {
                ProjectNameChange change = SinglePlan(root, "旧工程_零件A.sldprt");
                CheckEqual("旧前缀 → 替换为当前工程名", "Model_零件A.sldprt", change.TargetName);
                CheckEqual("归类为修改", "修改", change.Category);
            });

            // 3. 前缀已正确 → 不产生计划
            WithProject(new[] { "Model_零件A.sldprt" }, delegate(string root)
            {
                CheckEqual("前缀正确时无改动", 0, Plan(root).Count);
            });

            // 4. 子工程目录 → 工程名_子工程名_文件名
            WithProject(new[] { @"Sub\Part.sldprt" }, delegate(string root)
            {
                ProjectNameChange change = SinglePlan(root, "Part.sldprt");
                CheckEqual("子工程目标名", "Model_Sub_Part.sldprt", change.TargetName);
                CheckEqual("记录子工程名", "Sub", change.SubProjectName);
            });

            // 5. 子工程内缺子工程前缀 → 补齐
            WithProject(new[] { @"Sub\Model_Part.sldprt" }, delegate(string root)
            {
                ProjectNameChange change = SinglePlan(root, "Model_Part.sldprt");
                CheckEqual("补齐子工程前缀", "Model_Sub_Part.sldprt", change.TargetName);
            });

            // 6. 装配体导出 STL → 主体 + [装配体导出]
            WithProject(new[] { "任意装配体名 - 任意旧工程_零件C.stl" }, delegate(string root)
            {
                ProjectNameChange change = SinglePlan(root, "任意装配体名 - 任意旧工程_零件C.stl");
                CheckEqual("装配体导出目标名", "Model_零件C[装配体导出].stl", change.TargetName);
                CheckEqual("类型为装配体导出", "装配体导出", change.FileType);
            });

            // 7. 子工程内的装配体导出
            WithProject(new[] { @"Sub\Asm - Old_零件C.stl" }, delegate(string root)
            {
                ProjectNameChange change = SinglePlan(root, "Asm - Old_零件C.stl");
                CheckEqual("子工程装配体导出目标名", "Model_Sub_零件C[装配体导出].stl", change.TargetName);
            });

            // 8. 回归保护：已带标记的装配体导出不得被改名（旧实现会剥掉标记）
            WithProject(new[] { "Model_零件C[装配体导出].stl" }, delegate(string root)
            {
                CheckEqual("已带标记的文件不再改动", 0, Plan(root).Count);
            });

            // 9. 分类文件夹中的文件不算子工程
            WithProject(new[] { @"STL文件夹\Model_Part.stl" }, delegate(string root)
            {
                CheckEqual("分类文件夹不视为子工程", 0, Plan(root).Count);
            });

            // 10. 排序：零件 → 装配体导出 → STL，且按子工程名优先
            WithProject(new[]
            {
                "Old_零件.sldprt",
                "Asm - Old_零件.stl",
                "Old_普通.stl",
                @"Sub\Old_子零件.sldprt"
            }, delegate(string root)
            {
                List<ProjectNameChange> plan = Plan(root);
                CheckEqual("计划条数", 4, plan.Count);
                CheckEqual("排序第 1 项为根目录零件", "Old_零件.sldprt", plan[0].OriginalName);
                CheckEqual("排序第 2 项为子工程零件", "Old_子零件.sldprt", plan[1].OriginalName);
                CheckEqual("排序第 3 项为装配体导出", "Asm - Old_零件.stl", plan[2].OriginalName);
                CheckEqual("排序第 4 项为普通 STL", "Old_普通.stl", plan[3].OriginalName);
            });
        }

        // ---------------------------------------------------------------- 扫描状态标记

        private static void ProjectScannerTests()
        {
            WithProject(new[]
            {
                "Model_Part.sldprt",
                @"STL文件夹\Model_Part.stl",
                @"STL文件夹\Model_Ghost.stl",
                @"STL文件夹\Asm - Model_Part.stl"
            }, delegate(string root)
            {
                List<ModelFile> files = ProjectScanner.Scan(root);

                ModelFile organized = Find(files, "Model_Part.stl");
                ModelFile orphan = Find(files, "Model_Ghost.stl");
                ModelFile assemblyExport = Find(files, "Asm - Model_Part.stl");

                CheckFalse("分类文件夹内的 STL 不算未整理", organized.IsUnorganized);
                CheckFalse("有对应零件 → 不是未对应", organized.IsOrphan);
                CheckTrue("分类文件夹内的孤儿 STL 标记未对应", orphan.IsOrphan);
                CheckTrue("装配体导出被识别", assemblyExport.IsAssemblyExport);
                CheckFalse("装配体导出豁免未对应检查", assemblyExport.IsOrphan);
                CheckEqual("类型标签", "STL", organized.TypeLabel);
                CheckEqual("扫描到 4 个文件", 4, files.Count);
            });

            WithProject(new[] { "Loose.stl" }, delegate(string root)
            {
                ModelFile loose = Find(ProjectScanner.Scan(root), "Loose.stl");
                CheckTrue("根目录下的 STL 标记为未整理", loose.IsUnorganized);
            });
        }

        // ---------------------------------------------------------------- 归类整理

        private static void FileOrganizerTests()
        {
            WithProject(new[] { "A.stl", "B.stl", "C.3mf", @"Sub\D.stl" }, delegate(string root)
            {
                OrganizeResult result = FileOrganizer.Organize(root, false);

                CheckEqual("移动条数", 4, result.Moves.Count);
                CheckEqual("STL 计数", 3, result.StlMoved);
                CheckEqual("3MF 计数", 1, result.ThreeMfMoved);
                CheckTrue("A.stl 已归入 STL文件夹",
                    File.Exists(Path.Combine(root, "STL文件夹", "A.stl")));
                CheckTrue("C.3mf 已归入 3MF文件夹",
                    File.Exists(Path.Combine(root, "3MF文件夹", "C.3mf")));
                CheckTrue("子目录 STL 也集中到根分类文件夹",
                    File.Exists(Path.Combine(root, "STL文件夹", "D.stl")));
                CheckEqual("来源→目标 分组数（根→STL、根→3MF、Sub→STL）", 3, result.Logs.Count);
            });

            // 同名避让：追加 _2 而不是覆盖
            WithProject(new[] { "A.stl", @"STL文件夹\A.stl" }, delegate(string root)
            {
                OrganizeResult result = FileOrganizer.Organize(root, false);
                CheckEqual("同名文件移动 1 条", 1, result.Moves.Count);
                CheckTrue("同名文件改名为 A_2.stl",
                    File.Exists(Path.Combine(root, "STL文件夹", "A_2.stl")));
                CheckTrue("原分类文件夹中的同名文件未被覆盖",
                    File.Exists(Path.Combine(root, "STL文件夹", "A.stl")));
            });

            // 按文件夹整理
            WithProject(new[] { @"Sub\D.stl" }, delegate(string root)
            {
                FileOrganizer.Organize(root, true);
                CheckTrue("按文件夹整理在子目录内建分类文件夹",
                    File.Exists(Path.Combine(root, "Sub", "STL文件夹", "D.stl")));
            });

            // 撤销：逆序回退
            WithProject(new[] { "A.stl", "B.stl" }, delegate(string root)
            {
                OrganizeResult result = FileOrganizer.Organize(root, false);
                UndoResult undo = FileMoveUndo.Restore(result.Moves, "原位置已存在文件");
                CheckEqual("撤销恢复 2 项", 2, undo.Restored);
                CheckEqual("撤销无失败", 0, undo.Failures.Count);
                CheckTrue("A.stl 回到根目录", File.Exists(Path.Combine(root, "A.stl")));
                CheckTrue("B.stl 回到根目录", File.Exists(Path.Combine(root, "B.stl")));
            });
        }

        // ---------------------------------------------------------------- 配置缺省值

        /// <summary>
        /// 回归保护：v2.4.1 及更早版本的 config.json 里没有 BinaryStl 字段。
        /// 若该字段不可空，缺失字段会被反序列化成 false，导致升级到 v3.0.0 后
        /// 主程序从“固定二进制”静默变成 ASCII STL（体积约为二进制的 5～10 倍）。
        /// </summary>
        private static void ConfigDefaultTests()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            // 模拟 v2.4.1 写出的配置：没有 BinaryStl / StlUnits / StlQuality
            AppConfig legacy = serializer.Deserialize<AppConfig>(
                "{\"LastDir\":\"D:\\\\model\",\"Theme\":\"暗夜蓝\",\"FontSize\":14,\"KeepHistory\":false}");
            CheckTrue("旧配置缺少 BinaryStl 字段时反序列化为 null", legacy.BinaryStl == null);
            CheckTrue("旧配置仍按二进制导出", legacy.UseBinaryStl);

            AppConfig explicitOff = serializer.Deserialize<AppConfig>("{\"BinaryStl\":false}");
            CheckFalse("显式 false 时关闭二进制", explicitOff.UseBinaryStl);

            AppConfig explicitOn = serializer.Deserialize<AppConfig>("{\"BinaryStl\":true}");
            CheckTrue("显式 true 时开启二进制", explicitOn.UseBinaryStl);

            AppConfig defaults = AppConfig.CreateDefault();
            CheckTrue("默认配置为二进制", defaults.UseBinaryStl);
            CheckEqual("默认单位", "mm", defaults.StlUnits);
            CheckEqual("默认质量", "Fine", defaults.StlQuality);

            AppConfig roundTrip = serializer.Deserialize<AppConfig>(serializer.Serialize(defaults));
            CheckTrue("默认配置序列化往返后仍为二进制", roundTrip.UseBinaryStl);
        }

        private static void PathHelpersTests()
        {
            CheckTrue("路径比较忽略大小写",
                PathHelpers.PathEquals(@"C:\Model\A.stl", @"c:\model\a.stl"));
            CheckEqual("根目录显示为“根目录”", "根目录",
                PathHelpers.RelativeDisplay(@"C:\Model", @"C:\Model"));
            CheckEqual("子路径显示相对路径", @"STL文件夹",
                PathHelpers.RelativeDisplay(@"C:\Model", @"C:\Model\STL文件夹"));
        }

        // ---------------------------------------------------------------- 测试基础设施

        private static List<ProjectNameChange> Plan(string root)
        {
            return ProjectNamePlanner.BuildPlan(root, ProjectScanner.Scan(root));
        }

        private static ProjectNameChange SinglePlan(string root, string originalName)
        {
            List<ProjectNameChange> plan = Plan(root);
            foreach (ProjectNameChange change in plan)
            {
                if (string.Equals(change.OriginalName, originalName, StringComparison.OrdinalIgnoreCase))
                {
                    return change;
                }
            }
            throw new InvalidOperationException("未生成计划：" + originalName);
        }

        private static ModelFile Find(List<ModelFile> files, string name)
        {
            foreach (ModelFile file in files)
            {
                if (string.Equals(file.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }
            }
            throw new InvalidOperationException("未扫描到文件：" + name);
        }

        /// <summary>在临时目录中建立名为 Model 的工程根，写入给定相对路径的文件后执行断言。</summary>
        private static void WithProject(string[] relativeFiles, Action<string> body)
        {
            string temp = Path.Combine(
                Path.GetTempPath(),
                "ModelExplorerTests",
                Guid.NewGuid().ToString("N"));
            string root = Path.Combine(temp, "Model");
            Directory.CreateDirectory(root);

            try
            {
                foreach (string relative in relativeFiles)
                {
                    string full = Path.Combine(root, relative);
                    string directory = Path.GetDirectoryName(full);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    File.WriteAllText(full, "test");
                }

                body(root);
            }
            finally
            {
                try
                {
                    Directory.Delete(temp, true);
                }
                catch
                {
                }
            }
        }

        private static void CheckEqual(string name, object expected, object actual)
        {
            string expectedText = expected == null ? "<null>" : expected.ToString();
            string actualText = actual == null ? "<null>" : actual.ToString();
            if (expectedText == actualText)
            {
                Pass(name);
            }
            else
            {
                Fail(name + "：期望 [" + expectedText + "]，实际 [" + actualText + "]");
            }
        }

        private static void CheckTrue(string name, bool actual)
        {
            CheckEqual(name, true, actual);
        }

        private static void CheckFalse(string name, bool actual)
        {
            CheckEqual(name, false, actual);
        }

        private static void Pass(string name)
        {
            _passed++;
            Console.WriteLine("  [通过] " + name);
        }

        private static void Fail(string message)
        {
            _failed++;
            Failures.Add(message);
            Console.WriteLine("  [失败] " + message);
        }
    }
}
