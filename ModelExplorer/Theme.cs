using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ModelExplorer
{
    public class AppTheme
    {
        public string Name { get; set; }
        public Color Bg { get; set; }
        public Color Sidebar { get; set; }
        public Color Panel { get; set; }
        public Color PanelActive { get; set; }
        public Color Border { get; set; }
        public Color Text { get; set; }
        public Color Muted { get; set; }
        public Color Accent { get; set; }
        public Color AccentHover { get; set; }
        public Color PartColor { get; set; }
        public Color AssemblyColor { get; set; }
        public Color StlColor { get; set; }
        public Color Success { get; set; }
        public Color Error { get; set; }
        public Color Code { get; set; }

        // ---- 毛玻璃 ----
        //
        // 画刷按透明度惰性重建：透明度为 0 时返回与旧版逐像素一致的实色画刷，
        // 因此「关闭毛玻璃」不需要任何额外分支，观感直接回到 v3.1.3。
        private int _glassOpacity;
        private int _brushOpacity = -1;
        private Brush _bgBrush;
        private Brush _windowBaseBrush;
        private Brush _sidebarBrush;
        private Brush _panelBrush;
        private Brush _panelActiveBrush;
        private Brush _opaquePanelBrush;
        private Brush _opaquePanelActiveBrush;
        private Brush _popupBrush;
        private Brush _borderBrush;
        private Brush _textBrush;
        private Brush _mutedBrush;
        private Brush _accentBrush;
        private Brush _accentHoverBrush;
        private Brush _partBrush;
        private Brush _assemblyBrush;
        private Brush _stlBrush;
        private Brush _successBrush;
        private Brush _errorBrush;
        private Brush _codeBrush;

        /// <summary>毛玻璃透明度（0～100）：0 = 面板不透明。由 <see cref="ThemeManager.ApplyGlass"/> 设置。</summary>
        public int GlassOpacity { get { return _glassOpacity; } }

        /// <summary>
        /// 极光背景的三个光斑颜色。直接跟随主题里已有的强调色 / STL 色 / 装配体色，
        /// 于是 5 套预设各有各的色相，不会全部糊成同一层灰。
        /// </summary>
        public Color AuroraPrimary { get { return Accent; } }
        public Color AuroraSecondary { get { return StlColor; } }
        public Color AuroraTertiary { get { return AssemblyColor; } }

        public Brush BgBrush { get { EnsureBrushes(); return _bgBrush; } }
        public Brush SidebarBrush { get { EnsureBrushes(); return _sidebarBrush; } }
        public Brush PanelBrush { get { EnsureBrushes(); return _panelBrush; } }
        public Brush PanelActiveBrush { get { EnsureBrushes(); return _panelActiveBrush; } }
        public Brush PopupBrush { get { EnsureBrushes(); return _popupBrush; } }
        public Brush BorderBrush { get { EnsureBrushes(); return _borderBrush; } }
        public Brush TextBrush { get { EnsureBrushes(); return _textBrush; } }
        public Brush MutedBrush { get { EnsureBrushes(); return _mutedBrush; } }
        public Brush AccentBrush { get { EnsureBrushes(); return _accentBrush; } }
        public Brush AccentHoverBrush { get { EnsureBrushes(); return _accentHoverBrush; } }
        public Brush PartBrush { get { EnsureBrushes(); return _partBrush; } }
        public Brush AssemblyBrush { get { EnsureBrushes(); return _assemblyBrush; } }
        public Brush StlBrush { get { EnsureBrushes(); return _stlBrush; } }
        public Brush SuccessBrush { get { EnsureBrushes(); return _successBrush; } }
        public Brush ErrorBrush { get { EnsureBrushes(); return _errorBrush; } }
        public Brush CodeBrush { get { EnsureBrushes(); return _codeBrush; } }

        /// <summary>
        /// 不透明面板色。给「不该透出背景」的地方用：工程名检查弹窗的列表行、
        /// 下拉菜单的选项——那些地方文字密集，透出壁纸只会更难读。
        /// </summary>
        public Brush OpaquePanelBrush { get { EnsureBrushes(); return _opaquePanelBrush; } }

        /// <summary>不透明面板色（悬停 / 表头态）。</summary>
        public Brush OpaquePanelActiveBrush { get { EnsureBrushes(); return _opaquePanelActiveBrush; } }

        /// <summary>
        /// 下拉弹窗底色。弹窗是独立的顶层窗口，背后就是主界面本身（不是极光层），
        /// 半透明会直接透出下面的文字，因此始终不透明。
        /// </summary>
        public Color PopupColor
        {
            get { return Panel; }
        }

        /// <summary>窗口底层的不透明底色。毛玻璃必须画在它之上：分层窗口一旦整体半透明，
        /// 就会直接透出桌面或下层窗口，压在上面的文字必然不可读。</summary>
        public Brush WindowBaseBrush { get { EnsureBrushes(); return _windowBaseBrush; } }

        /// <summary>设置毛玻璃透明度（0～100）：越大面板越透。</summary>
        public void SetGlassOpacity(int percent)
        {
            if (percent < 0)
            {
                percent = 0;
            }
            if (percent > 100)
            {
                percent = 100;
            }
            if (percent == _glassOpacity && _brushOpacity == _glassOpacity)
            {
                return;
            }
            _glassOpacity = percent;
            _brushOpacity = -1;
        }

        private void EnsureBrushes()
        {
            if (_brushOpacity == _glassOpacity && _bgBrush != null)
            {
                return;
            }

            _bgBrush = MakeBrush(Bg);
            _windowBaseBrush = MakeBrush(Sidebar);
            _textBrush = MakeBrush(Text);
            _mutedBrush = MakeBrush(Muted);
            _accentBrush = MakeBrush(Accent);
            _accentHoverBrush = MakeBrush(AccentHover);
            _partBrush = MakeBrush(PartColor);
            _assemblyBrush = MakeBrush(AssemblyColor);
            _stlBrush = MakeBrush(StlColor);
            _successBrush = MakeBrush(Success);
            _errorBrush = MakeBrush(Error);
            // 日志区要读长文本，始终不透明
            _codeBrush = MakeBrush(Code);
            // 列表与下拉菜单要的是「不透出背景」，与透明度滑杆无关
            _opaquePanelBrush = MakeBrush(Panel);
            _opaquePanelActiveBrush = MakeBrush(PanelActive);
            _popupBrush = MakeBrush(Panel);

            if (_glassOpacity <= 0)
            {
                _sidebarBrush = MakeBrush(Sidebar);
                _panelBrush = MakeBrush(Panel);
                _panelActiveBrush = MakeBrush(PanelActive);
                _borderBrush = MakeBrush(Border);
            }
            else
            {
                // 面板填充 = 1 - 透明度；侧栏比面板更实一点，否则压在壁纸上的正文会发飘
                double alpha = 1 - _glassOpacity / 100.0;
                // 玻璃材质是**无色**的：只保留原色的明度、去掉色相。
                // 否则熔岩红这类主题会把整片侧栏染成红色半透明，压在壁纸上像蒙了一层色纸；
                // 磨砂玻璃本身不吸色，主题色只该出现在强调色、文字与状态色上。
                // 原色略向白靠一点：近黑底上「更暗的半透明」看起来和背景没区别。
                _sidebarBrush = MakeGlassBrush(Blend(ToGray(Sidebar), Colors.White, 0.05), alpha + 0.06);
                _panelBrush = MakeGlassBrush(Blend(ToGray(Panel), Colors.White, 0.09), alpha);
                // 悬停 / 选中态是压在玻璃面板上的小色块，同样去色相
                _panelActiveBrush = MakeBrush(WithAlpha(ToGray(PanelActive), alpha + 0.16));
                // 描边也去色相：带色的一圈边会把「红色玻璃」的观感重新带回来
                _borderBrush = MakeBrush(WithAlpha(Blend(ToGray(Border), Colors.White, 0.18), 1 - alpha * 0.7));
            }

            _brushOpacity = _glassOpacity;
        }

        /// <summary>
        /// 玻璃面板填充：整体半透明，顶边略提亮、底边略压暗，
        /// 这点纵向明暗差就是「玻璃被上方光打亮」的关键线索，纯色半透明会显得脏。
        /// </summary>
        private static Brush MakeGlassBrush(Color color, double alpha)
        {
            Color top = WithAlpha(Blend(color, Colors.White, 0.07), ClampAlpha(alpha + 0.04));
            Color bottom = WithAlpha(Blend(color, Colors.Black, 0.06), ClampAlpha(alpha - 0.04));
            LinearGradientBrush brush = new LinearGradientBrush(top, bottom, new Point(0.5, 0), new Point(0.5, 1));
            brush.Freeze();
            return brush;
        }

        private static double ClampAlpha(double alpha)
        {
            if (alpha < 0)
            {
                return 0;
            }
            if (alpha > 1)
            {
                return 1;
            }
            return alpha;
        }

        /// <summary>
        /// 去色相、保留明度（Rec.601 亮度）。玻璃材质用它做填充：
        /// 同一套主题里「侧栏比面板暗、悬停比面板亮」的明度关系保持不变，但不再带主题色相。
        /// </summary>
        public static Color ToGray(Color color)
        {
            byte luma = (byte)((color.R * 299 + color.G * 587 + color.B * 114) / 1000);
            return Color.FromRgb(luma, luma, luma);
        }

        public static Color WithAlpha(Color color, double alpha)
        {
            return Color.FromArgb((byte)Math.Round(ClampAlpha(alpha) * 255), color.R, color.G, color.B);
        }

        public static Color Blend(Color color, Color other, double amount)
        {
            double keep = 1 - amount;
            return Color.FromArgb(
                color.A,
                (byte)Math.Round(color.R * keep + other.R * amount),
                (byte)Math.Round(color.G * keep + other.G * amount),
                (byte)Math.Round(color.B * keep + other.B * amount));
        }

        public static Brush MakeBrush(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }

    public static class ThemeManager
    {
        public static List<AppTheme> Presets { get; private set; }
        public static AppTheme Current { get; private set; }

        static ThemeManager()
        {
            Presets = new List<AppTheme>();
            Presets.Add(new AppTheme
            {
                Name = "终末地配色",
                Bg = Color.FromRgb(0x10, 0x10, 0x10),
                Sidebar = Color.FromRgb(0x16, 0x16, 0x16),
                Panel = Color.FromRgb(0x1C, 0x1C, 0x1C),
                PanelActive = Color.FromRgb(0x2A, 0x2A, 0x2A),
                Border = Color.FromRgb(0x3A, 0x3A, 0x3A),
                Text = Color.FromRgb(0xFF, 0xFF, 0xFF),
                Muted = Color.FromRgb(0xB0, 0xB0, 0xB0),
                Accent = Color.FromRgb(0xF5, 0xC5, 0x18),
                AccentHover = Color.FromRgb(0xFF, 0xD7, 0x5E),
                PartColor = Color.FromRgb(0xE8, 0xE8, 0xE8),
                AssemblyColor = Color.FromRgb(0xFF, 0xD7, 0x5E),
                StlColor = Color.FromRgb(0x8F, 0xA8, 0xC8),
                Success = Color.FromRgb(0x4C, 0xC3, 0x8A),
                Error = Color.FromRgb(0xFF, 0x6B, 0x6B),
                Code = Color.FromRgb(0x0A, 0x0A, 0x0A)
            });
            Presets.Add(new AppTheme
            {
                Name = "暗夜蓝",
                Bg = Color.FromRgb(0x0E, 0x17, 0x26),
                Sidebar = Color.FromRgb(0x11, 0x1E, 0x30),
                Panel = Color.FromRgb(0x15, 0x26, 0x3C),
                PanelActive = Color.FromRgb(0x1D, 0x32, 0x4C),
                Border = Color.FromRgb(0x27, 0x41, 0x5E),
                Text = Color.FromRgb(0xE7, 0xF1, 0xFF),
                Muted = Color.FromRgb(0x93, 0xA9, 0xC4),
                Accent = Color.FromRgb(0x3B, 0x9E, 0xFF),
                AccentHover = Color.FromRgb(0x6D, 0xB9, 0xFF),
                PartColor = Color.FromRgb(0x8F, 0xC7, 0xFF),
                AssemblyColor = Color.FromRgb(0x4D, 0xD6, 0xD6),
                StlColor = Color.FromRgb(0xB3, 0xA7, 0xFF),
                Success = Color.FromRgb(0x4C, 0xC3, 0x8A),
                Error = Color.FromRgb(0xFF, 0x6B, 0x6B),
                Code = Color.FromRgb(0x0A, 0x10, 0x18)
            });
            Presets.Add(new AppTheme
            {
                Name = "翡翠绿",
                Bg = Color.FromRgb(0x0D, 0x18, 0x15),
                Sidebar = Color.FromRgb(0x10, 0x22, 0x1D),
                Panel = Color.FromRgb(0x15, 0x2B, 0x24),
                PanelActive = Color.FromRgb(0x1D, 0x3A, 0x30),
                Border = Color.FromRgb(0x2A, 0x4B, 0x3E),
                Text = Color.FromRgb(0xE7, 0xF7, 0xF0),
                Muted = Color.FromRgb(0x8F, 0xB8, 0xA9),
                Accent = Color.FromRgb(0x2F, 0xB5, 0x7D),
                AccentHover = Color.FromRgb(0x5E, 0xD2, 0x9D),
                PartColor = Color.FromRgb(0x8B, 0xE3, 0xB8),
                AssemblyColor = Color.FromRgb(0xB9, 0xE8, 0x8F),
                StlColor = Color.FromRgb(0x6E, 0xC1, 0xFF),
                Success = Color.FromRgb(0x4C, 0xC3, 0x8A),
                Error = Color.FromRgb(0xFF, 0x6B, 0x6B),
                Code = Color.FromRgb(0x08, 0x12, 0x10)
            });
            Presets.Add(new AppTheme
            {
                Name = "紫罗兰",
                Bg = Color.FromRgb(0x16, 0x11, 0x22),
                Sidebar = Color.FromRgb(0x1C, 0x15, 0x30),
                Panel = Color.FromRgb(0x25, 0x1B, 0x3D),
                PanelActive = Color.FromRgb(0x32, 0x26, 0x4F),
                Border = Color.FromRgb(0x46, 0x37, 0x66),
                Text = Color.FromRgb(0xF0, 0xEA, 0xFF),
                Muted = Color.FromRgb(0xA9, 0x9B, 0xC7),
                Accent = Color.FromRgb(0xA7, 0x8B, 0xFA),
                AccentHover = Color.FromRgb(0xC3, 0xAE, 0xFC),
                PartColor = Color.FromRgb(0xCB, 0xB8, 0xFF),
                AssemblyColor = Color.FromRgb(0xF2, 0xC6, 0xFF),
                StlColor = Color.FromRgb(0x8F, 0xC7, 0xFF),
                Success = Color.FromRgb(0x4C, 0xC3, 0x8A),
                Error = Color.FromRgb(0xFF, 0x6B, 0x6B),
                Code = Color.FromRgb(0x0F, 0x0B, 0x17)
            });
            Presets.Add(new AppTheme
            {
                Name = "熔岩红",
                Bg = Color.FromRgb(0x1A, 0x0F, 0x10),
                Sidebar = Color.FromRgb(0x24, 0x13, 0x16),
                Panel = Color.FromRgb(0x2F, 0x19, 0x1C),
                PanelActive = Color.FromRgb(0x40, 0x22, 0x26),
                Border = Color.FromRgb(0x5A, 0x34, 0x38),
                Text = Color.FromRgb(0xFF, 0xED, 0xED),
                Muted = Color.FromRgb(0xD0, 0xA2, 0xA6),
                Accent = Color.FromRgb(0xFF, 0x5C, 0x5C),
                AccentHover = Color.FromRgb(0xFF, 0x8A, 0x8A),
                PartColor = Color.FromRgb(0xFF, 0x9A, 0x8A),
                AssemblyColor = Color.FromRgb(0xFF, 0xD0, 0x8A),
                StlColor = Color.FromRgb(0xFF, 0xC3, 0xE0),
                Success = Color.FromRgb(0x4C, 0xC3, 0x8A),
                Error = Color.FromRgb(0xFF, 0x6B, 0x6B),
                Code = Color.FromRgb(0x13, 0x09, 0x0A)
            });
            Presets.Add(new AppTheme
            {
                Name = "暖阳金",
                Bg = Color.FromRgb(0x19, 0x16, 0x10),
                Sidebar = Color.FromRgb(0x24, 0x1F, 0x16),
                Panel = Color.FromRgb(0x2F, 0x29, 0x1D),
                PanelActive = Color.FromRgb(0x40, 0x38, 0x2A),
                Border = Color.FromRgb(0x59, 0x4D, 0x39),
                Text = Color.FromRgb(0xFF, 0xF5, 0xE6),
                Muted = Color.FromRgb(0xD0, 0xBC, 0x9E),
                Accent = Color.FromRgb(0xF5, 0xC4, 0x51),
                AccentHover = Color.FromRgb(0xFF, 0xD9, 0x7A),
                PartColor = Color.FromRgb(0xFF, 0xE0, 0x8A),
                AssemblyColor = Color.FromRgb(0xF0, 0xA8, 0x68),
                StlColor = Color.FromRgb(0x8F, 0xC7, 0xFF),
                Success = Color.FromRgb(0x4C, 0xC3, 0x8A),
                Error = Color.FromRgb(0xFF, 0x6B, 0x6B),
                Code = Color.FromRgb(0x12, 0x0F, 0x0A)
            });
            // V3.5.0 新增六套：玻璃材质改成无色之后，预设之间的差异主要体现在强调色与状态色上，
            // 所以这里按色相铺开（含一套完全中性的石墨灰，配壁纸最百搭）。
            Presets.Add(new AppTheme
            {
                Name = "石墨灰",
                Bg = Color.FromRgb(0x12, 0x12, 0x12),
                Sidebar = Color.FromRgb(0x18, 0x18, 0x18),
                Panel = Color.FromRgb(0x1F, 0x1F, 0x1F),
                PanelActive = Color.FromRgb(0x2C, 0x2C, 0x2C),
                Border = Color.FromRgb(0x3C, 0x3C, 0x3C),
                Text = Color.FromRgb(0xF2, 0xF2, 0xF2),
                Muted = Color.FromRgb(0xAB, 0xAB, 0xAB),
                Accent = Color.FromRgb(0xD8, 0xD8, 0xD8),
                AccentHover = Color.FromRgb(0xF5, 0xF5, 0xF5),
                PartColor = Color.FromRgb(0xE6, 0xE6, 0xE6),
                AssemblyColor = Color.FromRgb(0xC9, 0xC9, 0xC9),
                StlColor = Color.FromRgb(0x9C, 0xB0, 0xC4),
                Success = Color.FromRgb(0x4C, 0xC3, 0x8A),
                Error = Color.FromRgb(0xFF, 0x6B, 0x6B),
                Code = Color.FromRgb(0x0A, 0x0A, 0x0A)
            });
            Presets.Add(new AppTheme
            {
                Name = "深海青",
                Bg = Color.FromRgb(0x08, 0x17, 0x1A),
                Sidebar = Color.FromRgb(0x0B, 0x20, 0x24),
                Panel = Color.FromRgb(0x10, 0x2A, 0x2F),
                PanelActive = Color.FromRgb(0x17, 0x39, 0x3F),
                Border = Color.FromRgb(0x24, 0x50, 0x55),
                Text = Color.FromRgb(0xE4, 0xF7, 0xF6),
                Muted = Color.FromRgb(0x8F, 0xB6, 0xB6),
                Accent = Color.FromRgb(0x2F, 0xC7, 0xC0),
                AccentHover = Color.FromRgb(0x63, 0xDE, 0xD8),
                PartColor = Color.FromRgb(0x8F, 0xE3, 0xDE),
                AssemblyColor = Color.FromRgb(0xA8, 0xE6, 0xA1),
                StlColor = Color.FromRgb(0x7F, 0xC4, 0xFF),
                Success = Color.FromRgb(0x4C, 0xC3, 0x8A),
                Error = Color.FromRgb(0xFF, 0x6B, 0x6B),
                Code = Color.FromRgb(0x05, 0x10, 0x0F)
            });
            Presets.Add(new AppTheme
            {
                Name = "樱花粉",
                Bg = Color.FromRgb(0x1A, 0x10, 0x14),
                Sidebar = Color.FromRgb(0x24, 0x14, 0x19),
                Panel = Color.FromRgb(0x2F, 0x1A, 0x20),
                PanelActive = Color.FromRgb(0x40, 0x24, 0x2C),
                Border = Color.FromRgb(0x5C, 0x37, 0x42),
                Text = Color.FromRgb(0xFF, 0xEA, 0xF0),
                Muted = Color.FromRgb(0xD3, 0xA4, 0xB0),
                Accent = Color.FromRgb(0xFF, 0x7B, 0xA8),
                AccentHover = Color.FromRgb(0xFF, 0xA0, 0xC2),
                PartColor = Color.FromRgb(0xFF, 0xC2, 0xD6),
                AssemblyColor = Color.FromRgb(0xFF, 0xD9, 0xA8),
                StlColor = Color.FromRgb(0x9F, 0xB6, 0xFF),
                Success = Color.FromRgb(0x4C, 0xC3, 0x8A),
                Error = Color.FromRgb(0xFF, 0x6B, 0x6B),
                Code = Color.FromRgb(0x12, 0x0A, 0x0D)
            });
            Presets.Add(new AppTheme
            {
                Name = "靛蓝",
                Bg = Color.FromRgb(0x0D, 0x10, 0x24),
                Sidebar = Color.FromRgb(0x12, 0x16, 0x36),
                Panel = Color.FromRgb(0x17, 0x1D, 0x46),
                PanelActive = Color.FromRgb(0x21, 0x2A, 0x5E),
                Border = Color.FromRgb(0x33, 0x3E, 0x7A),
                Text = Color.FromRgb(0xE8, 0xEC, 0xFF),
                Muted = Color.FromRgb(0x9B, 0xA4, 0xD0),
                Accent = Color.FromRgb(0x6E, 0x7B, 0xFF),
                AccentHover = Color.FromRgb(0x93, 0xA0, 0xFF),
                PartColor = Color.FromRgb(0xB7, 0xC0, 0xFF),
                AssemblyColor = Color.FromRgb(0x7F, 0xE0, 0xD0),
                StlColor = Color.FromRgb(0x8F, 0xB8, 0xFF),
                Success = Color.FromRgb(0x4C, 0xC3, 0x8A),
                Error = Color.FromRgb(0xFF, 0x6B, 0x6B),
                Code = Color.FromRgb(0x08, 0x0A, 0x18)
            });
            Presets.Add(new AppTheme
            {
                Name = "赤陶橙",
                Bg = Color.FromRgb(0x1A, 0x12, 0x10),
                Sidebar = Color.FromRgb(0x24, 0x19, 0x16),
                Panel = Color.FromRgb(0x2F, 0x21, 0x1C),
                PanelActive = Color.FromRgb(0x40, 0x2D, 0x26),
                Border = Color.FromRgb(0x5E, 0x43, 0x3A),
                Text = Color.FromRgb(0xFF, 0xED, 0xE4),
                Muted = Color.FromRgb(0xD0, 0xA8, 0x95),
                Accent = Color.FromRgb(0xE8, 0x76, 0x3C),
                AccentHover = Color.FromRgb(0xFF, 0x95, 0x58),
                PartColor = Color.FromRgb(0xFF, 0xB9, 0x8A),
                AssemblyColor = Color.FromRgb(0xFF, 0xD0, 0x8A),
                StlColor = Color.FromRgb(0x8F, 0xC7, 0xFF),
                Success = Color.FromRgb(0x4C, 0xC3, 0x8A),
                Error = Color.FromRgb(0xFF, 0x6B, 0x6B),
                Code = Color.FromRgb(0x12, 0x0C, 0x09)
            });
            Presets.Add(new AppTheme
            {
                Name = "苔原绿",
                Bg = Color.FromRgb(0x10, 0x14, 0x10),
                Sidebar = Color.FromRgb(0x16, 0x1C, 0x15),
                Panel = Color.FromRgb(0x1C, 0x24, 0x1B),
                PanelActive = Color.FromRgb(0x28, 0x32, 0x25),
                Border = Color.FromRgb(0x3C, 0x4A, 0x38),
                Text = Color.FromRgb(0xED, 0xF5, 0xE9),
                Muted = Color.FromRgb(0xA9, 0xBC, 0xA0),
                Accent = Color.FromRgb(0x8F, 0xBF, 0x4A),
                AccentHover = Color.FromRgb(0xAF, 0xD9, 0x6C),
                PartColor = Color.FromRgb(0xC6, 0xE3, 0x9A),
                AssemblyColor = Color.FromRgb(0xE3, 0xD0, 0x8A),
                StlColor = Color.FromRgb(0x8F, 0xC7, 0xFF),
                Success = Color.FromRgb(0x4C, 0xC3, 0x8A),
                Error = Color.FromRgb(0xFF, 0x6B, 0x6B),
                Code = Color.FromRgb(0x0A, 0x0E, 0x09)
            });

            Current = Presets[0];
        }

        public static void Apply(string name)
        {
            AppTheme theme = Presets.Find(t => t.Name == name);
            // 只换配色时保留当前透明度：主题与毛玻璃是两个独立的设置项
            int opacity = Current == null ? 0 : Current.GlassOpacity;
            Current = theme ?? Presets[0];
            Current.SetGlassOpacity(opacity);
        }

        /// <summary>
        /// 按配置一次性应用配色与毛玻璃（GUI 启动、冒烟测试共用同一入口，
        /// 避免出现「主题按配置、玻璃按默认」这种半生效状态）。
        /// </summary>
        public static void Apply(AppConfig config)
        {
            if (config == null)
            {
                Apply((string)null);
                ApplyGlass(false, 0);
                return;
            }

            Apply(config.Theme);
            ApplyGlass(config.UseGlass, config.GlassOpacityValue);
        }

        /// <summary>
        /// 设置毛玻璃透明度：0 = 面板不透明（背景被完全挡住，此时 Glass 不会再铺背景层），
        /// 100 = 面板完全透明。关闭开关等价于 0。
        /// </summary>
        public static void ApplyGlass(bool enabled, int opacityPercent)
        {
            if (Current == null)
            {
                return;
            }

            Current.SetGlassOpacity(enabled ? opacityPercent : 0);
        }
    }

    public class ToggleSwitch : CheckBox
    {
        public ToggleSwitch()
        {
            Cursor = Cursors.Hand;
            Template = UiFactory.ToggleTemplate();
        }
    }

    public static class UiFactory
    {
        public static ControlTemplate RoundedButtonTemplate(bool primary)
        {
            string hover = Hex(ThemeManager.Current.AccentHover);
            if (!primary)
            {
                hover = "#3A4253";
            }
            string xaml =
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'>" +
                "<Border x:Name='bd' Background='{TemplateBinding Background}' CornerRadius='7' Padding='{TemplateBinding Padding}' RenderTransformOrigin='0.5,0.5'>" +
                "<Border.RenderTransform><ScaleTransform x:Name='bdScale'/></Border.RenderTransform>" +
                "<ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>" +
                "</Border>" +
                "<ControlTemplate.Triggers>" +
                "<Trigger Property='IsMouseOver' Value='True'>" +
                "<Setter TargetName='bd' Property='Background' Value='" + hover + "'/>" +
                "<Trigger.EnterActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1.035' Duration='0:0:0.09'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1.035' Duration='0:0:0.09'/>" +
                "</Storyboard></BeginStoryboard></Trigger.EnterActions>" +
                "<Trigger.ExitActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1' Duration='0:0:0.12'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1' Duration='0:0:0.12'/>" +
                "</Storyboard></BeginStoryboard></Trigger.ExitActions>" +
                "</Trigger>" +
                "<Trigger Property='IsPressed' Value='True'>" +
                "<Trigger.EnterActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='0.92' Duration='0:0:0.07'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='0.92' Duration='0:0:0.07'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='Opacity' To='0.82' Duration='0:0:0.07'/>" +
                "</Storyboard></BeginStoryboard></Trigger.EnterActions>" +
                "<Trigger.ExitActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1' Duration='0:0:0.08'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1' Duration='0:0:0.08'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='Opacity' To='1' Duration='0:0:0.08'/>" +
                "</Storyboard></BeginStoryboard></Trigger.ExitActions>" +
                "</Trigger>" +
                "<Trigger Property='IsEnabled' Value='False'><Setter TargetName='bd' Property='Opacity' Value='0.45'/></Trigger>" +
                "</ControlTemplate.Triggers>" +
                "</ControlTemplate>";
            return (ControlTemplate)XamlReader.Parse(xaml);
        }

        public static ControlTemplate RoundedChromeButtonTemplate()
        {
            string xaml =
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'>" +
                "<Border x:Name='bd' Background='{TemplateBinding Background}' CornerRadius='5' Padding='{TemplateBinding Padding}' RenderTransformOrigin='0.5,0.5'>" +
                "<Border.RenderTransform><ScaleTransform x:Name='bdScale'/></Border.RenderTransform>" +
                "<ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>" +
                "</Border>" +
                "<ControlTemplate.Triggers>" +
                "<Trigger Property='IsMouseOver' Value='True'>" +
                "<Setter TargetName='bd' Property='Background' Value='#3A4253'/>" +
                "<Trigger.EnterActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1.09' Duration='0:0:0.08'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1.09' Duration='0:0:0.08'/>" +
                "</Storyboard></BeginStoryboard></Trigger.EnterActions>" +
                "<Trigger.ExitActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1' Duration='0:0:0.1'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1' Duration='0:0:0.1'/>" +
                "</Storyboard></BeginStoryboard></Trigger.ExitActions>" +
                "</Trigger>" +
                "<Trigger Property='IsPressed' Value='True'>" +
                "<Trigger.EnterActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='0.93' Duration='0:0:0.05'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='0.93' Duration='0:0:0.05'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='Opacity' To='0.78' Duration='0:0:0.05'/>" +
                "</Storyboard></BeginStoryboard></Trigger.EnterActions>" +
                "<Trigger.ExitActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1' Duration='0:0:0.08'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1' Duration='0:0:0.08'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='Opacity' To='1' Duration='0:0:0.08'/>" +
                "</Storyboard></BeginStoryboard></Trigger.ExitActions>" +
                "</Trigger>" +
                "</ControlTemplate.Triggers>" +
                "</ControlTemplate>";
            return (ControlTemplate)XamlReader.Parse(xaml);
        }

        public static Button MakeChromeButton(string glyph, RoutedEventHandler handler)
        {
            bool isMinimize = glyph == "—";
            Path icon = new Path
            {
                Data = Geometry.Parse(isMinimize ? "M 0,0 L 16,0" : "M 0,0 L 14,14 M 14,0 L 0,14"),
                Stroke = ThemeManager.Current.MutedBrush,
                StrokeThickness = 2.4,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Width = isMinimize ? 16 : 14,
                Height = isMinimize ? 2 : 14,
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Button button = new Button
            {
                Content = icon,
                Width = 36,
                Height = 28,
                Background = Brushes.Transparent,
                Foreground = ThemeManager.Current.MutedBrush,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(2, 0, 0, 0)
            };
            button.Template = RoundedChromeButtonTemplate();
            button.Click += handler;
            return button;
        }

        public static Border CreateWindowChrome(FrameworkElement content, AppTheme theme)
        {
            Grid shell = new Grid
            {
                ClipToBounds = true
            };

            // WindowBaseBrush 而不是 SidebarBrush：对话框是独立的顶层窗口，
            // 底层必须不透明，否则会把主界面透过对话框背景显示出来。
            Border background = new Border
            {
                Background = theme.WindowBaseBrush,
                CornerRadius = new CornerRadius(14),
                ClipToBounds = true
            };

            Rectangle line = new Rectangle
            {
                Stroke = theme.BorderBrush,
                StrokeThickness = 1,
                RadiusX = 14,
                RadiusY = 14,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            content.Margin = new Thickness(1);
            shell.Children.Add(background);
            shell.Children.Add(line);
            shell.Children.Add(content);

            // 六个对话框共用这里，毛玻璃只需接一次；关闭时该方法内部直接返回
            Glass.Apply(shell, theme);

            return new Border
            {
                Child = shell,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };
        }

        /// <summary>
        /// 拨钮开关模板（设置窗口使用）。
        ///
        /// V3.0.2 起加入滑动动画：滑块沿轨道平移 20px（0.16s 缓出），关闭时缓入滑回，
        /// 与主界面开关的时长和缓动一致。
        /// 注意：只做位移、不再修改对齐方式——位移与「改为右对齐」叠加会让滑块冲出轨道，
        /// 主界面 MainWindow.xaml 的 ToggleSwitchStyle 正是那种写法（已单独反馈）。
        /// </summary>
        public static ControlTemplate ToggleTemplate()
        {
            string accent = Hex(ThemeManager.Current.Accent);
            string trackOff = "#3A4253";
            string xaml =
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='CheckBox'>" +
                "<Grid Width='48' Height='28'>" +
                "<Border x:Name='track' Width='46' Height='26' CornerRadius='13' Background='" + trackOff + "'/>" +
                "<Ellipse x:Name='thumb' Width='20' Height='20' Fill='#FFFFFF' HorizontalAlignment='Left' Margin='3,0,0,0'>" +
                "<Ellipse.RenderTransform><TranslateTransform x:Name='thumbMove'/></Ellipse.RenderTransform>" +
                "</Ellipse>" +
                "<ContentPresenter Visibility='Collapsed'/>" +
                "</Grid>" +
                "<ControlTemplate.Triggers>" +
                "<Trigger Property='IsChecked' Value='True'>" +
                "<Setter TargetName='track' Property='Background' Value='" + accent + "'/>" +
                "<Trigger.EnterActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='thumbMove' Storyboard.TargetProperty='(TranslateTransform.X)' To='20' Duration='0:0:0.16'>" +
                "<DoubleAnimation.EasingFunction><QuadraticEase EasingMode='EaseOut'/></DoubleAnimation.EasingFunction>" +
                "</DoubleAnimation>" +
                "</Storyboard></BeginStoryboard></Trigger.EnterActions>" +
                "<Trigger.ExitActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='thumbMove' Storyboard.TargetProperty='(TranslateTransform.X)' To='0' Duration='0:0:0.16'>" +
                "<DoubleAnimation.EasingFunction><QuadraticEase EasingMode='EaseInOut'/></DoubleAnimation.EasingFunction>" +
                "</DoubleAnimation>" +
                "</Storyboard></BeginStoryboard></Trigger.ExitActions>" +
                "</Trigger>" +
                "</ControlTemplate.Triggers>" +
                "</ControlTemplate>";
            return (ControlTemplate)XamlReader.Parse(xaml);
        }

        public static ControlTemplate RoundedComboBoxTemplate()
        {
            // 弹窗底色写死成字面量而不是模板绑定：ComboBox.Background 是半透明的玻璃画刷，
            // 弹窗窗口背后没有极光层、只有主界面，沿用它会直接透出底下的文字。
            string popup = HexA(ThemeManager.Current.PopupColor);
            string xaml =
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
                " xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ComboBox'>" +
                "<Grid>" +
                "<ToggleButton x:Name='ToggleButton' Focusable='False' ClickMode='Press'" +
                " Background='{TemplateBinding Background}' Foreground='{TemplateBinding Foreground}'" +
                " BorderBrush='{TemplateBinding BorderBrush}' Padding='{TemplateBinding Padding}'" +
                " IsChecked='{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}'" +
                " Content='{TemplateBinding SelectionBoxItem}' ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'>" +
                "<ToggleButton.Template>" +
                "<ControlTemplate TargetType='ToggleButton'>" +
                "<Border x:Name='bd' Background='{TemplateBinding Background}'" +
                " BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}'" +
                " CornerRadius='8' Padding='{TemplateBinding Padding}'>" +
                "<Grid>" +
                "<Grid.ColumnDefinitions><ColumnDefinition Width='*'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions>" +
                "<ContentPresenter Content='{TemplateBinding Content}' VerticalAlignment='Center'/>" +
                "<Path Grid.Column='1' Data='M 0,0 L 8,8 L 16,0' Stroke='{TemplateBinding Foreground}'" +
                " StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' Width='14' Height='10'" +
                " Stretch='Fill' Margin='8,0,4,0' VerticalAlignment='Center'/>" +
                "</Grid></Border></ControlTemplate>" +
                "</ToggleButton.Template></ToggleButton>" +
                "<Popup x:Name='PART_Popup' Placement='Bottom' AllowsTransparency='True' Focusable='False'" +
                " IsOpen='{TemplateBinding IsDropDownOpen}' PopupAnimation='Slide'>" +
                "<Border Background='" + popup + "' BorderBrush='{TemplateBinding BorderBrush}'" +
                " BorderThickness='0' CornerRadius='8' MinWidth='280'" +
                " MaxHeight='{TemplateBinding MaxDropDownHeight}' Padding='4'>" +
                "<ScrollViewer VerticalScrollBarVisibility='Auto' HorizontalScrollBarVisibility='Disabled' Background='Transparent'>" +
                "<ItemsPresenter/>" +
                "</ScrollViewer></Border></Popup>" +
                "</Grid></ControlTemplate>";
            return (ControlTemplate)XamlReader.Parse(xaml);
        }

        public static ControlTemplate RoundedCheckBoxTemplate()
        {
            string accent = Hex(ThemeManager.Current.Accent);
            string panel = Hex(ThemeManager.Current.PanelActive);
            string border = Hex(ThemeManager.Current.Border);
            string xaml =
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='CheckBox'>" +
                "<Grid Width='24' Height='24'>" +
                "<Border x:Name='box' Width='24' Height='24' CornerRadius='7' Background='" + panel + "'" +
                " BorderBrush='" + border + "' BorderThickness='1'>" +
                "<Path x:Name='check' Data='M 2,6 L 7,11 L 14,2' Stroke='#151515' StrokeThickness='2.6'" +
                " StrokeStartLineCap='Round' StrokeEndLineCap='Round' HorizontalAlignment='Center'" +
                " VerticalAlignment='Center' Width='14' Height='11' Stretch='Fill' Visibility='Collapsed'/>" +
                "</Border>" +
                "<ContentPresenter Visibility='Collapsed'/>" +
                "</Grid>" +
                "<ControlTemplate.Triggers>" +
                "<Trigger Property='IsChecked' Value='True'>" +
                "<Setter TargetName='box' Property='Background' Value='" + accent + "'/>" +
                "<Setter TargetName='check' Property='Visibility' Value='Visible'/>" +
                "</Trigger>" +
                "<Trigger Property='IsMouseOver' Value='True'>" +
                "<Setter TargetName='box' Property='BorderBrush' Value='" + accent + "'/>" +
                "</Trigger>" +
                "</ControlTemplate.Triggers>" +
                "</ControlTemplate>";
            return (ControlTemplate)XamlReader.Parse(xaml);
        }

        /// <summary>
        /// 滑杆模板（设置窗口的背景图分区用）。
        /// 轨道用滑杆两侧的 RepeatButton 分别画「已填充」和「未填充」两段，
        /// 滑块是强调色圆点加一个深色芯，和拨钮开关的观感保持一致。
        /// </summary>
        public static ControlTemplate SliderTemplate()
        {
            string accent = Hex(ThemeManager.Current.Accent);
            string groove = Hex(ThemeManager.Current.PanelActive);
            string core = Hex(ThemeManager.Current.Code);
            string xaml =
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
                " xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Slider'>" +
                "<Grid Height='22' VerticalAlignment='Center'>" +
                "<Border Height='4' CornerRadius='2' Background='" + groove + "' VerticalAlignment='Center'/>" +
                "<Track x:Name='PART_Track'>" +
                "<Track.DecreaseRepeatButton>" +
                "<RepeatButton Command='Slider.DecreaseLarge' Focusable='False' IsTabStop='False'>" +
                "<RepeatButton.Template><ControlTemplate TargetType='RepeatButton'>" +
                "<Border Height='4' CornerRadius='2' Background='" + accent + "' VerticalAlignment='Center'/>" +
                "</ControlTemplate></RepeatButton.Template></RepeatButton>" +
                "</Track.DecreaseRepeatButton>" +
                "<Track.Thumb>" +
                "<Thumb Width='16' Height='16' Focusable='False'>" +
                "<Thumb.Template><ControlTemplate TargetType='Thumb'>" +
                "<Grid>" +
                "<Ellipse Width='16' Height='16' Fill='" + accent + "'/>" +
                "<Ellipse Width='6' Height='6' Fill='" + core + "'/>" +
                "</Grid>" +
                "</ControlTemplate></Thumb.Template></Thumb>" +
                "</Track.Thumb>" +
                "<Track.IncreaseRepeatButton>" +
                "<RepeatButton Command='Slider.IncreaseLarge' Focusable='False' IsTabStop='False'>" +
                "<RepeatButton.Template><ControlTemplate TargetType='RepeatButton'>" +
                "<Border Height='4' Background='Transparent' VerticalAlignment='Center'/>" +
                "</ControlTemplate></RepeatButton.Template></RepeatButton>" +
                "</Track.IncreaseRepeatButton>" +
                "</Track></Grid></ControlTemplate>";
            return (ControlTemplate)XamlReader.Parse(xaml);
        }

        /// <summary>
        /// 分段选择按钮（背景图「适配」那一排）。用 RadioButton 拿到互斥语义，
        /// 选中态填充强调色并把文字压成深色，未选中态是面板色 + 常规文字色。
        ///
        /// 文字颜色必须用 TargetName 打在 ContentPresenter 上，不能写
        /// <c>&lt;Setter Property='Foreground'&gt;</c>：本地值（控件构造时赋的
        /// Foreground）优先级高于模板触发器，选中态会被本地值盖掉，
        /// 结果就是强调色底上压着灰字看不清。
        /// </summary>
        public static ControlTemplate SegmentTemplate()
        {
            string accent = Hex(ThemeManager.Current.Accent);
            string panel = Hex(ThemeManager.Current.PanelActive);
            string border = Hex(ThemeManager.Current.Border);
            string dark = "#151515";
            string xaml =
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
                " xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='RadioButton'>" +
                "<Border x:Name='bd' CornerRadius='8' Background='" + panel + "'" +
                " BorderBrush='" + border + "' BorderThickness='1' Padding='0,7'>" +
                "<ContentPresenter x:Name='presenter' HorizontalAlignment='Center' VerticalAlignment='Center'/>" +
                "</Border>" +
                "<ControlTemplate.Triggers>" +
                "<Trigger Property='IsChecked' Value='True'>" +
                "<Setter TargetName='bd' Property='Background' Value='" + accent + "'/>" +
                "<Setter TargetName='bd' Property='BorderBrush' Value='" + accent + "'/>" +
                "<Setter TargetName='presenter' Property='TextElement.Foreground' Value='" + dark + "'/>" +
                "</Trigger>" +
                "<Trigger Property='IsMouseOver' Value='True'>" +
                "<Setter TargetName='bd' Property='BorderBrush' Value='" + accent + "'/>" +
                "</Trigger>" +
                "</ControlTemplate.Triggers></ControlTemplate>";
            return (ControlTemplate)XamlReader.Parse(xaml);
        }

        private static string Hex(Color color)
        {
            return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }

        /// <summary>带透明度的颜色字面量，供必须脱离动态资源解析的模板使用（如弹窗底色）。</summary>
        private static string HexA(Color color)
        {
            return "#" + color.A.ToString("X2") + Hex(color).Substring(1);
        }
    }
}
