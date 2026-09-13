using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ModelExplorer
{
    /// <summary>
    /// 设置窗口。
    ///
    /// V3.4.2 起按参考的低饱和风格重排：左边是**分类导航**，右边每个分区是一张**卡片**
    /// （图标 + 标题 + 若干行），短选项改成**药丸分段**，配色预设改成**色板网格**。
    /// 只动版式与呈现方式，设置项本身、预览与保存逻辑与之前完全一致。
    /// </summary>
    public class SettingsWindow : Window
    {
        /// <summary>左侧分类导航的宽度。</summary>
        private const double NavWidth = 186;

        /// <summary>路径行右侧「浏览…」按钮的宽度。</summary>
        private const double ActionColumnWidth = 88;

        /// <summary>
        /// 背景图适配方式的显示标签与说明；token 顺序与 <see cref="AppConfig.BackgroundFitTokens"/> 一一对应，
        /// token 本身只在 AppConfig 里维护一份。
        /// </summary>
        private static readonly string[] BackgroundFitLabels = { "覆盖", "填充", "居中", "拉伸" };
        private static readonly string[] BackgroundFitHints =
        {
            "等比放大铺满窗口，超出部分裁掉，不留空。",
            "等比缩放到完整可见，四周用极光底色补齐。",
            "不缩放，原尺寸居中，四周用极光底色补齐。",
            "非等比拉伸铺满窗口，比例可能与原图不同。"
        };

        private readonly AppConfig _source;
        private TextBox _bambuPathBox;
        private TextBox _solidWorksPathBox;
        private ChoiceGroup _themeChoices;
        private Slider _fontSizeSlider;
        private TextBlock _fontSizeValue;
        private ChoiceGroup _stlUnitsChoices;
        private ChoiceGroup _stlQualityChoices;
        private ToggleSwitch _binaryStlSwitch;
        private ToggleSwitch _keepHistorySwitch;
        private ToggleSwitch _openBambuSwitch;
        private ToggleSwitch _organizeByFolderSwitch;
        private ToggleSwitch _glassSwitch;
        private Slider _glassBlurSlider;
        private Slider _glassOpacitySlider;
        private TextBlock _glassBlurValue;
        private TextBlock _glassOpacityValue;
        private bool _uiReady;
        private string _backgroundImage;
        private bool _backgroundSaved;
        private TextBlock _backgroundPathText;
        private TextBlock _backgroundFitHint;
        private Button _backgroundClearButton;
        private RadioButton[] _backgroundFitButtons;
        private Slider _backgroundDarkenSlider;
        private TextBlock _backgroundDarkenValue;
        private readonly List<NavVisual> _navVisuals = new List<NavVisual>();
        private readonly List<Page> _pages = new List<Page>();
        private bool _suppressThemePreview;

        public AppConfig Result { get; private set; }

        public SettingsWindow(AppConfig source)
        {
            _source = source;
            Result = null;

            Title = "设置";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            // 参考的版式是「左导航 + 右卡片」，比原来的单栏更宽也更矮
            Width = 880;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Icon = AppIcon.WindowIcon;

            BuildUi();
            UiAnimation.FadeInOnOpen(this);
        }

        // ------------------------------------------------------------------ 版式骨架

        private void BuildUi()
        {
            AppTheme theme = ThemeManager.Current;

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(62) });

            Content = UiFactory.CreateWindowChrome(root, theme);
            ((Border)Content).MouseLeftButtonDown += SettingsWindow_MouseLeftButtonDown;

            Grid header = new Grid { Margin = new Thickness(24, 0, 10, 0) };
            header.Children.Add(new TextBlock
            {
                Text = "设置",
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 19,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            });
            Button closeButton = UiFactory.MakeChromeButton("×", delegate { Close(); });
            closeButton.HorizontalAlignment = HorizontalAlignment.Right;
            closeButton.VerticalAlignment = VerticalAlignment.Center;
            header.Children.Add(closeButton);
            root.Children.Add(header);
            Grid.SetRow(header, 0);

            Grid body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(NavWidth) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.Children.Add(body);
            Grid.SetRow(body, 1);

            StackPanel nav = new StackPanel { Margin = new Thickness(12, 2, 8, 0) };
            Grid.SetColumn(nav, 0);
            body.Children.Add(nav);

            // V3.4.7：原先导航与内容之间有一条 1px 竖分隔线。它要么铺满内容区高度、
            // 下半屏拖出一道空档，要么在毛玻璃下从半透明面板里透出来，怎么算长度都不好看，
            // 用户也明确说不要了——左右两列靠留白（导航右 8 + 内容左 20）区分即可，直接删掉。

            Grid pages = new Grid { Margin = new Thickness(20, 0, 18, 0) };
            Grid.SetColumn(pages, 1);
            body.Children.Add(pages);

            _pages.Clear();
            _pages.Add(NewPage(nav, pages, Icons.Sliders, "外观", theme));
            _pages.Add(NewPage(nav, pages, Icons.Window, "外部程序", theme));
            _pages.Add(NewPage(nav, pages, Icons.Download, "导出", theme));
            _pages.Add(NewPage(nav, pages, Icons.Folder, "整理", theme));

            BuildAppearancePage(_pages[0].Body, theme);
            BuildProgramsPage(_pages[1].Body, theme);
            BuildExportPage(_pages[2].Body, theme);
            BuildOrganizePage(_pages[3].Body, theme);

            _pages[0].NavButton.IsChecked = true;

            StackPanel footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(16, 0, 24, 0)
            };
            Button cancelButton = MakeButton("取消", delegate { Close(); }, false, 90, 34);
            cancelButton.Margin = new Thickness(0, 0, 10, 0);
            footer.Children.Add(cancelButton);
            footer.Children.Add(MakeButton("保存", Save_Click, true, 90, 34));
            root.Children.Add(footer);
            Grid.SetRow(footer, 2);

            // 控件都建完了才允许「实时预览」：滑杆初始化时会触发 ValueChanged，
            // 那时背景图分区的控件还不存在
            _uiReady = true;
        }

        /// <summary>建一个分类页：导航项 + 该页的滚动容器，导航切换时切可见性。</summary>
        private Page NewPage(StackPanel nav, Grid pages, string icon, string title, AppTheme theme)
        {
            TextBlock label = new TextBlock
            {
                Text = title,
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Path glyph = UiFactory.IconPath(icon, 15, theme.MutedBrush);

            Grid content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(glyph, 0);
            content.Children.Add(glyph);
            Grid.SetColumn(label, 1);
            content.Children.Add(label);

            RadioButton button = new RadioButton
            {
                GroupName = "SettingsNav",
                Content = content,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 4),
                Template = UiFactory.NavItemTemplate()
            };

            StackPanel body = new StackPanel { Margin = new Thickness(0, 6, 0, 10) };
            ScrollViewer view = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Visibility = Visibility.Collapsed,
                Content = body
            };
            pages.Children.Add(view);

            NavVisual visual = new NavVisual { Button = button, Label = label, Glyph = glyph };
            _navVisuals.Add(visual);
            button.Checked += delegate
            {
                foreach (UIElement child in pages.Children)
                {
                    child.Visibility = Visibility.Collapsed;
                }
                view.Visibility = Visibility.Visible;
                foreach (NavVisual item in _navVisuals)
                {
                    bool active = item.Button == button;
                    item.Label.Foreground = active ? theme.TextBrush : theme.MutedBrush;
                    item.Glyph.Stroke = active ? theme.AccentBrush : theme.MutedBrush;
                }
            };

            nav.Children.Add(button);
            return new Page { NavButton = button, Body = body, View = view };
        }

        // ------------------------------------------------------------------ 各分类页

        /// <summary>外观：配色预设、毛玻璃、背景图、字体大小。</summary>
        private void BuildAppearancePage(StackPanel page, AppTheme theme)
        {
            Card themeCard = NewCard(page, Icons.Sliders, "配色预设", theme);
            _themeChoices = NewChoiceGroup(
                themeCard.Body,
                theme,
                3,
                CollectPresetNames(),
                _source.Theme,
                true);

            Card glassCard = NewCard(page, Icons.Droplet, "毛玻璃", theme);
            _glassSwitch = new ToggleSwitch
            {
                IsChecked = _source.UseGlass,
                VerticalAlignment = VerticalAlignment.Center
            };
            AddToggleRow(glassCard.Body, "毛玻璃效果",
                "半透明玻璃面板 + 极光背景。关闭后回到纯色界面，低配机器或远程桌面下可关掉。",
                _glassSwitch, theme);

            _glassBlurSlider = MakeSlider(0, AppConfig.MaxGlassBlur);
            _glassBlurSlider.Value = _source.GlassBlurValue;
            _glassBlurValue = MakeValueLabel(theme);
            _glassBlurSlider.ValueChanged += delegate
            {
                _glassBlurValue.Text = ((int)Math.Round(_glassBlurSlider.Value)) + " px";
                PreviewGlass();
            };
            AddSliderRow(glassCard.Body, "模糊", _glassBlurSlider, _glassBlurValue, theme);
            AddHint(glassCard.Body, "作用在整层背景上：极光与自定义背景图一起变糊。0 = 不模糊。", theme);

            _glassOpacitySlider = MakeSlider(0, 100);
            _glassOpacitySlider.Value = _source.GlassOpacityValue;
            _glassOpacityValue = MakeValueLabel(theme);
            _glassOpacitySlider.ValueChanged += delegate
            {
                _glassOpacityValue.Text = ((int)Math.Round(_glassOpacitySlider.Value)) + " %";
                PreviewGlass();
            };
            AddSliderRow(glassCard.Body, "透明度", _glassOpacitySlider, _glassOpacityValue, theme);
            AddHint(glassCard.Body, "越大面板越透、越能看见背景；0 = 面板不透明，背景被完全挡住。", theme);

            _glassBlurValue.Text = ((int)Math.Round(_glassBlurSlider.Value)) + " px";
            _glassOpacityValue.Text = ((int)Math.Round(_glassOpacitySlider.Value)) + " %";
            SetGlassControlsEnabled(_source.UseGlass);
            // 开关要**实时生效**，和两个滑杆一致：此前它只改滑杆的可用状态，
            // 关掉再打开时主题的透明度还停在 0，界面就一直是实色的
            // （看着像「透明度被重置了」，用户反馈）。
            _glassSwitch.Checked += delegate
            {
                SetGlassControlsEnabled(true);
                PreviewGlass();
            };
            _glassSwitch.Unchecked += delegate
            {
                SetGlassControlsEnabled(false);
                PreviewGlass();
            };

            BuildBackgroundCard(page, theme);

            Card fontCard = NewCard(page, Icons.Letter, "字体大小", theme);
            _fontSizeSlider = MakeSlider(9, 16);
            _fontSizeSlider.Value = _source.FontSize;
            _fontSizeValue = MakeValueLabel(theme);
            _fontSizeSlider.ValueChanged += delegate
            {
                _fontSizeValue.Text = ((int)Math.Round(_fontSizeSlider.Value)).ToString();
            };
            AddSliderRow(fontCard.Body, "字号", _fontSizeSlider, _fontSizeValue, theme);
            AddHint(fontCard.Body, "主界面与侧栏的字号按这个值整体缩放（保存后生效）。", theme);
            _fontSizeValue.Text = ((int)Math.Round(_fontSizeSlider.Value)).ToString();
        }

        /// <summary>外部程序：Bambu Studio 与 SolidWorks 的路径。</summary>
        private void BuildProgramsPage(StackPanel page, AppTheme theme)
        {
            Card bambuCard = NewCard(page, Icons.Window, "Bambu Studio", theme);
            AddHint(bambuCard.Body, "设置 bambu-studio.exe 的完整路径，适配不同电脑。", theme);
            _bambuPathBox = NewPathBox(_source.BambuPath, theme);
            AddPathRow(bambuCard.Body, _bambuPathBox, "浏览...", Browse_Click, theme);

            Card solidWorksCard = NewCard(page, Icons.Window, "SolidWorks", theme);
            AddHint(solidWorksCard.Body, "设置 SLDWORKS.exe 的完整路径，用于 API 调用。", theme);
            _solidWorksPathBox = NewPathBox(_source.SolidWorksPath, theme);
            AddPathRow(solidWorksCard.Body, _solidWorksPathBox, "浏览...", BrowseSolidWorks_Click, theme);
        }

        /// <summary>导出：STL 导出参数与转换设置。</summary>
        private void BuildExportPage(StackPanel page, AppTheme theme)
        {
            Card stlCard = NewCard(page, Icons.Download, "STL 导出", theme);
            AddHint(stlCard.Body, "与 SolidWorks 插件、命令行工具共用同一份配置。", theme);

            _binaryStlSwitch = new ToggleSwitch
            {
                IsChecked = _source.UseBinaryStl,
                VerticalAlignment = VerticalAlignment.Center
            };
            AddToggleRow(stlCard.Body, "使用二进制 STL",
                "开启时导出二进制 STL，体积小、加载快；关闭后导出 ASCII 文本格式，体积约为二进制的 5～10 倍。",
                _binaryStlSwitch, theme);

            _stlUnitsChoices = NewChoiceGroup(
                stlCard.Body,
                theme,
                4,
                new[] { "mm", "cm", "m", "in" },
                SolidWorksStlExporter.NormalizeUnits(_source.StlUnits),
                false,
                "STL 单位");
            _stlQualityChoices = NewChoiceGroup(
                stlCard.Body,
                theme,
                4,
                StlQualityLabels.DisplayNames,
                StlQualityLabels.ToDisplay(_source.StlQuality),
                false,
                "STL 质量");

            Card convertCard = NewCard(page, Icons.Swap, "转换设置", theme);
            _keepHistorySwitch = new ToggleSwitch
            {
                IsChecked = _source.KeepHistory,
                VerticalAlignment = VerticalAlignment.Center
            };
            AddToggleRow(convertCard.Body, "保留历史版本",
                "同名 STL 已存在时生成带时间戳的新文件，而不是直接覆盖。",
                _keepHistorySwitch, theme);

            _openBambuSwitch = new ToggleSwitch
            {
                IsChecked = _source.OpenBambu,
                VerticalAlignment = VerticalAlignment.Center
            };
            AddToggleRow(convertCard.Body, "导出后打开 Bambu Studio",
                "导出完成后自动启动 Bambu Studio 并载入刚导出的 STL。",
                _openBambuSwitch, theme);
        }

        /// <summary>整理：按文件夹整理。</summary>
        private void BuildOrganizePage(StackPanel page, AppTheme theme)
        {
            Card card = NewCard(page, Icons.Folder, "文件整理", theme);
            _organizeByFolderSwitch = new ToggleSwitch
            {
                IsChecked = _source.OrganizeByFolder,
                VerticalAlignment = VerticalAlignment.Center
            };
            AddToggleRow(card.Body, "按文件夹整理",
                "开启时在每个文件所在目录下分别使用「STL文件夹 / 3MF文件夹」；" +
                "关闭时把散落的 STL / 3MF 集中到工程根目录的这两个文件夹。",
                _organizeByFolderSwitch, theme);
        }

        /// <summary>背景图卡片：选图 / 清除 / 适配 / 暗化。</summary>
        private void BuildBackgroundCard(StackPanel page, AppTheme theme)
        {
            Card card = NewCard(page, Icons.Image, "背景图片", theme);
            _backgroundImage = _source.BackgroundImage ?? "";

            _backgroundPathText = new TextBlock
            {
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            AddDisplayRow(card.Body, _backgroundPathText, "选择图片…", BrowseBackground_Click, theme);
            _backgroundClearButton = MakeButton("清除背景图", ClearBackground_Click, false, 110, 30);
            _backgroundClearButton.HorizontalAlignment = HorizontalAlignment.Left;
            _backgroundClearButton.Margin = new Thickness(0, 10, 0, 0);
            card.Body.Children.Add(_backgroundClearButton);

            AddSectionLabel(card.Body, "适配", theme);
            Grid fitRow = new Grid();
            _backgroundFitButtons = new RadioButton[AppConfig.BackgroundFitTokens.Length];
            for (int i = 0; i < AppConfig.BackgroundFitTokens.Length; i++)
            {
                bool first = i == 0;
                bool last = i == AppConfig.BackgroundFitTokens.Length - 1;
                RadioButton segment = new RadioButton
                {
                    Content = BackgroundFitLabels[i],
                    GroupName = "BackdropFit",
                    Foreground = theme.TextBrush,
                    FontFamily = new FontFamily("Microsoft YaHei UI"),
                    FontSize = 13,
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(first ? 0 : 5, 0, last ? 0 : 5, 0),
                    Template = UiFactory.SegmentTemplate()
                };
                segment.Checked += BackgroundFit_Checked;
                fitRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Grid.SetColumn(segment, i);
                fitRow.Children.Add(segment);
                _backgroundFitButtons[i] = segment;
            }
            card.Body.Children.Add(fitRow);

            _backgroundFitHint = new TextBlock
            {
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            };
            card.Body.Children.Add(_backgroundFitHint);

            _backgroundDarkenSlider = MakeSlider(0, AppConfig.MaxBackgroundDarken);
            _backgroundDarkenSlider.Value = _source.BackgroundDarkenValue;
            _backgroundDarkenValue = MakeValueLabel(theme);
            _backgroundDarkenSlider.ValueChanged += delegate
            {
                _backgroundDarkenValue.Text = ((int)Math.Round(_backgroundDarkenSlider.Value)) + " %";
                PreviewBackground();
            };
            AddSliderRow(card.Body, "暗化", _backgroundDarkenSlider, _backgroundDarkenValue, theme, 14);
            AddHint(card.Body,
                "压暗背景以保证面板上的小字仍看得清。设得比可读性下限更暗时以你的设置为准，" +
                "更亮时自动补足到下限；模糊在「毛玻璃」卡片里调，两者共用同一个值。",
                theme);
            _backgroundDarkenValue.Text = ((int)Math.Round(_backgroundDarkenSlider.Value)) + " %";

            RefreshBackgroundUi(theme, true);
        }

        // ------------------------------------------------------------------ 卡片与行

        private Card NewCard(StackPanel page, string icon, string title, AppTheme theme)
        {
            StackPanel inner = new StackPanel();
            StackPanel head = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 14)
            };
            head.Children.Add(UiFactory.IconPath(icon, 15, theme.AccentBrush, 1.5));
            head.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            inner.Children.Add(head);

            StackPanel body = new StackPanel();
            inner.Children.Add(body);

            Border card = new Border
            {
                Background = theme.PanelBrush,
                BorderBrush = theme.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16, 14, 16, 16),
                Margin = new Thickness(0, 0, 0, 14),
                Child = inner
            };
            page.Children.Add(card);
            return new Card { Body = body };
        }

        private static void AddHint(StackPanel body, string text, AppTheme theme)
        {
            body.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            });
        }

        private static void AddSectionLabel(StackPanel body, string text, AppTheme theme)
        {
            body.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13,
                Margin = new Thickness(0, 14, 0, 8)
            });
        }

        /// <summary>「图标 + 标题 + 说明 + 开关」一行，对应参考图里的设置项。</summary>
        private void AddToggleRow(StackPanel body, string title, string description, ToggleSwitch toggle, AppTheme theme)
        {
            Grid row = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel text = new StackPanel();
            text.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13
            });
            text.Children.Add(new TextBlock
            {
                Text = description,
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 16, 0)
            });
            row.Children.Add(text);

            Grid.SetColumn(toggle, 1);
            row.Children.Add(toggle);
            body.Children.Add(row);
        }

        /// <summary>路径输入行：文本框外壳 + 右侧按钮。</summary>
        private void AddPathRow(StackPanel body, TextBox box, string buttonText, RoutedEventHandler handler, AppTheme theme)
        {
            Grid row = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(NewInputShell(box, theme));

            Button browse = MakeButton(buttonText, handler, false, ActionColumnWidth, 34);
            browse.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(browse, 1);
            row.Children.Add(browse);
            body.Children.Add(row);
        }

        /// <summary>背景图那一行：左边是只读的路径文字，用同一套外壳。</summary>
        private void AddDisplayRow(StackPanel body, TextBlock display, string buttonText, RoutedEventHandler handler, AppTheme theme)
        {
            Grid row = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new Border
            {
                Background = theme.PanelBrush,
                BorderBrush = theme.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(12, 9, 12, 9),
                Child = display
            });

            Button browse = MakeButton(buttonText, handler, false, ActionColumnWidth, 36);
            browse.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(browse, 1);
            row.Children.Add(browse);
            body.Children.Add(row);
        }

        private static TextBox NewPathBox(string text, AppTheme theme)
        {
            return new TextBox
            {
                Text = text,
                Background = Brushes.Transparent,
                Foreground = theme.TextBrush,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                CaretBrush = theme.TextBrush,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                VerticalContentAlignment = VerticalAlignment.Center
            };
        }

        private static Border NewInputShell(TextBox box, AppTheme theme)
        {
            return new Border
            {
                Background = theme.PanelBrush,
                BorderBrush = theme.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(12, 9, 12, 9),
                Child = box
            };
        }

        /// <summary>「标题 + 滑杆 + 数值」一行。</summary>
        private void AddSliderRow(StackPanel body, string title, Slider slider, TextBlock value, AppTheme theme)
        {
            AddSliderRow(body, title, slider, value, theme, 6);
        }

        private void AddSliderRow(StackPanel body, string title, Slider slider, TextBlock value, AppTheme theme, double topMargin)
        {
            Grid row = new Grid { Margin = new Thickness(0, topMargin, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });

            TextBlock label = new TextBlock
            {
                Text = title,
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(label, 0);
            row.Children.Add(label);
            Grid.SetColumn(slider, 1);
            row.Children.Add(slider);
            Grid.SetColumn(value, 2);
            row.Children.Add(value);
            body.Children.Add(row);
        }

        /// <summary>
        /// 一组互斥的选项按钮（药丸或色板网格）。参考图里短选项都用这种平铺按钮而不是下拉框，
        /// 少一次展开点击，也能一眼看全选项。
        /// </summary>
        private ChoiceGroup NewChoiceGroup(
            StackPanel body,
            AppTheme theme,
            int columns,
            IList<string> values,
            string selected,
            bool swatch,
            string title = null)
        {
            if (title != null)
            {
                AddSectionLabel(body, title, theme);
            }

            ChoiceGroup group = new ChoiceGroup { Values = values };
            group.Buttons = new RadioButton[values.Count];

            // 组名必须**一组一个**：写在循环里会变成每个按钮一个组名，
            // WPF 的互斥只在同名组内生效，那样就变成可以多选（曾因此出现三个同时选中）。
            string groupName = "Choice" + Guid.NewGuid().ToString("N");

            Grid grid = new Grid { Margin = new Thickness(0, title == null ? 2 : 0, 0, 0) };
            for (int i = 0; i < columns; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            int rows = (values.Count + columns - 1) / columns;
            for (int r = 0; r < rows; r++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            for (int i = 0; i < values.Count; i++)
            {
                int column = i % columns;
                int row = i / columns;
                RadioButton button = new RadioButton
                {
                    Content = swatch ? BuildSwatchContent(values[i], theme) : BuildPillContent(values[i], theme),
                    GroupName = groupName,
                    Foreground = theme.TextBrush,
                    Cursor = Cursors.Hand,
                    Template = swatch ? UiFactory.SwatchTemplate() : UiFactory.SegmentTemplate(),
                    Margin = new Thickness(column == 0 ? 0 : 5, 0, 0, row == rows - 1 ? 0 : 8),
                    Tag = i
                };
                if (swatch)
                {
                    // 配色要**实时应用**：选中即换肤，不必先保存
                    button.Checked += delegate
                    {
                        if (!_uiReady || _suppressThemePreview)
                        {
                            return;
                        }
                        string name = values[(int)button.Tag];
                        if (name != ThemeManager.Current.Name)
                        {
                            ApplyThemeLive(name);
                        }
                    };
                }
                Grid.SetColumn(button, column);
                Grid.SetRow(button, row);
                grid.Children.Add(button);
                group.Buttons[i] = button;
            }

            body.Children.Add(grid);
            group.SelectedValue = selected;
            return group;
        }

        private static FrameworkElement BuildPillContent(string text, AppTheme theme)
        {
            return new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private static FrameworkElement BuildSwatchContent(string presetName, AppTheme theme)
        {
            Grid content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            AppTheme preset = ThemeManager.Presets.Find(t => t.Name == presetName) ?? ThemeManager.Current;
            Ellipse dot = new Ellipse
            {
                Width = 11,
                Height = 11,
                Fill = preset.AccentBrush,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(dot, 0);
            content.Children.Add(dot);

            TextBlock label = new TextBlock
            {
                Text = presetName,
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(label, 1);
            content.Children.Add(label);
            return content;
        }

        private static string[] CollectPresetNames()
        {
            string[] names = new string[ThemeManager.Presets.Count];
            for (int i = 0; i < ThemeManager.Presets.Count; i++)
            {
                names[i] = ThemeManager.Presets[i].Name;
            }
            return names;
        }

        // ------------------------------------------------------------------ 实时换肤

        /// <summary>
        /// 换配色时重建整个界面。
        ///
        /// 为什么必须重建：面板色、文字色是构造时赋的本地值，而药丸 / 色板 / 滑杆 / 开关的
        /// 模板更是把颜色写成了创建时的字面量——不重建，模板里的强调色不会跟着换。
        /// 重建前把当前所有控件的值抓成一份快照，重建后再灌回去，预览状态不丢。
        /// </summary>
        private void ApplyThemeLive(string name)
        {
            PreviewState state = CaptureState();
            state.Theme = name;

            ThemeManager.Apply(name);
            ThemeManager.ApplyGlass(_glassSwitch.IsChecked == true, (int)Math.Round(_glassOpacitySlider.Value));
            RefreshAppScrollBarResources();
            Glass.Configure(CurrentBackdropSettings());
            Glass.Invalidate();

            RebuildUi(state);
        }

        /// <summary>滚动条用的是应用级资源（主窗口负责设置），换肤时一并刷新，免得还是旧配色。</summary>
        private static void RefreshAppScrollBarResources()
        {
            Application app = Application.Current;
            if (app == null)
            {
                return;
            }

            AppTheme theme = ThemeManager.Current;
            app.Resources["ScrollBarTrackBrush"] = theme.CodeBrush;
            app.Resources["ScrollBarThumbBrush"] = theme.BorderBrush;
            app.Resources["ScrollBarThumbHoverBrush"] = theme.PanelActiveBrush;
            app.Resources["ScrollBarThumbPressedBrush"] = theme.AccentBrush;
        }

        private void RebuildUi(PreviewState state)
        {
            _uiReady = false;
            _navVisuals.Clear();
            BuildUi();
            _uiReady = true;
            ApplyState(state);
        }

        private PreviewState CaptureState()
        {
            PreviewState state = new PreviewState();
            state.NavIndex = 0;
            for (int i = 0; i < _pages.Count; i++)
            {
                if (_pages[i].NavButton.IsChecked == true)
                {
                    state.NavIndex = i;
                    state.PageScroll = _pages[i].View.VerticalOffset;
                    break;
                }
            }

            state.Theme = _themeChoices.SelectedValue;
            state.FontSize = (int)Math.Round(_fontSizeSlider.Value);
            state.GlassOn = _glassSwitch.IsChecked == true;
            state.GlassBlur = _glassBlurSlider.Value;
            state.GlassOpacity = _glassOpacitySlider.Value;
            state.BackgroundImage = _backgroundImage;
            state.BackgroundFit = SelectedFitIndex();
            state.BackgroundDarken = _backgroundDarkenSlider.Value;
            state.BinaryStl = _binaryStlSwitch.IsChecked == true;
            state.KeepHistory = _keepHistorySwitch.IsChecked == true;
            state.OpenBambu = _openBambuSwitch.IsChecked == true;
            state.OrganizeByFolder = _organizeByFolderSwitch.IsChecked == true;
            state.BambuPath = _bambuPathBox.Text;
            state.SolidWorksPath = _solidWorksPathBox.Text;
            state.StlUnits = _stlUnitsChoices.SelectedValue;
            state.StlQuality = _stlQualityChoices.SelectedValue;
            return state;
        }

        private void ApplyState(PreviewState state)
        {
            _suppressThemePreview = true;
            try
            {
                _themeChoices.SelectedValue = state.Theme;
                _fontSizeSlider.Value = state.FontSize;
                _glassSwitch.IsChecked = state.GlassOn;
                _glassBlurSlider.Value = state.GlassBlur;
                _glassOpacitySlider.Value = state.GlassOpacity;
                _backgroundImage = state.BackgroundImage ?? "";
                _backgroundFitButtons[state.BackgroundFit].IsChecked = true;
                _backgroundDarkenSlider.Value = state.BackgroundDarken;
                _binaryStlSwitch.IsChecked = state.BinaryStl;
                _keepHistorySwitch.IsChecked = state.KeepHistory;
                _openBambuSwitch.IsChecked = state.OpenBambu;
                _organizeByFolderSwitch.IsChecked = state.OrganizeByFolder;
                _bambuPathBox.Text = state.BambuPath;
                _solidWorksPathBox.Text = state.SolidWorksPath;
                _stlUnitsChoices.SelectedValue = state.StlUnits;
                _stlQualityChoices.SelectedValue = state.StlQuality;
                _backgroundClearButton.IsEnabled = !string.IsNullOrEmpty(_backgroundImage);
                UpdateBackgroundPathText(ThemeManager.Current);
            }
            finally
            {
                _suppressThemePreview = false;
            }

            int index = state.NavIndex;
            if (index < 0 || index >= _pages.Count)
            {
                index = 0;
            }
            _pages[index].NavButton.IsChecked = true;
            if (state.PageScroll > 0)
            {
                _pages[index].View.ScrollToVerticalOffset(state.PageScroll);
            }
        }

        /// <summary>切换配色前的界面快照，重建后照它恢复。</summary>
        private sealed class PreviewState
        {
            public int NavIndex { get; set; }
            public double PageScroll { get; set; }
            public string Theme { get; set; }
            public int FontSize { get; set; }
            public bool GlassOn { get; set; }
            public double GlassBlur { get; set; }
            public double GlassOpacity { get; set; }
            public string BackgroundImage { get; set; }
            public int BackgroundFit { get; set; }
            public double BackgroundDarken { get; set; }
            public bool BinaryStl { get; set; }
            public bool KeepHistory { get; set; }
            public bool OpenBambu { get; set; }
            public bool OrganizeByFolder { get; set; }
            public string BambuPath { get; set; }
            public string SolidWorksPath { get; set; }
            public string StlUnits { get; set; }
            public string StlQuality { get; set; }
        }

        // ------------------------------------------------------------------ 预览与保存

        /// <summary>毛玻璃两个滑杆的实时预览：改的是全局主题画刷与背景层，不写配置。</summary>
        private void PreviewGlass()
        {
            if (!_uiReady)
            {
                return;
            }

            ThemeManager.ApplyGlass(
                _glassSwitch.IsChecked == true,
                (int)Math.Round(_glassOpacitySlider.Value));
            Glass.Configure(CurrentBackdropSettings());
            Glass.Invalidate();
        }

        /// <summary>把当前控件状态交给 Glass 立刻重画背景（不写配置）。</summary>
        private void PreviewBackground()
        {
            if (!_uiReady)
            {
                return;
            }

            Glass.Configure(CurrentBackdropSettings());
            Glass.Invalidate();
        }

        private BackdropSettings CurrentBackdropSettings()
        {
            return new BackdropSettings
            {
                ImagePath = string.IsNullOrEmpty(_backgroundImage) ? null : _backgroundImage,
                Fit = BackdropSettings.ParseFit(AppConfig.BackgroundFitTokens[SelectedFitIndex()]),
                Blur = (int)Math.Round(_glassBlurSlider.Value),
                Darken = (int)Math.Round(_backgroundDarkenSlider.Value)
            };
        }

        private void SetGlassControlsEnabled(bool enabled)
        {
            _glassBlurSlider.IsEnabled = enabled;
            _glassOpacitySlider.IsEnabled = enabled;
        }

        /// <summary>取消或直接关窗时把预览还原成保存前的配置，避免界面与 config.json 不一致。</summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (!_backgroundSaved)
            {
                // 实时换肤可能已经把主题改掉了，取消时要连主题一起还原
                ThemeManager.Apply(_source.Theme);
                ThemeManager.ApplyGlass(_source.UseGlass, _source.GlassOpacityValue);
                RefreshAppScrollBarResources();
                Glass.Configure(_source);
                Glass.Invalidate();
            }
        }

        private void RefreshBackgroundUi(AppTheme theme, bool loadFromConfig)
        {
            if (loadFromConfig)
            {
                int index = Array.IndexOf(AppConfig.BackgroundFitTokens, _source.BackgroundFitValue);
                if (index < 0)
                {
                    index = 0;
                }
                _backgroundFitButtons[index].IsChecked = true;
                _backgroundFitHint.Text = BackgroundFitHints[index];
            }

            _backgroundDarkenValue.Text = ((int)Math.Round(_backgroundDarkenSlider.Value)) + " %";
            _backgroundClearButton.IsEnabled = !string.IsNullOrEmpty(_backgroundImage);
            UpdateBackgroundPathText(theme);
        }

        private void UpdateBackgroundPathText(AppTheme theme)
        {
            if (string.IsNullOrEmpty(_backgroundImage))
            {
                _backgroundPathText.Text = "未设置（只用极光背景）";
                _backgroundPathText.Foreground = theme.MutedBrush;
                return;
            }

            bool exists = System.IO.File.Exists(_backgroundImage);
            _backgroundPathText.Text = exists
                ? _backgroundImage
                : _backgroundImage + "　（文件不存在，已忽略）";
            _backgroundPathText.Foreground = exists ? theme.TextBrush : theme.ErrorBrush;
        }

        private void BrowseBackground_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Title = "选择背景图片";
                dialog.Filter = "图片|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|所有文件|*.*";
                dialog.CheckFileExists = true;
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                _backgroundImage = dialog.FileName;

                // 第一次选图时如果暗化还是 0，给个默认值：不然亮壁纸上的小字立刻难读
                if (_backgroundDarkenSlider.Value <= 0)
                {
                    _backgroundDarkenSlider.Value = AppConfig.DefaultBackgroundDarken;
                }

                AppTheme theme = ThemeManager.Current;
                _backgroundClearButton.IsEnabled = true;
                UpdateBackgroundPathText(theme);
                PreviewBackground();
            }
        }

        private void ClearBackground_Click(object sender, RoutedEventArgs e)
        {
            _backgroundImage = "";
            _backgroundClearButton.IsEnabled = false;
            UpdateBackgroundPathText(ThemeManager.Current);
            PreviewBackground();
        }

        private void BackgroundFit_Checked(object sender, RoutedEventArgs e)
        {
            int index = Array.IndexOf(_backgroundFitButtons, sender as RadioButton);
            if (index >= 0)
            {
                _backgroundFitHint.Text = BackgroundFitHints[index];
            }
            PreviewBackground();
        }

        private int SelectedFitIndex()
        {
            for (int i = 0; i < _backgroundFitButtons.Length; i++)
            {
                if (_backgroundFitButtons[i].IsChecked == true)
                {
                    return i;
                }
            }
            return 0;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Title = "选择 Bambu Studio";
                dialog.Filter = "Bambu Studio|bambu-studio.exe|可执行文件|*.exe|所有文件|*.*";
                dialog.CheckFileExists = true;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _bambuPathBox.Text = dialog.FileName;
                }
            }
        }

        private void BrowseSolidWorks_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Title = "选择 SolidWorks";
                dialog.Filter = "SolidWorks|SLDWORKS.exe|可执行文件|*.exe|所有文件|*.*";
                dialog.CheckFileExists = true;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _solidWorksPathBox.Text = dialog.FileName;
                }
            }
        }

        private void SettingsWindow_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // 滑杆范围就是 9～16，取整后即为合法字号
            int fontSize = (int)Math.Round(_fontSizeSlider.Value);

            Result = new AppConfig
            {
                LastDir = _source.LastDir,
                KeepHistory = _keepHistorySwitch.IsChecked == true,
                OpenBambu = _openBambuSwitch.IsChecked == true,
                OrganizeByFolder = _organizeByFolderSwitch.IsChecked == true,
                BinaryStl = _binaryStlSwitch.IsChecked == true,
                StlUnits = _stlUnitsChoices.SelectedValue,
                StlQuality = StlQualityLabels.ToToken(_stlQualityChoices.SelectedValue),
                BambuPath = _bambuPathBox.Text.Trim(),
                SolidWorksPath = _solidWorksPathBox.Text.Trim(),
                Theme = _themeChoices.SelectedValue,
                FontSize = fontSize,
                Glass = _glassSwitch.IsChecked == true,
                // 3.4.0 起用滑杆：旧的三档强度不再写，读取时只作迁移用
                GlassBlur = (int)Math.Round(_glassBlurSlider.Value),
                GlassOpacity = (int)Math.Round(_glassOpacitySlider.Value),
                BackgroundImage = _backgroundImage ?? "",
                BackgroundFit = AppConfig.BackgroundFitTokens[SelectedFitIndex()],
                BackgroundDarken = (int)Math.Round(_backgroundDarkenSlider.Value),
                ProjectNameUnchecked = _source.ProjectNameUnchecked ?? new List<string>()
            };
            // 保存后主窗口会用新配置重建，这里不要再把预览还原回去
            _backgroundSaved = true;
            DialogResult = true;
        }

        // ------------------------------------------------------------------ 小工具

        private static Slider MakeSlider(double minimum, double maximum)
        {
            return new Slider
            {
                Minimum = minimum,
                Maximum = maximum,
                IsMoveToPointEnabled = true,
                IsSnapToTickEnabled = true,
                TickFrequency = 1,
                Height = 22,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0),
                Template = UiFactory.SliderTemplate()
            };
        }

        private static TextBlock MakeValueLabel(AppTheme theme)
        {
            return new TextBlock
            {
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static Button MakeButton(string text, RoutedEventHandler handler, bool primary, double width, double height)
        {
            Button button = new Button
            {
                Content = text,
                Width = width,
                Height = height,
                Background = primary ? ThemeManager.Current.AccentBrush : ThemeManager.Current.PanelActiveBrush,
                Foreground = primary ? new SolidColorBrush(Color.FromRgb(0x15, 0x15, 0x15)) : ThemeManager.Current.TextBrush,
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13,
                FontWeight = primary ? FontWeights.Bold : FontWeights.Normal,
                Cursor = Cursors.Hand
            };
            button.Template = UiFactory.RoundedButtonTemplate(primary);
            button.Click += handler;
            return button;
        }

        /// <summary>设置窗用的线性图标（16×16 视窗，纯描边）。</summary>
        private static class Icons
        {
            public const string Sliders = "M 2,5 H 14 M 2,11 H 14 M 5.5,3.2 V 6.8 M 10.5,9.2 V 12.8";
            public const string Droplet = "M 8,1.9 C 8,1.9 3.5,7.4 3.5,10.2 A 4.5,4.5 0 0 0 12.5,10.2 C 12.5,7.4 8,1.9 8,1.9 Z";
            public const string Image = "M 2.2,3.6 H 13.8 V 12.4 H 2.2 Z M 2.2,10 L 6,6.6 L 8.6,9 L 10.6,7.4 L 13.8,10.2";
            public const string Letter = "M 3,13 L 6.4,3.4 L 9.8,13 M 4.4,9.6 H 8.4";
            public const string Window = "M 2.2,3.6 H 13.8 V 12.4 H 2.2 Z M 2.2,6.6 H 13.8";
            public const string Download = "M 8,2.2 V 9.8 M 4.6,6.6 L 8,10 L 11.4,6.6 M 2.6,13.4 H 13.4";
            public const string Swap = "M 2.6,5.6 H 12 M 9.4,3.2 L 11.8,5.6 L 9.4,8 M 13.4,10.4 H 4 M 6.6,8 L 4.2,10.4 L 6.6,12.8";
            public const string Folder = "M 2.2,4.6 H 6.4 L 7.8,6.6 H 13.8 V 12.4 H 2.2 Z";
        }

        /// <summary>左侧导航项的三件套，用来在切换时统一改选中态颜色。</summary>
        private sealed class NavVisual
        {
            public RadioButton Button { get; set; }
            public TextBlock Label { get; set; }
            public Path Glyph { get; set; }
        }

        private sealed class Page
        {
            public RadioButton NavButton { get; set; }
            public StackPanel Body { get; set; }
            public ScrollViewer View { get; set; }
        }

        private sealed class Card
        {
            public StackPanel Body { get; set; }
        }

        /// <summary>一组互斥选项（药丸或色板）。</summary>
        private sealed class ChoiceGroup
        {
            public IList<string> Values { get; set; }
            public RadioButton[] Buttons { get; set; }

            public int SelectedIndex
            {
                get
                {
                    for (int i = 0; i < Buttons.Length; i++)
                    {
                        if (Buttons[i].IsChecked == true)
                        {
                            return i;
                        }
                    }
                    return -1;
                }
            }

            public string SelectedValue
            {
                get
                {
                    int index = SelectedIndex;
                    return index < 0 ? (Values.Count > 0 ? Values[0] : "") : Values[index];
                }
                set
                {
                    int index = -1;
                    for (int i = 0; i < Values.Count; i++)
                    {
                        if (Values[i] == value)
                        {
                            index = i;
                            break;
                        }
                    }
                    if (index < 0)
                    {
                        index = 0;
                    }
                    if (Buttons.Length > 0)
                    {
                        Buttons[index].IsChecked = true;
                    }
                }
            }
        }
    }
}
