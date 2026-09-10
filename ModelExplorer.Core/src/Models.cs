using System.IO;

namespace ModelExplorer
{
    public enum ModelKind
    {
        Part,
        Assembly,
        Stl,
        ThreeMf
    }

    /// <summary>
    /// 扫描结果的领域模型。
    ///
    /// 修复 v2.4.1 的分层缺陷：原先本类带有 TypeBrush（WPF Brush）属性，使领域模型
    /// 反向依赖 WPF，ProjectScanner 无法脱离 UI 框架测试。颜色改由 GUI 层的
    /// KindBrushConverter 负责。
    /// </summary>
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

        /// <summary>无扩展名的主文件名。</summary>
        public string BaseName
        {
            get { return System.IO.Path.GetFileNameWithoutExtension(Name); }
        }
    }
}
