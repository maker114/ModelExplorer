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

        public Brush BgBrush { get { return MakeBrush(Bg); } }
        public Brush SidebarBrush { get { return MakeBrush(Sidebar); } }
        public Brush PanelBrush { get { return MakeBrush(Panel); } }
        public Brush PanelActiveBrush { get { return MakeBrush(PanelActive); } }
        public Brush BorderBrush { get { return MakeBrush(Border); } }
        public Brush TextBrush { get { return MakeBrush(Text); } }
        public Brush MutedBrush { get { return MakeBrush(Muted); } }
        public Brush AccentBrush { get { return MakeBrush(Accent); } }
        public Brush AccentHoverBrush { get { return MakeBrush(AccentHover); } }
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

            Current = Presets[0];
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
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1.025' Duration='0:0:0.09'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1.025' Duration='0:0:0.09'/>" +
                "</Storyboard></BeginStoryboard></Trigger.EnterActions>" +
                "<Trigger.ExitActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1' Duration='0:0:0.12'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1' Duration='0:0:0.12'/>" +
                "</Storyboard></BeginStoryboard></Trigger.ExitActions>" +
                "</Trigger>" +
                "<Trigger Property='IsPressed' Value='True'>" +
                "<Trigger.EnterActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='0.97' Duration='0:0:0.06'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='0.97' Duration='0:0:0.06'/>" +
                "</Storyboard></BeginStoryboard></Trigger.EnterActions>" +
                "<Trigger.ExitActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1' Duration='0:0:0.08'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1' Duration='0:0:0.08'/>" +
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
                "</Storyboard></BeginStoryboard></Trigger.EnterActions>" +
                "<Trigger.ExitActions><BeginStoryboard><Storyboard>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1' Duration='0:0:0.08'/>" +
                "<DoubleAnimation Storyboard.TargetName='bd' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1' Duration='0:0:0.08'/>" +
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

            Border background = new Border
            {
                Background = theme.SidebarBrush,
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

            return new Border
            {
                Child = shell,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };
        }

        public static ControlTemplate ToggleTemplate()
        {
            string accent = Hex(ThemeManager.Current.Accent);
            string trackOff = "#3A4253";
            string xaml =
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='CheckBox'>" +
                "<Grid Width='48' Height='28'>" +
                "<Border x:Name='track' Width='46' Height='26' CornerRadius='13' Background='" + trackOff + "'/>" +
                "<Ellipse x:Name='thumb' Width='20' Height='20' Fill='#FFFFFF' HorizontalAlignment='Left' Margin='3,0,0,0'/>" +
                "<ContentPresenter Visibility='Collapsed'/>" +
                "</Grid>" +
                "<ControlTemplate.Triggers>" +
                "<Trigger Property='IsChecked' Value='True'>" +
                "<Setter TargetName='track' Property='Background' Value='" + accent + "'/>" +
                "<Setter TargetName='thumb' Property='HorizontalAlignment' Value='Right'/>" +
                "<Setter TargetName='thumb' Property='Margin' Value='0,0,3,0'/>" +
                "</Trigger>" +
                "</ControlTemplate.Triggers>" +
                "</ControlTemplate>";
            return (ControlTemplate)XamlReader.Parse(xaml);
        }

        public static ControlTemplate RoundedComboBoxTemplate()
        {
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
                "<Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}'" +
                " BorderThickness='0' CornerRadius='8' MinWidth='280'" +
                " MaxHeight='{TemplateBinding MaxDropDownHeight}' Padding='4'>" +
                "<ScrollViewer VerticalScrollBarVisibility='Auto' HorizontalScrollBarVisibility='Disabled' Background='Transparent'>" +
                "<ItemsPresenter/>" +
                "</ScrollViewer></Border></Popup>" +
                "</Grid></ControlTemplate>";
            return (ControlTemplate)XamlReader.Parse(xaml);
        }

        private static string Hex(Color color)
        {
            return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }
    }
}
