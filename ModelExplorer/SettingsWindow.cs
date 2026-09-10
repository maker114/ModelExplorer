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

        private readonly AppConfig _source;
        private TextBox _bambuPathBox;
        private TextBox _solidWorksPathBox;
        private ComboBox _themeCombo;
        private ComboBox _fontSizeCombo;
        private ToggleSwitch _binaryStlSwitch;
        private ToggleSwitch _keepHistorySwitch;
        private ToggleSwitch _openBambuSwitch;
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
            Height = 830;
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
                FontSize = 18,
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
                FontSize = 11,
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
                FontSize = 11,
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
                FontSize = 11,
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
                FontSize = 11,
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

            // ---- STL 导出 ----
            body.Children.Add(SectionTitle("STL 导出", 22));
            body.Children.Add(new TextBlock
            {
                Text = "与 SolidWorks 插件、命令行工具共用同一份配置",
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11,
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
            _stlUnitsCombo = CreateCombo(theme);
            _stlUnitsCombo.Items.Add("mm");
            _stlUnitsCombo.Items.Add("cm");
            _stlUnitsCombo.Items.Add("m");
            _stlUnitsCombo.Items.Add("in");
            _stlUnitsCombo.SelectedItem = SolidWorksStlExporter.NormalizeUnits(_source.StlUnits);
            StyleComboBox(_stlUnitsCombo, theme);
            unitsPanel.Children.Add(_stlUnitsCombo);
            // 说明文字与该列下拉框左对齐，避免被误认为属于另一列
            unitsPanel.Children.Add(HintText("需与切片软件保持一致。", theme, 6, 0));
            stlOptionsRow.Children.Add(unitsPanel);

            StackPanel qualityPanel = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            qualityPanel.Children.Add(OptionLabel("STL 质量", theme));
            _stlQualityCombo = CreateCombo(theme);
            foreach (string qualityName in StlQualityLabels.DisplayNames)
            {
                _stlQualityCombo.Items.Add(qualityName);
            }
            _stlQualityCombo.SelectedItem = StlQualityLabels.ToDisplay(_source.StlQuality);
            StyleComboBox(_stlQualityCombo, theme);
            qualityPanel.Children.Add(_stlQualityCombo);
            // 说明文字与该列下拉框左对齐，避免被误认为属于另一列
            qualityPanel.Children.Add(HintText("质量越高越精细，文件也越大。", theme, 6, 0));
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
                FontSize = 12,
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
                FontSize = 12,
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

            Style itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, theme.PanelBrush));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, theme.TextBrush));
            itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(14, 6, 10, 6)));
            itemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));

            Trigger hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Control.BackgroundProperty, theme.PanelActiveBrush));
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
                BinaryStl = _binaryStlSwitch.IsChecked == true,
                StlUnits = (string)_stlUnitsCombo.SelectedItem,
                StlQuality = StlQualityLabels.ToToken((string)_stlQualityCombo.SelectedItem),
                BambuPath = _bambuPathBox.Text.Trim(),
                SolidWorksPath = _solidWorksPathBox.Text.Trim(),
                Theme = (string)_themeCombo.SelectedItem,
                FontSize = fontSize,
                ProjectNameUnchecked = _source.ProjectNameUnchecked ?? new System.Collections.Generic.List<string>()
            };
            DialogResult = true;
        }

        private static TextBlock OptionLabel(string text, AppTheme theme)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11,
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
                FontSize = 12,
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
                FontSize = 10,
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
                FontSize = 12
            };
        }

        private static TextBlock SectionTitle(string text, double topMargin = 0)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = ThemeManager.Current.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, topMargin, 0, 0)
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
                FontSize = 12,
                FontWeight = primary ? FontWeights.Bold : FontWeights.Normal,
                Cursor = Cursors.Hand
            };
            button.Template = UiFactory.RoundedButtonTemplate(primary);
            button.Click += handler;
            return button;
        }
    }
}
