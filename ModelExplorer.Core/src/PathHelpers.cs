using System;
using System.IO;

namespace ModelExplorer
{
    /// <summary>路径比较与相对显示工具，原先散落在 MainWindow、ProjectScanner 中。</summary>
    public static class PathHelpers
    {
        public static bool PathEquals(string first, string second)
        {
            return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetRelativePath(string root, string file)
        {
            string rootFull = Path.GetFullPath(root).TrimEnd('\\') + "\\";
            string fileFull = Path.GetFullPath(file);
            if (fileFull.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            {
                return fileFull.Substring(rootFull.Length);
            }
            return fileFull;
        }

        /// <summary>用于日志与整理报告的短路径显示，根目录本身显示为“根目录”。</summary>
        public static string RelativeDisplay(string root, string path)
        {
            string rootFull = Path.GetFullPath(root).TrimEnd('\\');
            string pathFull = Path.GetFullPath(path).TrimEnd('\\');
            if (string.Equals(pathFull, rootFull, StringComparison.OrdinalIgnoreCase))
            {
                return "根目录";
            }
            if (pathFull.StartsWith(rootFull + "\\", StringComparison.OrdinalIgnoreCase))
            {
                return pathFull.Substring(rootFull.Length + 1);
            }
            return pathFull;
        }
    }
}
