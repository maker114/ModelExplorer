using System;
using System.Windows.Media;

namespace ModelExplorer
{
    public enum ModelKind
    {
        Part,
        Assembly,
        Stl,
        ThreeMf
    }

    public class ModelFile
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string RelativePath { get; set; }
        public string Folder { get; set; }
        public bool FolderVisible { get; set; }
        public string TypeLabel { get; set; }
        public ModelKind Kind { get; set; }
        public long Size { get; set; }
        public string SizeText { get; set; }
        public string ModifiedText { get; set; }
        public bool IsOrphan { get; set; }
        public bool IsUnorganized { get; set; }
        public bool IsAssemblyExport { get; set; }

        public string MatchText
        {
            get
            {
                string result = "";
                if (IsUnorganized)
                {
                    result += " [未整理]";
                }
                if (IsOrphan)
                {
                    result += " [未对应]";
                }
                return result;
            }
        }

        public string UnorganizedText
        {
            get { return IsUnorganized ? "[未整理]" : ""; }
        }

        public string OrphanText
        {
            get { return IsOrphan ? "[未对应]" : ""; }
        }

        public string FolderText
        {
            get { return "归属文件夹：" + Folder; }
        }

        public Brush TypeBrush
        {
            get
            {
                if (Kind == ModelKind.Assembly)
                {
                    return ThemeManager.Current.AssemblyBrush;
                }
                if (Kind == ModelKind.Stl)
                {
                    return ThemeManager.Current.StlBrush;
                }
                if (Kind == ModelKind.ThreeMf)
                {
                    return ThemeManager.Current.AssemblyBrush;
                }
                return ThemeManager.Current.PartBrush;
            }
        }
    }
}
