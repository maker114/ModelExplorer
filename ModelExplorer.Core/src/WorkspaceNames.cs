using System;
using System.IO;

namespace ModelExplorer
{
    /// <summary>
    /// 界面小图标（16×16 视窗、纯描边路径）的**唯一真源**。
    ///
    /// 设置窗在代码里直接引用这些常量（<c>UiFactory.IconPath</c>）；
    /// 主界面的分区标题写在 XAML 里、没法引用常量，所以 `MainWindow.xaml` 里把同样的
    /// 字符串又写了一遍——`MainWindowIconTests` 会逐条核对两边是否一致，
    /// 改了这里忘了改 XAML 就会测试失败，避免图标悄悄漂移。
    /// </summary>
    public static class AppIcons
    {
        // ---- 设置窗 ----
        public const string Sliders = "M 2,5 H 14 M 2,11 H 14 M 5.5,3.2 V 6.8 M 10.5,9.2 V 12.8";
        public const string Droplet = "M 8,1.9 C 8,1.9 3.5,7.4 3.5,10.2 A 4.5,4.5 0 0 0 12.5,10.2 C 12.5,7.4 8,1.9 8,1.9 Z";
        public const string Image = "M 2.2,3.6 H 13.8 V 12.4 H 2.2 Z M 2.2,10 L 6,6.6 L 8.6,9 L 10.6,7.4 L 13.8,10.2";
        public const string Letter = "M 3,13 L 6.4,3.4 L 9.8,13 M 4.4,9.6 H 8.4";
        public const string Window = "M 2.2,3.6 H 13.8 V 12.4 H 2.2 Z M 2.2,6.6 H 13.8";
        public const string Download = "M 8,2.2 V 9.8 M 4.6,6.6 L 8,10 L 11.4,6.6 M 2.6,13.4 H 13.4";
        public const string Swap = "M 2.6,5.6 H 12 M 9.4,3.2 L 11.8,5.6 L 9.4,8 M 13.4,10.4 H 4 M 6.6,8 L 4.2,10.4 L 6.6,12.8";
        public const string Folder = "M 2.2,4.6 H 6.4 L 7.8,6.6 H 13.8 V 12.4 H 2.2 Z";

        // ---- 主界面分区标题（与 MainWindow.xaml 里的 Data 必须逐字一致）----
        public const string Stats = "M 2.4,13.2 V 9 M 6.4,13.2 V 5.4 M 10.4,13.2 V 7.6 M 13.8,13.2 V 3.6";
        /// <summary>「工程名整理」：带书写线的名牌（改名）。V3.5.2 第二轮把原来的折线换掉——折线更像统计。</summary>
        public const string Rename = "M 2.2,3.4 H 13.8 V 12.6 H 2.2 Z M 4.6,6.4 H 10.4 M 4.6,9.4 H 8";
        public const string Log = "M 3.4,2.4 H 9.6 L 12.6,5.4 V 13.6 H 3.4 Z M 9.4,2.6 V 5.6 H 12.4 M 5.6,8.4 H 10.4 M 5.6,11.2 H 8.8";
    }

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
