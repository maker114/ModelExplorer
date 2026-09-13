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

        // ---- 界面毛玻璃 ----
        /// <summary>
        /// 是否启用毛玻璃界面（半透明玻璃面板 + 极光背景）。
        ///
        /// 必须可空，理由同 BinaryStl：v3.1.3 及更早版本的 config.json 里没有这个字段，
        /// 若用 bool，反序列化缺失字段会得到 false，老用户升级后会被静默关掉毛玻璃。
        /// 缺失（null）表示沿用新版本的默认观感，即开启。请统一用 <see cref="UseGlass"/> 取值。
        /// </summary>
        public bool? Glass { get; set; }

        /// <summary>
        /// 旧版（3.2.0 / 3.3.0）的三档强度：0 = 轻柔，1 = 标准，2 = 浓郁。
        /// 3.4.0 起界面改成「模糊 + 透明度」两个滑杆，这个字段只用于把旧配置的观感迁移过来，
        /// 不再有新界面写它。缺失（null）时按 <see cref="DefaultGlassStrength"/> 处理。
        /// </summary>
        public int? GlassStrength { get; set; }

        /// <summary>毛玻璃模糊（像素，0～60）：作用在整层背景上（极光与自定义背景图都算）。</summary>
        public int? GlassBlur { get; set; }

        /// <summary>
        /// 毛玻璃透明度（百分比，0～100）。0 = 面板完全不透明（背景被挡住），
        /// 100 = 面板完全透明，只剩边框。旧配置缺失时由三档强度换算。
        /// </summary>
        public int? GlassOpacity { get; set; }

        // ---- 背景图 ----
        /// <summary>自定义背景图路径，空 = 只用极光背景。六个窗口共用同一张图。</summary>
        public string BackgroundImage { get; set; }

        /// <summary>背景图适配方式：cover / fill / center / stretch，见 <see cref="BackgroundFitValue"/>。</summary>
        public string BackgroundFit { get; set; }

        /// <summary>壁纸模糊（像素，0～40）：只在设置了背景图时生效。</summary>
        public int BackgroundBlur { get; set; }

        /// <summary>暗化百分比（0～90）：压住背景亮度，保证面板上的小字仍可读。</summary>
        public int BackgroundDarken { get; set; }

        // ---- 外部程序路径 ----
        public string BambuPath { get; set; }
        public string SolidWorksPath { get; set; }

        public const string DefaultTheme = "丹砂";

        /// <summary>默认字号。V3.6.0 按作者常用设置由 12 提到 14。</summary>
        public const int DefaultFontSize = 14;
        public const string DefaultStlUnits = "mm";
        public const string DefaultStlQuality = "Fine";

        /// <summary>
        /// 旧的「三档强度」默认值。3.4.0 起界面改成两个滑杆，这个字段只用于迁移：
        /// 老配置若连 GlassBlur / GlassOpacity 都没有，就按这一档（轻柔）换算成
        /// 26 px / 14 %——那是 V3.4.0 时「轻柔档」的实际观感，保持不变，
        /// 免得已经升级过一次的老配置再被改一次外观。
        ///
        /// 注意：它**不等于** <see cref="DefaultGlassBlur"/> / <see cref="DefaultGlassOpacity"/>。
        /// 全新建的配置走 <see cref="CreateDefault"/> 直接用新默认值（3 px / 72 %）；
        /// 只有历史上真的带过强度字段的配置才走这条换算。
        /// </summary>
        public const int DefaultGlassStrength = 0;

        /// <summary>
        /// 默认毛玻璃模糊（像素）。V3.6.0 按作者常用设置由 40 降到 3——
        /// 极光本身是烘焙时糊过的，再叠一层重模糊只会让渐变发灰。
        /// 上限一并由 60 收到 40（这台机器上从来用不到更高）。
        /// </summary>
        public const int DefaultGlassBlur = 3;
        public const int MaxGlassBlur = 40;

        /// <summary>默认毛玻璃透明度（百分比）。V3.6.0 按作者常用设置由 26 提到 72。</summary>
        public const int DefaultGlassOpacity = 72;

        public const string DefaultBackgroundFit = "cover";
        public const int DefaultBackgroundBlur = 8;
        public const int DefaultBackgroundDarken = 50;
        public const int MaxBackgroundBlur = 40;
        public const int MaxBackgroundDarken = 90;

        /// <summary>
        /// 适配方式的合法取值，是这一组 token 的**唯一真源**：设置窗口的分段按钮按同一顺序排布，
        /// 校验也走这里，避免界面与校验各写一份而漂移。
        /// </summary>
        public static readonly string[] BackgroundFitTokens = { "cover", "fill", "center", "stretch" };

        /// <summary>实际生效的毛玻璃开关：只有显式写成 false 才关闭。</summary>
        public bool UseGlass
        {
            get { return Glass != false; }
        }

        /// <summary>实际生效的毛玻璃强度，夹到 0～2（仅迁移用）。</summary>
        public int GlassStrengthValue
        {
            get
            {
                int value = GlassStrength ?? DefaultGlassStrength;
                if (value < 0)
                {
                    return 0;
                }
                if (value > 2)
                {
                    return 2;
                }
                return value;
            }
        }

        /// <summary>
        /// 实际生效的毛玻璃模糊。优先级：新字段 → 3.3.0 的壁纸模糊（设了背景图时）
        /// → 旧三档强度换算。这样任何一代旧配置升级上来都不会突然变糊或变锐。
        /// </summary>
        public int GlassBlurValue
        {
            get
            {
                if (GlassBlur.HasValue)
                {
                    return ClampInt(GlassBlur.Value, 0, MaxGlassBlur);
                }
                if (!string.IsNullOrEmpty(BackgroundImage) && BackgroundBlur > 0)
                {
                    return ClampInt(BackgroundBlur, 0, MaxGlassBlur);
                }
                return StrengthBlur(GlassStrengthValue);
            }
        }

        /// <summary>实际生效的毛玻璃透明度：新字段优先，其次由旧三档强度换算。</summary>
        public int GlassOpacityValue
        {
            get
            {
                if (GlassOpacity.HasValue)
                {
                    return ClampInt(GlassOpacity.Value, 0, 100);
                }
                return StrengthOpacity(GlassStrengthValue);
            }
        }

        private static int StrengthBlur(int strength)
        {
            return strength <= 0 ? 26 : (strength == 1 ? 40 : 56);
        }

        private static int StrengthOpacity(int strength)
        {
            return strength <= 0 ? 14 : (strength == 1 ? 26 : 38);
        }

        /// <summary>实际生效的背景图适配方式；非法值一律回退到 cover。</summary>
        public string BackgroundFitValue
        {
            get
            {
                string fit = (BackgroundFit ?? "").Trim().ToLowerInvariant();
                for (int i = 0; i < BackgroundFitTokens.Length; i++)
                {
                    if (BackgroundFitTokens[i] == fit)
                    {
                        return fit;
                    }
                }
                return DefaultBackgroundFit;
            }
        }

        /// <summary>
        /// 实际生效的壁纸模糊。旧配置没有这个字段，反序列化得到 0 —— 此时等于「不模糊」，
        /// 而没设背景图时该值根本不参与渲染（极光沿用按玻璃档位推出的模糊），所以无需可空。
        /// </summary>
        public int BackgroundBlurValue
        {
            get { return ClampInt(BackgroundBlur, 0, MaxBackgroundBlur); }
        }

        /// <summary>实际生效的暗化百分比。渲染时还会叠加一个可读性下限，见 Backdrop。</summary>
        public int BackgroundDarkenValue
        {
            get { return ClampInt(BackgroundDarken, 0, MaxBackgroundDarken); }
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }
            if (value > max)
            {
                return max;
            }
            return value;
        }

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
            config.Glass = true;
            config.GlassStrength = DefaultGlassStrength;
            config.GlassBlur = DefaultGlassBlur;
            config.GlassOpacity = DefaultGlassOpacity;
            config.BackgroundImage = "";
            config.BackgroundFit = DefaultBackgroundFit;
            config.BackgroundBlur = DefaultBackgroundBlur;
            config.BackgroundDarken = DefaultBackgroundDarken;
            config.BambuPath = "";
            config.SolidWorksPath = "";
            return config;
        }
    }
}
