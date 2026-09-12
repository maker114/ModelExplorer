using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ModelExplorer
{
    public class SettingsWindow : Window
    {
        /// <summary>
        /// 表单右侧「操作列」的宽度：与路径行里 80px 的「浏览...」按钮 + 8px 间距一致。
        /// 下拉框行按此预留右侧空间，使各输入控件的右边缘与路径文本框对齐。
        /// </summary>
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
        private ComboBox _themeCombo;
        private ComboBox _fontSizeCombo;
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
        private ComboBox _stlUnitsCombo;
        private ComboBox _stlQualityCombo;

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
            Width = 560;
            // V3.1.0：分区变多后窗口不再一路拉长（曾到 990），改为固定常规高度，
            // 中间一栏用 ScrollViewer 滚动；页眉与「取消 / 保存」按钮固定不动。
            Height = 720;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Icon = AppIcon.WindowIcon;

            BuildUi();
            UiAnimation.FadeInOnOpen(this);
        }

        private void BuildUi()
        {
            AppTheme theme = ThemeManager.Current;

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });

            Content = UiFactory.CreateWindowChrome(root, theme);
            ((Border)Content).MouseLeftButtonDown += SettingsWindow_MouseLeftButtonDown;

            Grid header = new Grid { Margin = new Thickness(24, 0, 10, 0) };
            TextBlock title = new TextBlock
            {
                Text = "设置",
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 19,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            header.Children.Add(title);

            Button closeButton = UiFactory.MakeChromeButton("×", delegate { Close(); });
            closeButton.HorizontalAlignment = HorizontalAlignment.Right;
            closeButton.VerticalAlignment = VerticalAlignment.Center;
            header.Children.Add(closeButton);
            root.Children.Add(header);
            Grid.SetRow(root.Children[root.Children.Count - 1], 0);

            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            StackPanel body = new StackPanel { Margin = new Thickness(24, 8, 24, 8) };
            scroll.Content = body;
            root.Children.Add(scroll);
            Grid.SetRow(root.Children[root.Children.Count - 1], 1);

            body.Children.Add(SectionTitle("Bambu Studio"));
            body.Children.Add(new TextBlock
            {
                Text = "设置 bambu-studio.exe 的完整路径，适配不同电脑",
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 10)
            });

            Grid pathRow = new Grid();
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _bambuPathBox = new TextBox
            {
                Text = _source.BambuPath,
                Background = Brushes.Transparent,
                Foreground = theme.TextBrush,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                CaretBrush = theme.TextBrush,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Border pathShell = new Border
            {
                Background = theme.PanelBrush,
                BorderBrush = theme.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 6, 8, 6),
                Child = _bambuPathBox
            };
            Grid.SetColumn(pathShell, 0);
            pathRow.Children.Add(pathShell);

            Button browse = MakeButton("浏览...", Browse_Click, false, 80, 32);
            Grid.SetColumn(browse, 1);
            browse.Margin = new Thickness(8, 0, 0, 0);
            pathRow.Children.Add(browse);
            body.Children.Add(pathRow);

            body.Children.Add(SectionTitle("SolidWorks", 22));
            body.Children.Add(new TextBlock
            {
                Text = "设置 SLDWORKS.exe 的完整路径，用于 API 调用",
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 10)
            });

            Grid solidWorksPathRow = new Grid();
            solidWorksPathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            solidWorksPathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _solidWorksPathBox = new TextBox
            {
                Text = _source.SolidWorksPath,
                Background = Brushes.Transparent,
                Foreground = theme.TextBrush,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                CaretBrush = theme.TextBrush,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Border solidWorksPathShell = new Border
            {
                Background = theme.PanelBrush,
                BorderBrush = theme.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 6, 8, 6),
                Child = _solidWorksPathBox
            };
            Grid.SetColumn(solidWorksPathShell, 0);
            solidWorksPathRow.Children.Add(solidWorksPathShell);

            Button browseSolidWorks = MakeButton("浏览...", BrowseSolidWorks_Click, false, 80, 32);
            Grid.SetColumn(browseSolidWorks, 1);
            browseSolidWorks.Margin = new Thickness(8, 0, 0, 0);
            solidWorksPathRow.Children.Add(browseSolidWorks);
            body.Children.Add(solidWorksPathRow);

            // ---- 转换设置（V3.0.5 从主界面左侧栏移入）----
            body.Children.Add(SectionTitle("转换设置", 22));

            _keepHistorySwitch = new ToggleSwitch
            {
                IsChecked = _source.KeepHistory,
                VerticalAlignment = VerticalAlignment.Center
            };
            body.Children.Add(MakeToggleRow("保留历史版本", _keepHistorySwitch, theme, 8));
            body.Children.Add(HintText(
                "同名 STL 已存在时生成带时间戳的新文件，而不是直接覆盖。",
                theme, 2, 14));

            _openBambuSwitch = new ToggleSwitch
            {
                IsChecked = _source.OpenBambu,
                VerticalAlignment = VerticalAlignment.Center
            };
            body.Children.Add(MakeToggleRow("导出后打开 Bambu Studio", _openBambuSwitch, theme, 0));
            body.Children.Add(HintText(
                "导出完成后自动启动 Bambu Studio 并载入刚导出的 STL。",
                theme, 2, 0));

            // ---- 文件整理（V3.0.7 从主界面左侧栏移入）----
            body.Children.Add(SectionTitle("文件整理", 22));

            _organizeByFolderSwitch = new ToggleSwitch
            {
                IsChecked = _source.OrganizeByFolder,
                VerticalAlignment = VerticalAlignment.Center
            };
            body.Children.Add(MakeToggleRow("按文件夹整理", _organizeByFolderSwitch, theme, 8));
            body.Children.Add(HintText(
                "开启时在每个文件所在目录下分别使用「STL文件夹 / 3MF文件夹」；" +
                "关闭时把散落的 STL / 3MF 集中到工程根目录的这两个文件夹。",
                theme, 2, 0));

            // ---- STL 导出 ----
            body.Children.Add(SectionTitle("STL 导出", 22));
            body.Children.Add(new TextBlock
            {
                Text = "与 SolidWorks 插件、命令行工具共用同一份配置",
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 10)
            });

            _binaryStlSwitch = new ToggleSwitch
            {
                IsChecked = _source.UseBinaryStl,
                VerticalAlignment = VerticalAlignment.Center
            };
            body.Children.Add(MakeToggleRow("使用二进制 STL", _binaryStlSwitch, theme, 0));

            body.Children.Add(HintText(
                "开启时导出二进制 STL，体积小、加载快；关闭后导出 ASCII 文本格式，文件体积约为二进制的 5～10 倍。",
                theme,
                2,
                18));

            // 右边缘与上方「Bambu Studio / SolidWorks」路径文本框的右边缘对齐：
            // 那一行右侧是 80px 的「浏览...」按钮加 8px 间距，这里预留同样的宽度，
            // 否则下拉框会比文本框多伸出 88px，看起来不齐。
            Grid stlOptionsRow = new Grid { Margin = new Thickness(0, 0, ActionColumnWidth, 0) };
            stlOptionsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            stlOptionsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel unitsPanel = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            unitsPanel.Children.Add(OptionLabel("STL 单位", theme));
            // 小字说明放在下拉框上方，与标签一起构成该字段的说明
            unitsPanel.Children.Add(HintText("需与切片软件保持一致。", theme, 0, 6));
            _stlUnitsCombo = CreateCombo(theme);
            _stlUnitsCombo.Items.Add("mm");
            _stlUnitsCombo.Items.Add("cm");
            _stlUnitsCombo.Items.Add("m");
            _stlUnitsCombo.Items.Add("in");
            _stlUnitsCombo.SelectedItem = SolidWorksStlExporter.NormalizeUnits(_source.StlUnits);
            StyleComboBox(_stlUnitsCombo, theme);
            unitsPanel.Children.Add(_stlUnitsCombo);
            stlOptionsRow.Children.Add(unitsPanel);

            StackPanel qualityPanel = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            qualityPanel.Children.Add(OptionLabel("STL 质量", theme));
            // 小字说明放在下拉框上方，与标签一起构成该字段的说明
            qualityPanel.Children.Add(HintText("质量越高越精细，文件也越大。", theme, 0, 6));
            _stlQualityCombo = CreateCombo(theme);
            foreach (string qualityName in StlQualityLabels.DisplayNames)
            {
                _stlQualityCombo.Items.Add(qualityName);
            }
            _stlQualityCombo.SelectedItem = StlQualityLabels.ToDisplay(_source.StlQuality);
            StyleComboBox(_stlQualityCombo, theme);
            qualityPanel.Children.Add(_stlQualityCombo);
            Grid.SetColumn(qualityPanel, 1);
            stlOptionsRow.Children.Add(qualityPanel);

            body.Children.Add(stlOptionsRow);

            body.Children.Add(SectionTitle("外观预设", 22));
            _themeCombo = new ComboBox
            {
                Background = theme.PanelBrush,
                Foreground = theme.TextBrush,
                BorderBrush = theme.BorderBrush,
                Height = 32,
                Padding = new Thickness(8, 6, 8, 6),
                VerticalContentAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13,
                Margin = new Thickness(0, 8, 0, 0)
            };
            foreach (AppTheme preset in ThemeManager.Presets)
            {
                _themeCombo.Items.Add(preset.Name);
            }
            _themeCombo.SelectedItem = _source.Theme;
            if (_themeCombo.SelectedItem == null)
            {
                _themeCombo.SelectedItem = ThemeManager.Presets[0].Name;
            }
            StyleComboBox(_themeCombo, theme);
            body.Children.Add(_themeCombo);

            // ---- 毛玻璃 ----
            body.Children.Add(SectionTitle("毛玻璃", 22));

            _glassSwitch = new ToggleSwitch
            {
                IsChecked = _source.UseGlass,
                VerticalAlignment = VerticalAlignment.Center
            };
            body.Children.Add(MakeToggleRow("毛玻璃效果", _glassSwitch, theme, 8));
            body.Children.Add(HintText(
                "半透明玻璃面板 + 极光背景。关闭后回到纯色界面，低配机器或远程桌面下可关掉。",
                theme, 2, 12));

            _glassBlurSlider = MakeSlider(0, AppConfig.MaxGlassBlur);
            _glassBlurSlider.Value = _source.GlassBlurValue;
            _glassBlurValue = MakeValueLabel(theme);
            _glassBlurSlider.ValueChanged += delegate
            {
                _glassBlurValue.Text = ((int)Math.Round(_glassBlurSlider.Value)) + " px";
                PreviewGlass();
            };
            body.Children.Add(MakeSliderRow("模糊", _glassBlurSlider, _glassBlurValue, theme, 6));
            body.Children.Add(HintText(
                "作用在整层背景上：极光与自定义背景图一起变糊。0 = 不模糊。",
                theme, 6, 12));

            _glassOpacitySlider = MakeSlider(0, 100);
            _glassOpacitySlider.Value = _source.GlassOpacityValue;
            _glassOpacityValue = MakeValueLabel(theme);
            _glassOpacitySlider.ValueChanged += delegate
            {
                _glassOpacityValue.Text = ((int)Math.Round(_glassOpacitySlider.Value)) + " %";
                PreviewGlass();
            };
            body.Children.Add(MakeSliderRow("透明度", _glassOpacitySlider, _glassOpacityValue, theme, 6));
            body.Children.Add(HintText(
                "越大面板越透、越能看见背景；0 = 面板不透明，背景被完全挡住。",
                theme, 6, 0));
            _glassBlurValue.Text = ((int)Math.Round(_glassBlurSlider.Value)) + " px";
            _glassOpacityValue.Text = ((int)Math.Round(_glassOpacitySlider.Value)) + " %";

            // 关掉毛玻璃时这两个滑杆无意义，直接置灰，避免出现「改了却看不出效果」的设置
            SetGlassControlsEnabled(_source.UseGlass);
            _glassSwitch.Checked += delegate { SetGlassControlsEnabled(true); };
            _glassSwitch.Unchecked += delegate { SetGlassControlsEnabled(false); };

            BuildBackgroundSection(body, theme);

            body.Children.Add(SectionTitle("字体大小", 22));
            _fontSizeCombo = new ComboBox
            {
                Background = theme.PanelBrush,
                Foreground = theme.TextBrush,
                BorderBrush = theme.BorderBrush,
                Height = 32,
                Padding = new Thickness(8, 6, 8, 6),
                VerticalContentAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13,
                Margin = new Thickness(0, 8, 0, 0)
            };
            for (int i = 9; i <= 16; i++)
            {
                _fontSizeCombo.Items.Add(i.ToString());
            }
            _fontSizeCombo.SelectedItem = _source.FontSize.ToString();

            StyleComboBox(_fontSizeCombo, theme);
            body.Children.Add(_fontSizeCombo);

            StackPanel footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(16, 0, 16, 0)
            };
            Button cancelButton = MakeButton("取消", delegate { Close(); }, false, 90, 34);
            cancelButton.Margin = new Thickness(0, 0, 10, 0);
            footer.Children.Add(cancelButton);
            Button saveButton = MakeButton("保存", Save_Click, true, 90, 34);
            footer.Children.Add(saveButton);
            root.Children.Add(footer);
            Grid.SetRow(root.Children[root.Children.Count - 1], 2);

            // 控件都建完了才允许「实时预览」：滑杆初始化时会触发 ValueChanged，
            // 那时背景图分区的控件还不存在
            _uiReady = true;
        }

        /// <summary>
        /// 背景图分区。选图、适配、壁纸模糊、暗化都会**立刻预览**：直接改本窗口与主窗口已有的
        /// 背景层（<see cref="Glass.Configure(BackdropSettings)"/> + <see cref="Glass.Invalidate"/>），
        /// 但不写配置——点「保存」才落盘，取消或直接关窗会把预览还原（见 <see cref="OnClosed"/>）。
        /// 这样调滑杆时看到的就是最终效果，不必保存一次、重启一次。
        /// </summary>
        private void BuildBackgroundSection(StackPanel body, AppTheme theme)
        {
            body.Children.Add(SectionTitle("背景图", 22));

            _backgroundImage = _source.BackgroundImage ?? "";

            Grid pathRow = new Grid();
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _backgroundPathText = new TextBlock
            {
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            pathRow.Children.Add(new Border
            {
                Background = theme.PanelBrush,
                BorderBrush = theme.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8),
                Child = _backgroundPathText
            });

            Button browse = MakeButton("选择图片…", BrowseBackground_Click, false, ActionColumnWidth, 34);
            browse.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(browse, 1);
            pathRow.Children.Add(browse);
            // 与其它分区一致地留出 8px：否则分区标题下的分割线会贴到输入框上边框上，
            // 看起来像一条加粗的错位分割线
            pathRow.Margin = new Thickness(0, 8, 0, 0);
            body.Children.Add(pathRow);

            _backgroundClearButton = MakeButton("清除背景图", ClearBackground_Click, false, 110, 30);
            _backgroundClearButton.HorizontalAlignment = HorizontalAlignment.Left;
            _backgroundClearButton.Margin = new Thickness(0, 8, 0, 0);
            body.Children.Add(_backgroundClearButton);

            body.Children.Add(new TextBlock
            {
                Text = "适配",
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13,
                Margin = new Thickness(0, 16, 0, 8)
            });

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
                    // 未选中态用常规文字色而不是静音色：分段按钮上的字要一眼能读
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

            body.Children.Add(fitRow);

            _backgroundFitHint = HintText("", theme, 6, 14);
            body.Children.Add(_backgroundFitHint);

            _backgroundDarkenSlider = MakeSlider(0, AppConfig.MaxBackgroundDarken);
            _backgroundDarkenSlider.Value = _source.BackgroundDarkenValue;
            _backgroundDarkenValue = MakeValueLabel(theme);
            _backgroundDarkenSlider.ValueChanged += delegate
            {
                _backgroundDarkenValue.Text = ((int)Math.Round(_backgroundDarkenSlider.Value)) + " %";
                PreviewBackground();
            };
            body.Children.Add(MakeSliderRow("暗化", _backgroundDarkenSlider, _backgroundDarkenValue, theme, 6));
            body.Children.Add(HintText(
                "压暗背景以保证面板上的小字仍看得清。设得比可读性下限更暗时以你的设置为准；" +
                "更亮时自动补足到下限。模糊在「毛玻璃」分区里调，两者共用同一个值。",
                theme, 6, 0));
            _backgroundDarkenValue.Text = ((int)Math.Round(_backgroundDarkenSlider.Value)) + " %";

            RefreshBackgroundUi(theme, true);
        }

        /// <summary>把配置里的背景图设置灌进控件（首次或清除后调用）。</summary>
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

            _backgroundPathText.Text = System.IO.File.Exists(_backgroundImage)
                ? _backgroundImage
                : _backgroundImage + "　（文件不存在，已忽略）";
            _backgroundPathText.Foreground = System.IO.File.Exists(_backgroundImage)
                ? theme.TextBrush
                : theme.ErrorBrush;
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
                if (string.IsNullOrEmpty(_backgroundImage) == false && _backgroundDarkenSlider.Value <= 0)
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
                ThemeManager.ApplyGlass(_source.UseGlass, _source.GlassOpacityValue);
                Glass.Configure(_source);
                Glass.Invalidate();
            }
        }

        private static Slider MakeSlider(double minimum, double maximum)
        {
            Slider slider = new Slider
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
            return slider;
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

        /// <summary>「标签 + 滑杆 + 数值」一行，与参考的滑杆行布局一致。</summary>
        private static Grid MakeSliderRow(string label, Slider slider, TextBlock value, AppTheme theme, double topMargin)
        {
            Grid row = new Grid { Margin = new Thickness(0, topMargin, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });

            TextBlock text = new TextBlock
            {
                Text = label,
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(text, 0);
            row.Children.Add(text);
            Grid.SetColumn(slider, 1);
            row.Children.Add(slider);
            Grid.SetColumn(value, 2);
            row.Children.Add(value);
            return row;
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

        private static void StyleComboBox(ComboBox combo, AppTheme theme)
        {
            combo.Background = theme.PanelBrush;
            combo.Foreground = theme.TextBrush;
            combo.BorderBrush = theme.BorderBrush;
            combo.BorderThickness = new Thickness(1);
            combo.Padding = new Thickness(12, 6, 8, 6);
            combo.Template = UiFactory.RoundedComboBoxTemplate();

            // 下拉选项用不透明面板色：弹窗是独立窗口，背后就是主界面，透出来只会更难读
            Style itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, theme.OpaquePanelBrush));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, theme.TextBrush));
            itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(14, 6, 10, 6)));
            itemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));

            Trigger hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Control.BackgroundProperty, theme.OpaquePanelActiveBrush));
            itemStyle.Triggers.Add(hover);
            combo.ItemContainerStyle = itemStyle;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            int fontSize = 12;
            int.TryParse((string)_fontSizeCombo.SelectedItem, out fontSize);

            Result = new AppConfig
            {
                LastDir = _source.LastDir,
                KeepHistory = _keepHistorySwitch.IsChecked == true,
                OpenBambu = _openBambuSwitch.IsChecked == true,
                OrganizeByFolder = _organizeByFolderSwitch.IsChecked == true,
                BinaryStl = _binaryStlSwitch.IsChecked == true,
                StlUnits = (string)_stlUnitsCombo.SelectedItem,
                StlQuality = StlQualityLabels.ToToken((string)_stlQualityCombo.SelectedItem),
                BambuPath = _bambuPathBox.Text.Trim(),
                SolidWorksPath = _solidWorksPathBox.Text.Trim(),
                Theme = (string)_themeCombo.SelectedItem,
                FontSize = fontSize,
                Glass = _glassSwitch.IsChecked == true,
                // 3.4.0 起用滑杆：旧的三档强度不再写，读取时只作迁移用
                GlassBlur = (int)Math.Round(_glassBlurSlider.Value),
                GlassOpacity = (int)Math.Round(_glassOpacitySlider.Value),
                BackgroundImage = _backgroundImage ?? "",
                BackgroundFit = AppConfig.BackgroundFitTokens[SelectedFitIndex()],
                BackgroundDarken = (int)Math.Round(_backgroundDarkenSlider.Value),
                ProjectNameUnchecked = _source.ProjectNameUnchecked ?? new System.Collections.Generic.List<string>()
            };
            // 保存后主窗口会用新配置重建，这里不要再把预览还原回去
            _backgroundSaved = true;
            DialogResult = true;
        }

        private static TextBlock OptionLabel(string text, AppTheme theme)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 4)
            };
        }

        /// <summary>「标签 + 拨钮开关」一行，开关右对齐，与主界面原有的转换设置一致。</summary>
        private static Grid MakeToggleRow(string label, CheckBox toggle, AppTheme theme, double topMargin)
        {
            Grid row = new Grid { Margin = new Thickness(0, topMargin, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock text = new TextBlock
            {
                Text = label,
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(text, 0);
            row.Children.Add(text);

            Grid.SetColumn(toggle, 1);
            row.Children.Add(toggle);
            return row;
        }

        /// <summary>控件下方的小字说明，用于解释该设置的作用。</summary>
        private static TextBlock HintText(string text, AppTheme theme, double topMargin, double bottomMargin)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, topMargin, 0, bottomMargin)
            };
        }

        private static ComboBox CreateCombo(AppTheme theme)
        {
            return new ComboBox
            {
                Background = theme.PanelBrush,
                Foreground = theme.TextBrush,
                BorderBrush = theme.BorderBrush,
                Height = 32,
                Padding = new Thickness(8, 6, 8, 6),
                VerticalContentAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13
            };
        }

        /// <summary>
        /// 分区标题：用强调色高亮，并在下方加一条 1px 分割线，
        /// 把各设置分区在视觉上分开。
        /// </summary>
        private static FrameworkElement SectionTitle(string text, double topMargin = 0)
        {
            StackPanel header = new StackPanel
            {
                Margin = new Thickness(0, topMargin, 0, 0)
            };

            header.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = ThemeManager.Current.AccentBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 15,
                FontWeight = FontWeights.Bold
            });

            header.Children.Add(new Border
            {
                Height = 1,
                Background = ThemeManager.Current.BorderBrush,
                Margin = new Thickness(0, 6, 0, 0)
            });

            return header;
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
    }
}
