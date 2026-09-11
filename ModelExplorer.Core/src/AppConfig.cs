using System.Collections.Generic;

namespace ModelExplorer
{
    /// <summary>
    /// 全局配置（单一来源）。
    ///
    /// 修复 v2.4.1 的双配置缺陷：GUI 使用 %APPDATA%\ModelExplorer\config.json，
    /// 插件与 CLI 使用 %APPDATA%\ModelExplorerAddin\ModelExplorerAddin.config，
    /// 同一个“保留历史版本”开关在两处各存一份、互不生效。
    /// 现在三类程序共用本模型与同一个 config.json。
    /// </summary>
    public class AppConfig
    {
        // ---- 界面 / 扫描 ----
        public string LastDir { get; set; }
        public string Theme { get; set; }
        public int FontSize { get; set; }
        public bool OpenBambu { get; set; }
        public List<string> ProjectNameUnchecked { get; set; }

        // ---- 文件整理 ----
        /// <summary>
        /// 整理 STL / 3MF 时是否按文件夹整理：true = 在每个文件所在目录下分别使用
        /// `STL文件夹` / `3MF文件夹`；false = 集中到工程根目录的这两个文件夹。
        /// V3.0.7 起由设置窗口维护（此前是主界面上的开关，且不保存、每次启动都重置）。
        /// </summary>
        public bool OrganizeByFolder { get; set; }

        // ---- STL 导出（GUI 开关、插件与 CLI 共用） ----
        public bool KeepHistory { get; set; }

        /// <summary>
        /// 是否导出二进制 STL。
        ///
        /// 必须可空：v2.4.1 及更早版本的 config.json 里**没有**这个字段，
        /// 若用 bool，反序列化缺失字段会得到 false，导致升级后主程序从
        /// “固定二进制”静默变成 ASCII（文件体积约为二进制的 5～10 倍）。
        /// 缺失（null）表示沿用旧行为，即二进制。请统一用 <see cref="UseBinaryStl"/> 取值。
        /// </summary>
        public bool? BinaryStl { get; set; }

        public string StlUnits { get; set; }
        public string StlQuality { get; set; }

        /// <summary>实际生效的二进制开关：只有显式写成 false 才关闭。</summary>
        public bool UseBinaryStl
        {
            get { return BinaryStl != false; }
        }

        // ---- 外部程序路径 ----
        public string BambuPath { get; set; }
        public string SolidWorksPath { get; set; }

        public const string DefaultTheme = "终末地配色";
        public const int DefaultFontSize = 12;
        public const string DefaultStlUnits = "mm";
        public const string DefaultStlQuality = "Fine";

        public static AppConfig CreateDefault()
        {
            AppConfig config = new AppConfig();
            config.LastDir = "";
            config.Theme = DefaultTheme;
            config.FontSize = DefaultFontSize;
            config.OpenBambu = true;
            config.ProjectNameUnchecked = new List<string>();
            config.OrganizeByFolder = false;
            config.KeepHistory = false;
            config.BinaryStl = true;
            config.StlUnits = DefaultStlUnits;
            config.StlQuality = DefaultStlQuality;
            config.BambuPath = "";
            config.SolidWorksPath = "";
            return config;
        }
    }
}
