using System;
using System.Collections.Generic;

namespace ModelExplorer
{
    /// <summary>按文件夹汇总的一行统计。</summary>
    public class FolderStat
    {
        public string Folder { get; set; }
        public int Parts { get; set; }
        public int Assemblies { get; set; }
        public int Stls { get; set; }
        public int ThreeMfs { get; set; }

        public int Total
        {
            get { return Parts + Assemblies + Stls + ThreeMfs; }
        }
    }

    /// <summary>
    /// 详细统计的汇总逻辑。
    ///
    /// 从 MainWindow 抽到 Core，既让界面变薄，也让这段规则可以被测试覆盖——
    /// 它此前有一处静默错误：位于分类文件夹中的文件被整条跳过，
    /// 于是整理过的工程里 STL / 3MF 数量恒为 0（V3.1.2 修复）。
    /// </summary>
    public static class FolderStatistics
    {
        /// <summary>
        /// 按“归属文件夹”汇总各类文件数量，并按文件夹名排序。
        ///
        /// 位于 `STL文件夹` / `3MF文件夹` 中的文件不会单独成行，也不会被丢弃，
        /// 而是归到它们所属的上级文件夹：`工程\子工程\STL文件夹` 归入 `工程\子工程`。
        /// 这样“分类文件夹不单独成行”与“各类文件都要统计到”两个目标同时成立。
        /// </summary>
        public static List<FolderStat> Build(IEnumerable<ModelFile> files)
        {
            Dictionary<string, FolderStat> map =
                new Dictionary<string, FolderStat>(StringComparer.OrdinalIgnoreCase);

            if (files != null)
            {
                foreach (ModelFile model in files)
                {
                    string key = GetOwningFolder(model.Folder);
                    FolderStat stat;
                    if (!map.TryGetValue(key, out stat))
                    {
                        stat = new FolderStat { Folder = key };
                        map[key] = stat;
                    }

                    if (model.Kind == ModelKind.Part)
                    {
                        stat.Parts++;
                    }
                    else if (model.Kind == ModelKind.Assembly)
                    {
                        stat.Assemblies++;
                    }
                    else if (model.Kind == ModelKind.Stl)
                    {
                        stat.Stls++;
                    }
                    else if (model.Kind == ModelKind.ThreeMf)
                    {
                        stat.ThreeMfs++;
                    }
                }
            }

            List<FolderStat> list = new List<FolderStat>(map.Values);
            list.Sort(delegate(FolderStat a, FolderStat b)
            {
                return string.Compare(a.Folder, b.Folder, StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        /// <summary>
        /// 去掉显示路径末尾的分类文件夹层级，得到文件的归属文件夹。
        /// 例如 `工程\子工程\STL文件夹` → `工程\子工程`；根目录的文件返回“根目录”。
        /// </summary>
        public static string GetOwningFolder(string displayFolder)
        {
            if (string.IsNullOrEmpty(displayFolder))
            {
                return "根目录";
            }

            string[] parts = displayFolder.Split('\\');
            int end = parts.Length;
            while (end > 1 && WorkspaceNames.IsClassificationFolder(parts[end - 1]))
            {
                end--;
            }

            if (end == parts.Length)
            {
                return displayFolder;
            }
            return string.Join("\\", parts, 0, end);
        }
    }
}
