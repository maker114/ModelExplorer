namespace ModelExplorer
{
    /// <summary>
    /// STL 导出参数。GUI / CLI / SolidWorks 插件共用同一套选项与默认值，
    /// 原先三处各自实现导出流程导致参数无法统一。
    /// </summary>
    public class StlExportOptions
    {
        public StlExportOptions()
        {
            BinaryStl = true;
            StlUnits = "mm";
            StlQuality = "Fine";
            IntoClassificationFolder = true;
        }

        /// <summary>同名 STL 已存在时生成带时间戳的新文件，而不是覆盖。</summary>
        public bool KeepHistory { get; set; }

        public bool BinaryStl { get; set; }

        public string StlUnits { get; set; }

        public string StlQuality { get; set; }

        /// <summary>true = 输出到模型同目录的 STL文件夹；false = 输出到模型同目录（插件行为）。</summary>
        public bool IntoClassificationFolder { get; set; }

        /// <summary>显式指定输出文件（CLI --output），非空时优先于其它规则。</summary>
        public string ExplicitOutputPath { get; set; }

        public static StlExportOptions FromConfig(AppConfig config)
        {
            StlExportOptions options = new StlExportOptions();
            if (config != null)
            {
                options.KeepHistory = config.KeepHistory;
                options.BinaryStl = config.UseBinaryStl;
                options.StlUnits = config.StlUnits;
                options.StlQuality = config.StlQuality;
            }
            return options;
        }
    }

    /// <summary>GUI 导出选项：在 STL 参数之上增加“是否自动打开 Bambu Studio”。</summary>
    public class ConvertOptions : StlExportOptions
    {
        public ConvertOptions()
        {
            OpenBambu = true;
        }

        public bool OpenBambu { get; set; }
    }

    public class ConvertResult
    {
        public string StlPath { get; set; }
        public string ThreeMfPath { get; set; }
    }
}
