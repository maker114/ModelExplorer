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

        // ---- STL 导出（GUI 开关、插件与 CLI 共用） ----
        public bool KeepHistory { get; set; }
        public bool BinaryStl { get; set; }
        public string StlUnits { get; set; }
        public string StlQuality { get; set; }

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
