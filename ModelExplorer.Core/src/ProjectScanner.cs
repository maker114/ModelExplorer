using System;
using System.Collections.Generic;
using System.IO;

namespace ModelExplorer
{
    /// <summary>
    /// 工程目录递归扫描。纯文件系统逻辑，不依赖任何 UI 框架，可直接单元测试。
    /// </summary>
    public static class ProjectScanner
    {
        private static readonly HashSet<string> SkipDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "__pycache__", ".git", ".svn", "node_modules"
        };

        public static List<ModelFile> Scan(string root)
        {
            List<ModelFile> entries = new List<ModelFile>();
            if (string.IsNullOrEmpty(root))
            {
                return entries;
            }

            string rootName = Path.GetFileName(root.TrimEnd('\\'));
            if (string.IsNullOrEmpty(rootName))
            {
                rootName = root;
            }

            Stack<string> stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                string dir = stack.Pop();
                string[] subDirs;
                string[] files;

                try
                {
                    subDirs = Directory.GetDirectories(dir);
                    files = Directory.GetFiles(dir);
                }
                catch
                {
                    continue;
                }

                foreach (string subDir in subDirs)
                {
                    string name = Path.GetFileName(subDir);
                    if (!name.StartsWith(".") && !SkipDirs.Contains(name))
                    {
                        stack.Push(subDir);
                    }
                }

                foreach (string file in files)
                {
                    ModelKind kind;
                    if (!TryGetKind(file, out kind))
                    {
                        continue;
                    }

                    FileInfo info = new FileInfo(file);
                    string rel = PathHelpers.GetRelativePath(root, file);
                    string folder = Path.GetDirectoryName(rel);
                    string displayFolder = string.IsNullOrEmpty(folder) || folder == "."
                        ? rootName
                        : rootName + "\\" + folder.Replace("/", "\\");
                    entries.Add(new ModelFile
                    {
                        Name = Path.GetFileName(file),
                        Path = file,
                        RelativePath = rel,
                        Folder = displayFolder,
                        Kind = kind,
                        IsAssemblyExport = kind == ModelKind.Stl &&
                                          AssemblyExportRule.IsAssemblyExport(Path.GetFileName(file)),
                        TypeLabel = TypeLabelFor(kind),
                        Size = info.Length,
                        SizeText = HumanSize(info.Length),
                        ModifiedText = info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }

            MarkStlStatuses(root, entries);
            MarkFolderVisibility(entries);

            entries.Sort(delegate(ModelFile a, ModelFile b)
            {
                int typeCompare = a.Kind.CompareTo(b.Kind);
                if (typeCompare != 0)
                {
                    return typeCompare;
                }
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

            return entries;
        }

        public static bool TryGetKind(string file, out ModelKind kind)
        {
            string ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext == ".sldprt")
            {
                kind = ModelKind.Part;
                return true;
            }
            if (ext == ".sldasm")
            {
                kind = ModelKind.Assembly;
                return true;
            }
            if (ext == ".stl")
            {
                kind = ModelKind.Stl;
                return true;
            }
            if (ext == ".3mf")
            {
                kind = ModelKind.ThreeMf;
                return true;
            }
            kind = ModelKind.Part;
            return false;
        }

        public static string TypeLabelFor(ModelKind kind)
        {
            if (kind == ModelKind.Assembly)
            {
                return "装配体";
            }
            if (kind == ModelKind.Stl)
            {
                return "STL";
            }
            if (kind == ModelKind.ThreeMf)
            {
                return "3MF";
            }
            return "零件";
        }

        /// <summary>
        /// 依据 README 规则标记 [未整理] 与 [未对应]：
        ///  - [未整理]：STL 不在分类文件夹中。
        ///  - [未对应]：STL 主文件名在零件/装配体主文件名集合中不存在；装配体导出豁免。
        /// </summary>
        private static void MarkStlStatuses(string root, List<ModelFile> entries)
        {
            HashSet<string> sourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ModelFile model in entries)
            {
                if (model.Kind == ModelKind.Part || model.Kind == ModelKind.Assembly)
                {
                    sourceNames.Add(Path.GetFileNameWithoutExtension(model.Name));
                }
            }

            foreach (ModelFile model in entries)
            {
                if (model.Kind != ModelKind.Stl)
                {
                    continue;
                }

                string stlBaseName = Path.GetFileNameWithoutExtension(model.Name)
                    .Replace(AssemblyExportRule.Marker, "");
                model.IsOrphan = !model.IsAssemblyExport && !sourceNames.Contains(stlBaseName);
                model.IsUnorganized = !IsOrganizedStlPath(model.Path);
            }
        }

        private static void MarkFolderVisibility(List<ModelFile> entries)
        {
            HashSet<string> sourceFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ModelFile model in entries)
            {
                if ((model.Kind == ModelKind.Part || model.Kind == ModelKind.Assembly) &&
                    !string.IsNullOrEmpty(model.Folder))
                {
                    sourceFolders.Add(model.Folder);
                }
            }

            bool showFolder = sourceFolders.Count > 1;
            foreach (ModelFile model in entries)
            {
                if (model.Kind == ModelKind.Part || model.Kind == ModelKind.Assembly)
                {
                    model.FolderVisible = showFolder;
                }
            }
        }

        private static bool IsOrganizedStlPath(string file)
        {
            string fileDir = Path.GetDirectoryName(Path.GetFullPath(file));
            return WorkspaceNames.IsClassificationFolder(Path.GetFileName(fileDir));
        }

        private static string HumanSize(long bytes)
        {
            double size = bytes;
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int unitIndex = 0;
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }
            if (unitIndex == 0)
            {
                return ((long)size) + " B";
            }
            return size.ToString("0.0") + " " + units[unitIndex];
        }
    }
}
