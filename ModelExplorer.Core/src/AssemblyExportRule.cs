using System;
using System.IO;

namespace ModelExplorer
{
    /// <summary>
    /// “装配体导出 STL”判定的唯一来源。
    ///
    /// 修复 v2.4.1 的规则散落问题：判定条件原先分别在 Models.cs、ProjectScanner.cs、
    /// ProjectNameService.cs 各写一遍，改一处必漏两处。
    ///
    /// 这里刻意保留两个谓词，因为它们承担的是**不同**的语义，不能合并：
    ///  - <see cref="IsAssemblyExport"/>：扫描与界面打标用。只要文件名包含 " - "
    ///    或已带 [装配体导出] 标记即成立，用于豁免 [未对应] 检查。
    ///  - <see cref="HasRenameSeparator"/>：重命名计划用。只认 " - "。已经带标记的
    ///    文件不能再走装配体分支，否则会把 [装配体导出] 标记剥掉（回归）。
    /// </summary>
    public static class AssemblyExportRule
    {
        public const string Marker = "[装配体导出]";
        public const string Separator = " - ";

        /// <summary>扫描 / 界面打标口径：包含分隔符或已带标记。</summary>
        public static bool IsAssemblyExport(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }
            return fileName.IndexOf(Separator, StringComparison.Ordinal) >= 0 ||
                   fileName.IndexOf(Marker, StringComparison.Ordinal) >= 0;
        }

        /// <summary>重命名计划口径：仅以分隔符为准。</summary>
        public static bool HasRenameSeparator(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }
            return fileName.IndexOf(Separator, StringComparison.Ordinal) > 0;
        }

        /// <summary>去掉扩展名之前的 [装配体导出] 标记。</summary>
        public static string RemoveMarker(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return fileName;
            }

            int index = fileName.IndexOf(Marker, StringComparison.Ordinal);
            if (index < 0)
            {
                return fileName;
            }

            string extension = Path.GetExtension(fileName);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (nameWithoutExtension.EndsWith(Marker, StringComparison.Ordinal))
            {
                return nameWithoutExtension.Substring(0, nameWithoutExtension.Length - Marker.Length) + extension;
            }
            return fileName.Replace(Marker, "");
        }

        /// <summary>在扩展名之前追加 [装配体导出] 标记（已存在则原样返回）。</summary>
        public static string AddMarker(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return fileName;
            }

            string extension = Path.GetExtension(fileName);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (nameWithoutExtension.EndsWith(Marker, StringComparison.Ordinal))
            {
                return fileName;
            }
            return nameWithoutExtension + Marker + extension;
        }
    }
}
