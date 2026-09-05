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
                int underscoreIndex = originalName.IndexOf('_');
                string currentProjectName = underscoreIndex > 0
                    ? originalName.Substring(0, underscoreIndex)
                    : "";
                string suffix = underscoreIndex > 0
                    ? originalName.Substring(underscoreIndex + 1)
                    : originalName;

                string targetName;
                string category;
                if (currentProjectName.Length == 0)
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
                    Selected = true
                });
            }

            changes.Sort(delegate(ProjectNameChange a, ProjectNameChange b)
            {
                int categoryCompare = string.Compare(a.Category, b.Category, StringComparison.Ordinal);
                if (categoryCompare != 0)
                {
                    return categoryCompare;
                }
                return string.Compare(a.OriginalName, b.OriginalName, StringComparison.OrdinalIgnoreCase);
            });
            return changes;
        }
    }
}
