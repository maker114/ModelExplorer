using System.Collections.Generic;

namespace ModelExplorer
{
    /// <summary>
    /// STL 质量下拉框的「显示名 ↔ 配置值」映射。
    ///
    /// V3.0.3 起界面显示中文（粗糙 / 精细），但 config.json、命令行与
    /// SolidWorks 枚举映射仍沿用英文标记 <c>Coarse</c> / <c>Fine</c>：
    /// 这样已有配置不需要迁移，CLI 与插件的行为也不受影响。
    /// 主程序设置窗口与插件设置窗体共用本映射，避免两处显示不一致。
    /// </summary>
    public static class StlQualityLabels
    {
        public const string Coarse = "粗糙";
        public const string Fine = "精细";

        /// <summary>下拉框条目（按从粗到细排列）。</summary>
        public static IList<string> DisplayNames
        {
            get { return new List<string> { Coarse, Fine }; }
        }

        /// <summary>配置值 → 下拉显示名。</summary>
        public static string ToDisplay(string quality)
        {
            return SolidWorksStlExporter.NormalizeQuality(quality) == "Coarse" ? Coarse : Fine;
        }

        /// <summary>下拉显示名 → 配置值；无法识别时回退默认质量。</summary>
        public static string ToToken(string displayName)
        {
            if (displayName == Coarse || displayName == "Coarse")
            {
                return "Coarse";
            }
            if (displayName == Fine || displayName == "Fine")
            {
                return "Fine";
            }
            return AppConfig.DefaultStlQuality;
        }
    }
}
