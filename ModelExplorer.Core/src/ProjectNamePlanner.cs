using System;
using System.Collections.Generic;
using System.IO;

namespace ModelExplorer
{
    public class ProjectNameChange
    {
        public string SourcePath { get; set; }
        public string OriginalName { get; set; }
        public string TargetPath { get; set; }
        public string TargetName { get; set; }
        public string RootName { get; set; }
        public string SubProjectName { get; set; }
        public string Category { get; set; }
        public bool Selected { get; set; }
        public string FileType { get; set; }
    }

    /// <summary>
    /// 工程名整理计划。纯函数，无 UI / 文件系统写入依赖，是本工程最复杂的业务规则，
    /// 由 ModelExplorer.Tests 覆盖。
    ///
    /// 规则来源（README「工程名整理」）：
    ///  - 根工程名 = 工程根文件夹名；根目录下的直接子文件夹（除分类文件夹）为子工程。
    ///  - 目标名 = 工程名[_子工程名]_主体。
    ///  - 零件 / 普通 STL：第一个 "_" 之前为旧工程名。
    ///  - 装配体导出 STL：包含 " - " 即按装配体处理，忽略 " - " 之前内容，
    ///    取其后第一个 "_" 之后为主体，并在扩展名前追加 [装配体导出]。
    /// </summary>
    public static class ProjectNamePlanner
    {
        public static List<ProjectNameChange> BuildPlan(string root, IEnumerable<ModelFile> files)
        {
            List<ProjectNameChange> changes = new List<ProjectNameChange>();
            string rootName = Path.GetFileName(root.TrimEnd('\\'));
            if (string.IsNullOrEmpty(rootName) || files == null)
            {
                return changes;
            }

            foreach (ModelFile model in files)
            {
                if (model.Kind != ModelKind.Part && model.Kind != ModelKind.Stl)
                {
                    continue;
                }
                if (string.IsNullOrEmpty(model.Path) || !File.Exists(model.Path))
                {
                    continue;
                }

                string sourcePath = model.Path;
                string originalName = Path.GetFileName(sourcePath);
                string subProjectName = GetSubProjectName(model.RelativePath);
                string fileType = model.Kind == ModelKind.Part ? "零件" : "STL";
                bool assemblyExport = model.Kind == ModelKind.Stl &&
                                      AssemblyExportRule.HasRenameSeparator(originalName);

                string targetName;
                string category;
                if (assemblyExport)
                {
                    string body = ExtractAssemblyBody(originalName, rootName, subProjectName);
                    targetName = AssemblyExportRule.AddMarker(
                        BuildTargetFileName(rootName, subProjectName, body));
                    category = "修改";
                    fileType = "装配体导出";
                }
                else
                {
                    if (HasExpectedPrefix(originalName, rootName, subProjectName))
                    {
                        continue;
                    }

                    string body = GetRemainingBody(originalName, rootName, subProjectName);
                    targetName = BuildTargetFileName(rootName, subProjectName, body);
                    category = HasProjectPrefix(originalName) ? "修改" : "添加名称";
                }

                if (string.Equals(targetName, originalName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                changes.Add(new ProjectNameChange
                {
                    SourcePath = sourcePath,
                    OriginalName = originalName,
                    TargetPath = Path.Combine(Path.GetDirectoryName(sourcePath), targetName),
                    TargetName = targetName,
                    RootName = rootName,
                    SubProjectName = subProjectName,
                    Category = category,
                    Selected = true,
                    FileType = fileType
                });
            }

            changes.Sort(delegate(ProjectNameChange a, ProjectNameChange b)
            {
                int typeCompare = TypeOrder(a.FileType).CompareTo(TypeOrder(b.FileType));
                if (typeCompare != 0)
                {
                    return typeCompare;
                }
                int subProjectCompare = string.Compare(
                    a.SubProjectName ?? "",
                    b.SubProjectName ?? "",
                    StringComparison.OrdinalIgnoreCase);
                if (subProjectCompare != 0)
                {
                    return subProjectCompare;
                }
                int categoryCompare = string.Compare(a.Category, b.Category, StringComparison.Ordinal);
                if (categoryCompare != 0)
                {
                    return categoryCompare;
                }
                return string.Compare(a.OriginalName, b.OriginalName, StringComparison.OrdinalIgnoreCase);
            });
            return changes;
        }

        private static int TypeOrder(string fileType)
        {
            if (fileType == "零件")
            {
                return 0;
            }
            if (fileType == "装配体导出")
            {
                return 1;
            }
            return 2;
        }

        /// <summary>取装配体导出 STL 的主体部分（不含标记）。</summary>
        private static string ExtractAssemblyBody(string originalName, string rootName, string subProjectName)
        {
            int separator = originalName.LastIndexOf(AssemblyExportRule.Separator, StringComparison.Ordinal);
            string expectedPrefix = BuildExpectedPrefix(rootName, subProjectName);
            int expectedIndex = originalName.IndexOf(
                expectedPrefix + "_",
                separator + AssemblyExportRule.Separator.Length,
                StringComparison.OrdinalIgnoreCase);

            string body;
            if (expectedIndex >= separator + AssemblyExportRule.Separator.Length)
            {
                body = originalName.Substring(expectedIndex + expectedPrefix.Length + 1);
            }
            else
            {
                int bodyUnderscore = originalName.IndexOf('_', separator + AssemblyExportRule.Separator.Length);
                body = bodyUnderscore > separator + AssemblyExportRule.Separator.Length
                    ? originalName.Substring(bodyUnderscore + 1)
                    : originalName.Substring(separator + AssemblyExportRule.Separator.Length);
            }

            return AssemblyExportRule.RemoveMarker(body);
        }

        private static string GetSubProjectName(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return "";
            }

            string directory = Path.GetDirectoryName(relativePath.Replace('/', '\\'));
            if (string.IsNullOrEmpty(directory) || directory == ".")
            {
                return "";
            }

            string firstFolder = directory.Split('\\')[0];
            if (WorkspaceNames.IsClassificationFolder(firstFolder))
            {
                return "";
            }
            return firstFolder;
        }

        private static string BuildExpectedPrefix(string rootName, string subProjectName)
        {
            if (string.IsNullOrEmpty(subProjectName))
            {
                return rootName;
            }
            return rootName + "_" + subProjectName;
        }

        private static string BuildTargetFileName(string rootName, string subProjectName, string body)
        {
            return BuildExpectedPrefix(rootName, subProjectName) + "_" + body;
        }

        private static bool HasExpectedPrefix(string fileName, string rootName, string subProjectName)
        {
            int firstUnderscore = fileName.IndexOf('_');
            if (firstUnderscore <= 0)
            {
                return false;
            }
            string firstSegment = fileName.Substring(0, firstUnderscore);
            if (!string.Equals(firstSegment, rootName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (string.IsNullOrEmpty(subProjectName))
            {
                return true;
            }

            int secondUnderscore = fileName.IndexOf('_', firstUnderscore + 1);
            if (secondUnderscore <= firstUnderscore + 1)
            {
                return false;
            }
            string secondSegment = fileName.Substring(firstUnderscore + 1, secondUnderscore - firstUnderscore - 1);
            return string.Equals(secondSegment, subProjectName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasProjectPrefix(string fileName)
        {
            return fileName.IndexOf('_') > 0;
        }

        private static string GetRemainingBody(string fileName, string rootName, string subProjectName)
        {
            int firstUnderscore = fileName.IndexOf('_');
            if (firstUnderscore < 0)
            {
                return fileName;
            }

            string firstSegment = fileName.Substring(0, firstUnderscore);
            string remainder = fileName.Substring(firstUnderscore + 1);
            if (!string.IsNullOrEmpty(subProjectName) &&
                string.Equals(firstSegment, rootName, StringComparison.OrdinalIgnoreCase))
            {
                int secondUnderscore = remainder.IndexOf('_');
                if (secondUnderscore > 0)
                {
                    return remainder.Substring(secondUnderscore + 1);
                }
            }
            return remainder;
        }
    }
}
