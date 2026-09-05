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
        public string Category { get; set; }
        public bool Selected { get; set; }
        public string FileType { get; set; }
    }

    public static class ProjectNamePlanner
    {
        public static List<ProjectNameChange> BuildPlan(string root, IEnumerable<ModelFile> files)
        {
            List<ProjectNameChange> changes = new List<ProjectNameChange>();
            string rootName = Path.GetFileName(root.TrimEnd('\\'));
            if (string.IsNullOrEmpty(rootName))
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
                string fileType = model.Kind == ModelKind.Part ? "零件" : "STL";
                bool assemblyExport = model.Kind == ModelKind.Stl &&
                                      IsAssemblyExportStl(originalName);
                int underscoreIndex = originalName.IndexOf('_');
                string currentProjectName = underscoreIndex > 0
                    ? originalName.Substring(0, underscoreIndex)
                    : "";
                string suffix = underscoreIndex > 0
                    ? originalName.Substring(underscoreIndex + 1)
                    : originalName;

                string targetName;
                string category;
                if (assemblyExport)
                {
                    int separator = originalName.LastIndexOf(" - ", StringComparison.Ordinal);
                    int bodyUnderscore = originalName.IndexOf('_', separator + 3);
                    string body = bodyUnderscore > separator + 3
                        ? originalName.Substring(bodyUnderscore + 1)
                        : originalName.Substring(separator + 3);
                    body = RemoveAssemblyExportMarker(body);
                    targetName = AddAssemblyExportMarker(rootName + "_" + body);
                    category = "修改";
                    fileType = "装配体导出";
                }
                else if (currentProjectName.Length == 0)
                {
                    targetName = rootName + "_" + originalName;
                    category = "添加名称";
                }
                else if (!string.Equals(currentProjectName, rootName, StringComparison.OrdinalIgnoreCase))
                {
                    targetName = rootName + "_" + suffix;
                    category = "修改";
                }
                else
                {
                    continue;
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

        private static bool IsAssemblyExportStl(string fileName)
        {
            return fileName.IndexOf(" - ", StringComparison.Ordinal) > 0;
        }

        private static string RemoveAssemblyExportMarker(string fileName)
        {
            string marker = "[装配体导出]";
            int index = fileName.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0)
            {
                return fileName;
            }

            string extension = Path.GetExtension(fileName);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (nameWithoutExtension.EndsWith(marker, StringComparison.Ordinal))
            {
                return nameWithoutExtension.Substring(0, nameWithoutExtension.Length - marker.Length) + extension;
            }
            return fileName.Replace(marker, "");
        }

        private static string AddAssemblyExportMarker(string fileName)
        {
            string marker = "[装配体导出]";
            string extension = Path.GetExtension(fileName);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (nameWithoutExtension.EndsWith(marker, StringComparison.Ordinal))
            {
                return fileName;
            }
            return nameWithoutExtension + marker + extension;
        }
    }
}
