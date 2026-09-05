using System;
using System.Collections.Generic;
using System.IO;

namespace ModelExplorer
{
    public static class ProjectScanner
    {
        private static readonly HashSet<string> SkipDirs = new HashSet<string>
        {
            "__pycache__", ".git", ".svn", "node_modules"
        };

        public static List<ModelFile> Scan(string root)
        {
            List<ModelFile> entries = new List<ModelFile>();
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
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    ModelKind kind;
                    if (ext == ".sldprt")
                    {
                        kind = ModelKind.Part;
                    }
                    else if (ext == ".sldasm")
                    {
                        kind = ModelKind.Assembly;
                    }
                    else if (ext == ".stl")
                    {
                        kind = ModelKind.Stl;
                    }
                    else if (ext == ".3mf")
                    {
                        kind = ModelKind.ThreeMf;
                    }
                    else
                    {
                        continue;
                    }

                    FileInfo info = new FileInfo(file);
                    string rel = GetRelativePath(root, file);
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
                        TypeLabel = kind == ModelKind.Assembly ? "装配体" : kind == ModelKind.Stl ? "STL" : kind == ModelKind.ThreeMf ? "3MF" : "零件",
                        Size = info.Length,
                        SizeText = HumanSize(info.Length),
                        ModifiedText = info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }

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
                if (model.Kind == ModelKind.Stl)
                {
                    model.IsOrphan = !sourceNames.Contains(Path.GetFileNameWithoutExtension(model.Name));
                    model.IsUnorganized = !IsOrganizedStlPath(root, model.Path);
                }
            }

            HashSet<string> sourceFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ModelFile model in entries)
            {
                if ((model.Kind == ModelKind.Part || model.Kind == ModelKind.Assembly) && !string.IsNullOrEmpty(model.Folder))
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

        private static bool IsOrganizedStlPath(string root, string file)
        {
            string fileDir = Path.GetDirectoryName(Path.GetFullPath(file));
            string dirName = Path.GetFileName(fileDir);
            return string.Equals(dirName, "STL文件夹", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dirName, "3MF文件夹", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetRelativePath(string root, string file)
        {
            string rootFull = Path.GetFullPath(root).TrimEnd('\\') + "\\";
            string fileFull = Path.GetFullPath(file);
            if (fileFull.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            {
                return fileFull.Substring(rootFull.Length);
            }
            return fileFull;
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
