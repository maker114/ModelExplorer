using System;
using System.Collections.Generic;
using System.IO;

namespace ModelExplorer
{
    /// <summary>一次文件移动（整理或工程名重命名），也用作撤销记录。</summary>
    public sealed class FileMove
    {
        public string Source { get; set; }
        public string Dest { get; set; }
    }

    /// <summary>按“来源文件夹 → 目标文件夹”聚合的整理日志条目（文件夹为相对显示名）。</summary>
    public sealed class OrganizeLogEntry
    {
        public string SourceFolder { get; set; }
        public string TargetFolder { get; set; }
        public int StlCount { get; set; }
        public int ThreeMfCount { get; set; }
    }

    public sealed class OrganizeResult
    {
        public OrganizeResult()
        {
            Moves = new List<FileMove>();
            CreatedFolders = new List<string>();
            DeletedFolders = new List<string>();
            Logs = new List<OrganizeLogEntry>();
        }

        public List<FileMove> Moves { get; private set; }
        public int StlMoved { get; set; }
        public int ThreeMfMoved { get; set; }
        public List<string> CreatedFolders { get; private set; }
        public List<string> DeletedFolders { get; private set; }
        public List<OrganizeLogEntry> Logs { get; private set; }
    }

    /// <summary>
    /// STL / 3MF 归类整理。
    ///
    /// 从 MainWindow 抽出的纯文件系统逻辑（原为 code-behind 内约 200 行的匿名委托块），
    /// 使整理规则可独立测试，也让 MainWindow 不再直接操作 undo 栈以外的文件系统细节。
    /// 行为与原实现逐条对齐：跳过分类文件夹、同名追加 _2/_3、删除空分类文件夹。
    /// </summary>
    public static class FileOrganizer
    {
        public static OrganizeResult Organize(string root, bool byFolder)
        {
            OrganizeResult result = new OrganizeResult();
            string stlRoot = Path.Combine(root, WorkspaceNames.StlFolderName);
            string threeMfRoot = Path.Combine(root, WorkspaceNames.ThreeMfFolderName);
            Directory.CreateDirectory(stlRoot);
            Directory.CreateDirectory(threeMfRoot);

            Dictionary<string, OrganizeLogEntry> logs =
                new Dictionary<string, OrganizeLogEntry>(StringComparer.OrdinalIgnoreCase);
            Stack<string> stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                string dir = stack.Pop();
                if (WorkspaceNames.IsClassificationPath(dir))
                {
                    continue;
                }

                string[] subDirs;
                try
                {
                    subDirs = Directory.GetDirectories(dir);
                }
                catch
                {
                    continue;
                }
                foreach (string subDir in subDirs)
                {
                    stack.Push(subDir);
                }

                string[] files;
                try
                {
                    files = Directory.GetFiles(dir);
                }
                catch
                {
                    continue;
                }

                foreach (string file in files)
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    string folderName = WorkspaceNames.ClassificationFolderFor(ext);
                    if (folderName == null)
                    {
                        continue;
                    }

                    string targetDir = byFolder
                        ? Path.Combine(Path.GetDirectoryName(file), folderName)
                        : (folderName == WorkspaceNames.StlFolderName ? stlRoot : threeMfRoot);

                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                        result.CreatedFolders.Add(PathHelpers.RelativeDisplay(root, targetDir));
                    }

                    string dest = MoveUnique(file, targetDir);
                    result.Moves.Add(new FileMove { Source = file, Dest = dest });
                    RecordMove(logs, root, file, dest, folderName == WorkspaceNames.StlFolderName);

                    if (folderName == WorkspaceNames.StlFolderName)
                    {
                        result.StlMoved++;
                    }
                    else
                    {
                        result.ThreeMfMoved++;
                    }
                }
            }

            result.DeletedFolders.AddRange(DeleteEmptyClassificationFolders(root));

            List<OrganizeLogEntry> ordered = new List<OrganizeLogEntry>(logs.Values);
            ordered.Sort(delegate(OrganizeLogEntry a, OrganizeLogEntry b)
            {
                return string.Compare(a.SourceFolder, b.SourceFolder, StringComparison.OrdinalIgnoreCase);
            });
            result.Logs.AddRange(ordered);
            return result;
        }

        /// <summary>移动到目标目录；同名文件自动追加 _2、_3 序号，绝不覆盖。</summary>
        public static string MoveUnique(string source, string targetDir)
        {
            string target = Path.Combine(targetDir, Path.GetFileName(source));
            int index = 2;
            while (File.Exists(target))
            {
                string name = Path.GetFileNameWithoutExtension(source);
                string ext = Path.GetExtension(source);
                target = Path.Combine(targetDir, name + "_" + index + ext);
                index++;
            }
            File.Move(source, target);
            return target;
        }

        public static List<string> DeleteEmptyClassificationFolders(string root)
        {
            List<string> deleted = new List<string>();
            Stack<string> stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                string dir = stack.Pop();
                if (WorkspaceNames.IsClassificationPath(dir))
                {
                    try
                    {
                        if (Directory.GetFileSystemEntries(dir).Length == 0)
                        {
                            Directory.Delete(dir);
                            deleted.Add(PathHelpers.RelativeDisplay(root, dir));
                            continue;
                        }
                    }
                    catch
                    {
                    }
                }

                string[] subDirs;
                try
                {
                    subDirs = Directory.GetDirectories(dir);
                }
                catch
                {
                    continue;
                }
                foreach (string subDir in subDirs)
                {
                    stack.Push(subDir);
                }
            }

            return deleted;
        }

        private static void RecordMove(
            Dictionary<string, OrganizeLogEntry> logs,
            string root,
            string sourceFile,
            string destFile,
            bool isStl)
        {
            string sourceDir = Path.GetDirectoryName(sourceFile);
            string targetDir = Path.GetDirectoryName(destFile);
            string key = sourceDir + "|" + targetDir;

            OrganizeLogEntry entry;
            if (!logs.TryGetValue(key, out entry))
            {
                entry = new OrganizeLogEntry
                {
                    SourceFolder = PathHelpers.RelativeDisplay(root, sourceDir),
                    TargetFolder = PathHelpers.RelativeDisplay(root, targetDir)
                };
                logs[key] = entry;
            }

            if (isStl)
            {
                entry.StlCount++;
            }
            else
            {
                entry.ThreeMfCount++;
            }
        }
    }
}
