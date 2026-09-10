using System;
using System.IO;

namespace ModelExplorer
{
    /// <summary>
    /// 分类文件夹名称的唯一来源。
    ///
    /// 修复 v2.4.1 的契约缺陷：CLI 原先写入 "STL文件"，而 GUI 扫描 / 整理 /
    /// 判定“是否已整理”使用 "STL文件夹"，导致 CLI 导出的 STL 在 GUI 中
    /// 被误判为 [未整理] 并被重复搬移。所有模块必须从此处取值。
    /// </summary>
    public static class WorkspaceNames
    {
        public const string StlFolderName = "STL文件夹";
        public const string ThreeMfFolderName = "3MF文件夹";

        /// <summary>目录名（不含路径）是否为分类文件夹。</summary>
        public static bool IsClassificationFolder(string directoryName)
        {
            if (string.IsNullOrEmpty(directoryName))
            {
                return false;
            }
            return string.Equals(directoryName, StlFolderName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(directoryName, ThreeMfFolderName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>给定完整目录路径，其自身是否为分类文件夹。</summary>
        public static bool IsClassificationPath(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                return false;
            }
            return IsClassificationFolder(Path.GetFileName(directoryPath.TrimEnd('\\', '/')));
        }

        /// <summary>判断路径字符串中是否出现分类文件夹层级（用于统计口径）。</summary>
        public static bool ContainsClassificationSegment(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            return path.IndexOf("\\" + StlFolderName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   path.IndexOf("\\" + ThreeMfFolderName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>返回按扩展名应当归类到的文件夹名；非 STL / 3MF 返回 null。</summary>
        public static string ClassificationFolderFor(string extension)
        {
            if (string.Equals(extension, ".stl", StringComparison.OrdinalIgnoreCase))
            {
                return StlFolderName;
            }
            if (string.Equals(extension, ".3mf", StringComparison.OrdinalIgnoreCase))
            {
                return ThreeMfFolderName;
            }
            return null;
        }
    }
}
