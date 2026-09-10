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
    /// <summary>
    /// 一套配色。v3.1.0 起除原有的 15 个基础色外，额外区分了
    /// “深色底面（页眉 / 左侧导航 / 日志 / 状态栏）上的文字颜色”，
    /// 这样同一套模板既能用于黑底导航，也能用于深色预设。
    /// </summary>
    public class AppTheme
    {
        public string Name { get; set; }

        // ---- 基础面 ----
        public Color Bg { get; set; }
        public Color Sidebar { get; set; }
        public Color Panel { get; set; }
        public Color PanelActive { get; set; }
        public Color Border { get; set; }
        public Color Text { get; set; }
        public Color Muted { get; set; }

        // ---- 强调色 ----
        public Color Accent { get; set; }
        public Color AccentHover { get; set; }
        /// <summary>落在强调色块上的文字颜色（黄底黑字）。</summary>
        public Color OnAccent { get; set; }

        // ---- 深色底面（导航 / 页眉 / 日志 / 状态栏）上的文字 ----
        public Color SidebarText { get; set; }
        public Color SidebarMuted { get; set; }
        public Color CodeText { get; set; }
        /// <summary>深色底面上控件的悬停底色。</summary>
        public Color ChromeHover { get; set; }

        // ---- 类型徽标填充色 ----
        public Color PartColor { get; set; }
        public Color AssemblyColor { get; set; }
        public Color StlColor { get; set; }

        // ---- 语义色 ----
        public Color Success { get; set; }
        public Color Error { get; set; }
        public Color Code { get; set; }

        public Brush BgBrush { get { return MakeBrush(Bg); } }
        public Brush SidebarBrush { get { return MakeBrush(Sidebar); } }
        public Brush PanelBrush { get { return MakeBrush(Panel); } }
        public Brush PanelActiveBrush { get { return MakeBrush(PanelActive); } }
        public Brush BorderBrush { get { return MakeBrush(Border); } }
        public Brush TextBrush { get { return MakeBrush(Text); } }
        public Brush MutedBrush { get { return MakeBrush(Muted); } }
        public Brush AccentBrush { get { return MakeBrush(Accent); } }
        public Brush AccentHoverBrush { get { return MakeBrush(AccentHover); } }
        public Brush OnAccentBrush { get { return MakeBrush(OnAccent); } }
        public Brush SidebarTextBrush { get { return MakeBrush(SidebarText); } }
        public Brush SidebarMutedBrush { get { return MakeBrush(SidebarMuted); } }
        public Brush CodeTextBrush { get { return MakeBrush(CodeText); } }
        public Brush ChromeHoverBrush { get { return MakeBrush(ChromeHover); } }
        public Brush PartBrush { get { return MakeBrush(PartColor); } }
        public Brush AssemblyBrush { get { return MakeBrush(AssemblyColor); } }
        public Brush StlBrush { get { return MakeBrush(StlColor); } }
        public Brush SuccessBrush { get { return MakeBrush(Success); } }
        public Brush ErrorBrush { get { return MakeBrush(Error); } }
        public Brush CodeBrush { get { return MakeBrush(Code); } }

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

            // ---------------------------------------------------------------
            // 默认预设：黑白灰黄 · 工业机能（v3.1.0 新视觉）
            //   白色图纸底 + 黑色页面骨架（页眉 / 左侧导航 / 状态栏）
            //   + 灰色细线 + 唯一的高饱和黄作为强调
            // ---------------------------------------------------------------
            Presets.Add(new AppTheme
            {
                Name = "终末地配色",
                Bg = Color.FromRgb(0xF2, 0xF2, 0xEF),
                Sidebar = Color.FromRgb(0x11, 0x11, 0x11),
                Panel = Color.FromRgb(0xFF, 0xFF, 0xFF),
                PanelActive = Color.FromRgb(0xE7, 0xE7, 0xE1),
                Border = Color.FromRgb(0xD5, 0xD5, 0xCE),
                Text = Color.FromRgb(0x11, 0x11, 0x11),
                Muted = Color.FromRgb(0x6E, 0x6E, 0x66),
                Accent = Color.FromRgb(0xF2, 0xD4, 0x00),
                AccentHover = Color.FromRgb(0xFF, 0xE6, 0x4D),
                OnAccent = Color.FromRgb(0x11, 0x11, 0x11),
                SidebarText = Color.FromRgb(0xF5, 0xF5, 0xF1),
                SidebarMuted = Color.FromRgb(0x9E, 0x9E, 0x96),
                CodeText = Color.FromRgb(0xE6, 0xE6, 0xDF),
                ChromeHover = Color.FromRgb(0x2A, 0x2A, 0x2A),
                PartColor = Color.FromRgb(0x11, 0x11, 0x11),
                AssemblyColor = Color.FromRgb(0xF2, 0xD4, 0x00),
                StlColor = Color.FromRgb(0x6E, 0x6E, 0x66),
                Success = Color.FromRgb(0x1E, 0x7A, 0x4B),
                Error = Color.FromRgb(0xC1, 0x12, 0x1F),
                Code = Color.FromRgb(0x11, 0x11, 0x11)
            });

            // ---- 以下为保留的深色预设（黑白灰黄之外的可选外观） ----
            Presets.Add(Dark("暗夜蓝",
                Color.FromRgb(0x0E, 0x17, 0x26), Color.FromRgb(0x11, 0x1E, 0x30),
                Color.FromRgb(0x15, 0x26, 0x3C), Color.FromRgb(0x1D, 0x32, 0x4C),
                Color.FromRgb(0x27, 0x41, 0x5E), Color.FromRgb(0xE7, 0xF1, 0xFF),
                Color.FromRgb(0x93, 0xA9, 0xC4), Color.FromRgb(0x3B, 0x9E, 0xFF),
                Color.FromRgb(0x6D, 0xB9, 0xFF), Color.FromRgb(0x8F, 0xC7, 0xFF),
                Color.FromRgb(0x4D, 0xD6, 0xD6), Color.FromRgb(0xB3, 0xA7, 0xFF),
                Color.FromRgb(0x0A, 0x10, 0x18)));

            Presets.Add(Dark("翡翠绿",
                Color.FromRgb(0x0D, 0x18, 0x15), Color.FromRgb(0x10, 0x22, 0x1D),
                Color.FromRgb(0x15, 0x2B, 0x24), Color.FromRgb(0x1D, 0x3A, 0x30),
                Color.FromRgb(0x2A, 0x4B, 0x3E), Color.FromRgb(0xE7, 0xF7, 0xF0),
                Color.FromRgb(0x8F, 0xB8, 0xA9), Color.FromRgb(0x2F, 0xB5, 0x7D),
                Color.FromRgb(0x5E, 0xD2, 0x9D), Color.FromRgb(0x8B, 0xE3, 0xB8),
                Color.FromRgb(0xB9, 0xE8, 0x8F), Color.FromRgb(0x6E, 0xC1, 0xFF),
                Color.FromRgb(0x08, 0x12, 0x10)));

            Presets.Add(Dark("紫罗兰",
                Color.FromRgb(0x16, 0x11, 0x22), Color.FromRgb(0x1C, 0x15, 0x30),
                Color.FromRgb(0x25, 0x1B, 0x3D), Color.FromRgb(0x32, 0x26, 0x4F),
                Color.FromRgb(0x46, 0x37, 0x66), Color.FromRgb(0xF0, 0xEA, 0xFF),
                Color.FromRgb(0xA9, 0x9B, 0xC7), Color.FromRgb(0xA7, 0x8B, 0xFA),
                Color.FromRgb(0xC3, 0xAE, 0xFC), Color.FromRgb(0xCB, 0xB8, 0xFF),
                Color.FromRgb(0xF2, 0xC6, 0xFF), Color.FromRgb(0x8F, 0xC7, 0xFF),
                Color.FromRgb(0x0F, 0x0B, 0x17)));

            Presets.Add(Dark("熔岩红",
                Color.FromRgb(0x1A, 0x0F, 0x10), Color.FromRgb(0x24, 0x13, 0x16),
                Color.FromRgb(0x2F, 0x19, 0x1C), Color.FromRgb(0x40, 0x22, 0x26),
                Color.FromRgb(0x5A, 0x34, 0x38), Color.FromRgb(0xFF, 0xED, 0xED),
                Color.FromRgb(0xD0, 0xA2, 0xA6), Color.FromRgb(0xFF, 0x5C, 0x5C),
                Color.FromRgb(0xFF, 0x8A, 0x8A), Color.FromRgb(0xFF, 0x9A, 0x8A),
                Color.FromRgb(0xFF, 0xD0, 0x8A), Color.FromRgb(0xFF, 0xC3, 0xE0),
                Color.FromRgb(0x13, 0x09, 0x0A)));

            Presets.Add(Dark("暖阳金",
                Color.FromRgb(0x19, 0x16, 0x10), Color.FromRgb(0x24, 0x1F, 0x16),
                Color.FromRgb(0x2F, 0x29, 0x1D), Color.FromRgb(0x40, 0x38, 0x2A),
                Color.FromRgb(0x59, 0x4D, 0x39), Color.FromRgb(0xFF, 0xF5, 0xE6),
                Color.FromRgb(0xD0, 0xBC, 0x9E), Color.FromRgb(0xF5, 0xC4, 0x51),
                Color.FromRgb(0xFF, 0xD9, 0x7A), Color.FromRgb(0xFF, 0xE0, 0x8A),
                Color.FromRgb(0xF0, 0xA8, 0x68), Color.FromRgb(0x8F, 0xC7, 0xFF),
                Color.FromRgb(0x12, 0x0F, 0x0A)));

            Current = Presets[0];
        }

        /// <summary>
        /// 深色预设的统一构造：深色底面文字沿用正文/次要文字色，
        /// 强调色块上统一使用深色文字。
        /// </summary>
        private static AppTheme Dark(
            string name,
            Color bg, Color sidebar, Color panel, Color panelActive, Color border,
            Color text, Color muted, Color accent, Color accentHover,
            Color part, Color assembly, Color stl, Color code)
        {
            return new AppTheme
            {
                Name = name,
                Bg = bg,
                Sidebar = sidebar,
                Panel = panel,
                PanelActive = panelActive,
                Border = border,
                Text = text,
                Muted = muted,
                Accent = accent,
                AccentHover = accentHover,
                OnAccent = Color.FromRgb(0x15, 0x15, 0x15),
                SidebarText = text,
                SidebarMuted = muted,
                CodeText = muted,
                ChromeHover = panelActive,
                PartColor = part,
                AssemblyColor = assembly,
                StlColor = stl,
                Success = Color.FromRgb(0x4C, 0xC3, 0x8A),
                Error = Color.FromRgb(0xFF, 0x6B, 0x6B),
                Code = code
            };
        }

        public static void Apply(string name)
        {
            AppTheme theme = Presets.Find(t => t.Name == name);
            Current = theme ?? Presets[0];
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

    /// <summary>
    /// 程序化构建弹窗时使用的控件模板。
    ///
    /// v3.1.0 视觉：直角（CornerRadius = 0）、1px 细线、单一高饱和黄强调，
    /// 并且所有颜色都取自当前主题（不再出现写死的 #3A4253 / #FFFFFF）。
    /// 方法名保持不变，供各弹窗继续调用。
    /// </summary>
    public static class UiFactory
    {
        public static ControlTemplate RoundedButtonTemplate(bool primary)
        {
            AppTheme theme = ThemeManager.Current;
            string hover = Hex(primary ? theme.AccentHover : theme.PanelActive);
            string baseFill = primary ? Hex(theme.Accent) : Hex(theme.Panel);
            string borderBrush = primary ? Hex(theme.Accent) : Hex(theme.Border);
            string hoverBorder = primary ? Hex(theme.AccentHover) : Hex(theme.Text);
            string borderThickness = primary ? "0" : "1";

            string xaml =
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'>" +
                "<Border x:Name='bd' Background='" + baseFill + "' BorderBrush='" + borderBrush + "' BorderThickness='" + borderThickness + "'" +
                " CornerRadius='0' Padding='{TemplateBinding Padding}' RenderTransformOrigin='0.5,0.5'>" +
                "<Border.RenderTransform><ScaleTransform x:Name='bdScale'/></Border.RenderTransform>" +
                "<ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>" +
                "</Border>" +
                "<ControlTemplate.Triggers>" +
                "<Trigger Property='IsMouseOver' Value='True'>" +
                "<Setter TargetName='bd' Property='Background' Value='" + hover + "'/>" +
                "<Setter TargetName='bd' Property='BorderBrush' Value='" + hoverBorder + "'/>" +
                "<Trigger.EnterActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1.02' Duration='0:0:0.09'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1.02' Duration='0:0:0.09'/>" +
                "</Storyboard></BeginStoryboard></Trigger.EnterActions>" +
                "<Trigger.ExitActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1' Duration='0:0:0.12'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1' Duration='0:0:0.12'/>" +
                "</Storyboard></BeginStoryboard></Trigger.ExitActions>" +
                "</Trigger>" +
                "<Trigger Property='IsPressed' Value='True'>" +
                "<Trigger.EnterActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='0.97' Duration='0:0:0.07'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='0.97' Duration='0:0:0.07'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='Opacity' To='0.85' Duration='0:0:0.07'/>" +
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
            string hover = Hex(ThemeManager.Current.ChromeHover);
            string xaml =
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'>" +
                "<Border x:Name='bd' Background='{TemplateBinding Background}' CornerRadius='0' Padding='{TemplateBinding Padding}' RenderTransformOrigin='0.5,0.5'>" +
                "<Border.RenderTransform><ScaleTransform x:Name='bdScale'/></Border.RenderTransform>" +
                "<ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>" +
                "</Border>" +
                "<ControlTemplate.Triggers>" +
                "<Trigger Property='IsMouseOver' Value='True'>" +
                "<Setter TargetName='bd' Property='Background' Value='" + hover + "'/>" +
                "<Trigger.EnterActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1.06' Duration='0:0:0.08'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1.06' Duration='0:0:0.08'/>" +
                "</Storyboard></BeginStoryboard></Trigger.EnterActions>" +
                "<Trigger.ExitActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1' Duration='0:0:0.1'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1' Duration='0:0:0.1'/>" +
                "</Storyboard></BeginStoryboard></Trigger.ExitActions>" +
                "</Trigger>" +
                "<Trigger Property='IsPressed' Value='True'>" +
                "<Trigger.EnterActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='0.94' Duration='0:0:0.05'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='0.94' Duration='0:0:0.05'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='Opacity' To='0.8' Duration='0:0:0.05'/>" +
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
            AppTheme theme = ThemeManager.Current;
            Path icon = new Path
            {
                // 工业风：直角收边
                Data = Geometry.Parse(isMinimize ? "M 0,0 L 16,0" : "M 0,0 L 14,14 M 14,0 L 0,14"),
                Stroke = theme.SidebarMutedBrush,
                StrokeThickness = 2.2,
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
                Foreground = theme.SidebarMutedBrush,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(2, 0, 0, 0)
            };
            button.Template = RoundedChromeButtonTemplate();
            button.Click += handler;
            return button;
        }

        /// <summary>
        /// 弹窗外壳：v3.1.0 起使用图纸底色（theme.Bg）而不是导航色，
        /// 否则在“黑底导航”的默认主题下弹窗会变成黑底黑字。
        /// </summary>
        public static Border CreateWindowChrome(FrameworkElement content, AppTheme theme)
        {
            Grid shell = new Grid
            {
                ClipToBounds = true
            };

            Border background = new Border
            {
                Background = theme.BgBrush,
                CornerRadius = new CornerRadius(0),
                ClipToBounds = true
            };

            Rectangle line = new Rectangle
            {
                Stroke = theme.BorderBrush,
                StrokeThickness = 1,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            content.Margin = new Thickness(1);
            shell.Children.Add(background);
            shell.Children.Add(line);
            shell.Children.Add(content);

            return new Border
            {
                Child = shell,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };
        }

        /// <summary>
        /// 机械开关：直角轨道 + 方块滑块（原为圆角胶囊 + 圆形滑块）。
        /// 几何尺寸与动画目标名保持不变。
        /// </summary>
        public static ControlTemplate ToggleTemplate()
        {
            AppTheme theme = ThemeManager.Current;
            string accent = Hex(theme.Accent);
            string trackOff = Hex(theme.PanelActive);
            string trackBorder = Hex(theme.Border);
            string thumbOff = Hex(theme.Panel);
            string thumbOn = Hex(theme.OnAccent);
            string xaml =
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='CheckBox'>" +
                "<Grid Width='48' Height='28'>" +
                "<Border x:Name='track' Width='46' Height='26' CornerRadius='0' Background='" + trackOff + "'" +
                " BorderBrush='" + trackBorder + "' BorderThickness='1'/>" +
                "<Border x:Name='thumb' Width='20' Height='20' Background='" + thumbOff + "' BorderBrush='" + trackBorder + "' BorderThickness='1' HorizontalAlignment='Left' Margin='3,0,0,0'>" +
                "<Border.RenderTransform><TranslateTransform x:Name='thumbMove'/></Border.RenderTransform>" +
                "</Border>" +
                "<ContentPresenter Visibility='Collapsed'/>" +
                "</Grid>" +
                "<ControlTemplate.Triggers>" +
                "<Trigger Property='IsChecked' Value='True'>" +
                "<Setter TargetName='track' Property='Background' Value='" + accent + "'/>" +
                "<Setter TargetName='track' Property='BorderBrush' Value='" + accent + "'/>" +
                "<Setter TargetName='thumb' Property='Background' Value='" + thumbOn + "'/>" +
                "<Setter TargetName='thumb' Property='BorderBrush' Value='" + thumbOn + "'/>" +
                "<Trigger.EnterActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='thumbMove' Storyboard.TargetProperty='(TranslateTransform.X)' To='20' Duration='0:0:0.16'/>" +
                "</Storyboard></BeginStoryboard></Trigger.EnterActions>" +
                "<Trigger.ExitActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='thumbMove' Storyboard.TargetProperty='(TranslateTransform.X)' To='0' Duration='0:0:0.16'/>" +
                "</Storyboard></BeginStoryboard></Trigger.ExitActions>" +
                "</Trigger>" +
                "</ControlTemplate.Triggers>" +
                "</ControlTemplate>";
            return (ControlTemplate)XamlReader.Parse(xaml);
        }

        public static ControlTemplate RoundedComboBoxTemplate()
        {
            string panel = Hex(ThemeManager.Current.Panel);
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
                " CornerRadius='0' Padding='{TemplateBinding Padding}'>" +
                "<Grid>" +
                "<Grid.ColumnDefinitions><ColumnDefinition Width='*'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions>" +
                "<ContentPresenter Content='{TemplateBinding Content}' VerticalAlignment='Center'/>" +
                "<Path Grid.Column='1' Data='M 0,0 L 8,8 L 16,0' Stroke='{TemplateBinding Foreground}'" +
                " StrokeThickness='2' Width='14' Height='10'" +
                " Stretch='Fill' Margin='8,0,4,0' VerticalAlignment='Center'/>" +
                "</Grid></Border></ControlTemplate>" +
                "</ToggleButton.Template></ToggleButton>" +
                "<Popup x:Name='PART_Popup' Placement='Bottom' AllowsTransparency='True' Focusable='False'" +
                " IsOpen='{TemplateBinding IsDropDownOpen}' PopupAnimation='Slide'>" +
                "<Border Background='" + panel + "' BorderBrush='{TemplateBinding BorderBrush}'" +
                " BorderThickness='1' CornerRadius='0' MinWidth='280'" +
                " MaxHeight='{TemplateBinding MaxDropDownHeight}' Padding='0'>" +
                "<ScrollViewer VerticalScrollBarVisibility='Auto' HorizontalScrollBarVisibility='Disabled' Background='Transparent'>" +
                "<ItemsPresenter/>" +
                "</ScrollViewer></Border></Popup>" +
                "</Grid></ControlTemplate>";
            return (ControlTemplate)XamlReader.Parse(xaml);
        }

        public static ControlTemplate RoundedCheckBoxTemplate()
        {
            AppTheme theme = ThemeManager.Current;
            string accent = Hex(theme.Accent);
            string panel = Hex(theme.Panel);
            string border = Hex(theme.Border);
            string onAccent = Hex(theme.OnAccent);
            string xaml =
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='CheckBox'>" +
                "<Grid Width='24' Height='24'>" +
                "<Border x:Name='box' Width='24' Height='24' CornerRadius='0' Background='" + panel + "'" +
                " BorderBrush='" + border + "' BorderThickness='1'>" +
                "<Path x:Name='check' Data='M 2,6 L 7,11 L 14,2' Stroke='" + onAccent + "' StrokeThickness='2.6'" +
                " HorizontalAlignment='Center' VerticalAlignment='Center' Width='14' Height='11' Stretch='Fill' Visibility='Collapsed'/>" +
                "</Border>" +
                "<ContentPresenter Visibility='Collapsed'/>" +
                "</Grid>" +
                "<ControlTemplate.Triggers>" +
                "<Trigger Property='IsChecked' Value='True'>" +
                "<Setter TargetName='box' Property='Background' Value='" + accent + "'/>" +
                "<Setter TargetName='box' Property='BorderBrush' Value='" + accent + "'/>" +
                "<Setter TargetName='check' Property='Visibility' Value='Visible'/>" +
                "</Trigger>" +
                "<Trigger Property='IsMouseOver' Value='True'>" +
                "<Setter TargetName='box' Property='BorderBrush' Value='" + accent + "'/>" +
                "</Trigger>" +
                "</ControlTemplate.Triggers>" +
                "</ControlTemplate>";
            return (ControlTemplate)XamlReader.Parse(xaml);
        }

        private static string Hex(Color color)
        {
            return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }
    }
}
