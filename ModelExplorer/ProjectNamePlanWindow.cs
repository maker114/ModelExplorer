using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ModelExplorer
{
    public class ProjectNamePlanWindow : Window
    {
        private readonly List<ProjectNameChange> _changes;
        private readonly HashSet<string> _uncheckedPaths;
        private TextBlock _summaryText;
        private Button _confirmButton;

        public bool Confirmed { get; private set; }
        public List<ProjectNameChange> ConfirmedChanges { get; private set; }
        public List<string> NewUncheckedPaths { get; private set; }

        public ProjectNamePlanWindow(List<ProjectNameChange> changes, List<string> uncheckedPaths)
        {
            Confirmed = false;
            ConfirmedChanges = new List<ProjectNameChange>();
            NewUncheckedPaths = new List<string>();
            _changes = changes ?? new List<ProjectNameChange>();
            _uncheckedPaths = new HashSet<string>(
                uncheckedPaths ?? new List<string>(),
                StringComparer.OrdinalIgnoreCase);

            foreach (ProjectNameChange change in _changes)
            {
                change.Selected = !_uncheckedPaths.Contains(change.SourcePath);
            }

            Title = "工程名检查";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            Width = 1080;
            Height = 620;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Icon = AppIcon.WindowIcon;

            BuildUi();
            UiAnimation.FadeInOnOpen(this);
        }

        private void BuildUi()
        {
            AppTheme theme = ThemeManager.Current;

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });

            Content = UiFactory.CreateWindowChrome(root, theme);
            ((Border)Content).MouseLeftButtonDown += Window_MouseLeftButtonDown;

            TextBlock title = new TextBlock
            {
                Text = "工程名检查",
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid header = new Grid { Margin = new Thickness(18, 0, 10, 0) };
            header.Children.Add(title);
            Button close = UiFactory.MakeChromeButton("×", delegate { Close(); });
            close.HorizontalAlignment = HorizontalAlignment.Right;
            close.VerticalAlignment = VerticalAlignment.Center;
            header.Children.Add(close);
            root.Children.Add(header);
            Grid.SetRow(root.Children[root.Children.Count - 1], 0);

            Grid body = new Grid
            {
                Margin = new Thickness(18, 6, 18, 6)
            };
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            string rootName = _changes.Count > 0 ? _changes[0].RootName : "";

            Border rootBanner = new Border
            {
                Background = theme.PanelActiveBrush,
                BorderBrush = theme.AccentBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(2, 0, 2, 8)
            };
            StackPanel bannerPanel = new StackPanel();
            bannerPanel.Children.Add(new TextBlock
            {
                Text = "目标工程名",
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            });
            bannerPanel.Children.Add(new TextBlock
            {
                Text = rootName,
                Foreground = theme.AccentBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 26,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            });
            rootBanner.Child = bannerPanel;
            Grid.SetRow(rootBanner, 0);
            body.Children.Add(rootBanner);

            TextBlock description = new TextBlock
            {
                Text = "工程名取文件名中第一个“_”之前的部分，目标工程名：" + rootName,
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(2, 0, 2, 8)
            };
            Grid.SetRow(description, 1);
            body.Children.Add(description);

            _summaryText = new TextBlock
            {
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11,
                Margin = new Thickness(2, 0, 2, 8)
            };
            Grid.SetRow(_summaryText, 2);
            body.Children.Add(_summaryText);

            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            StackPanel rows = new StackPanel();
            rows.Children.Add(BuildTableHeader(theme));
            for (int i = 0; i < _changes.Count; i++)
            {
                rows.Children.Add(BuildChangeRow(_changes[i], i, theme));
            }
            scroll.Content = rows;
            Grid.SetRow(scroll, 3);
            body.Children.Add(scroll);
            root.Children.Add(body);
            Grid.SetRow(root.Children[root.Children.Count - 1], 1);

            StackPanel footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(16, 0, 16, 0)
            };
            Button cancel = MakeButton("取消", false);
            cancel.Margin = new Thickness(0, 0, 10, 0);
            cancel.Click += delegate { Close(); };
            footer.Children.Add(cancel);

            _confirmButton = MakeButton("确认执行", true);
            _confirmButton.Click += Confirm_Click;
            footer.Children.Add(_confirmButton);
            root.Children.Add(footer);
            Grid.SetRow(root.Children[root.Children.Count - 1], 2);

            UpdateSummary();
        }

        private Border BuildTableHeader(AppTheme theme)
        {
            Border border = CreateRowBorder(theme, true);
            Grid grid = CreateRowGrid();
            AddHeaderCell(grid, 0, "勾选");
            AddHeaderCell(grid, 1, "文件类型");
            AddHeaderCell(grid, 2, "分类");
            AddHeaderCell(grid, 3, "当前文件名");
            AddHeaderCell(grid, 4, "修改后文件名");
            border.Child = grid;
            return border;
        }

        private Border BuildChangeRow(ProjectNameChange change, int index, AppTheme theme)
        {
            Border border = CreateRowBorder(theme, false);
            Grid grid = CreateRowGrid();

            CheckBox checkBox = new CheckBox
            {
                IsChecked = change.Selected,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = theme.TextBrush,
                Template = UiFactory.RoundedCheckBoxTemplate()
            };
            checkBox.Checked += delegate { SetChangeSelected(change, true); };
            checkBox.Unchecked += delegate { SetChangeSelected(change, false); };
            Grid.SetColumn(checkBox, 0);
            grid.Children.Add(checkBox);

            AddTypeCell(grid, 1, change.FileType);
            AddCell(grid, 2, change.Category, change.Category == "修改");
            AddCell(grid, 3, change.OriginalName, false, change.SourcePath);
            AddCell(grid, 4, change.TargetName, true, change.TargetPath);
            border.Child = grid;
            return border;
        }

        private static Grid CreateRowGrid()
        {
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(118) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            return grid;
        }

        private static Border CreateRowBorder(AppTheme theme, bool header)
        {
            Border border = new Border
            {
                Background = header ? theme.PanelActiveBrush : theme.PanelBrush,
                BorderBrush = header ? theme.BorderBrush : new SolidColorBrush(Color.FromRgb(0x24, 0x24, 0x24)),
                BorderThickness = new Thickness(1, 1, 1, header ? 1 : 0),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 0, header ? 6 : 0)
            };
            return border;
        }

        private static void AddHeaderCell(Grid grid, int column, string text)
        {
            TextBlock cell = new TextBlock
            {
                Text = text,
                Foreground = ThemeManager.Current.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(4, 0, 4, 0)
            };
            Grid.SetColumn(cell, column);
            grid.Children.Add(cell);
        }

        private static void AddCell(Grid grid, int column, string text, bool bold, string tooltip = null)
        {
            TextBlock cell = new TextBlock
            {
                Text = text,
                Foreground = ThemeManager.Current.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(4, 0, 4, 0)
            };
            if (!string.IsNullOrEmpty(tooltip))
            {
                ToolTipService.SetToolTip(cell, tooltip);
            }
            Grid.SetColumn(cell, column);
            grid.Children.Add(cell);
        }

        private static void AddTypeCell(Grid grid, int column, string fileType)
        {
            AppTheme theme = ThemeManager.Current;
            Brush brush = fileType == "零件" ? theme.PartBrush
                : fileType == "装配体导出" ? theme.AssemblyBrush
                : theme.StlBrush;
            TextBlock text = new TextBlock
            {
                Text = fileType ?? "STL",
                Foreground = brush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Border badge = new Border
            {
                Child = text,
                CornerRadius = new CornerRadius(6),
                BorderBrush = brush,
                BorderThickness = new Thickness(1),
                Background = MakeTypeBackground(brush),
                Padding = new Thickness(8, 2, 8, 2),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(badge, column);
            grid.Children.Add(badge);
        }

        private static Brush MakeTypeBackground(Brush brush)
        {
            SolidColorBrush solid = brush as SolidColorBrush;
            if (solid == null)
            {
                return Brushes.Transparent;
            }
            Color color = solid.Color;
            return new SolidColorBrush(Color.FromArgb(0x22, color.R, color.G, color.B));
        }

        private void SetChangeSelected(ProjectNameChange change, bool selected)
        {
            change.Selected = selected;
            if (selected)
            {
                _uncheckedPaths.Remove(change.SourcePath);
            }
            else
            {
                _uncheckedPaths.Add(change.SourcePath);
            }
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            int selectedCount = 0;
            foreach (ProjectNameChange change in _changes)
            {
                if (change.Selected)
                {
                    selectedCount++;
                }
            }

            _summaryText.Text = "待处理 " + _changes.Count + " 项，已勾选 " + selectedCount + " 项。只有勾选的文件会执行修改。";
            _confirmButton.Content = "确认执行 (" + selectedCount + ")";
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            ConfirmedChanges = new List<ProjectNameChange>();
            NewUncheckedPaths = new List<string>();
            foreach (ProjectNameChange change in _changes)
            {
                if (change.Selected)
                {
                    ConfirmedChanges.Add(change);
                }
                else
                {
                    NewUncheckedPaths.Add(change.SourcePath);
                }
            }

            Confirmed = true;
            DialogResult = true;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private static Button MakeButton(string text, bool primary)
        {
            Button button = new Button
            {
                Content = text,
                Width = primary ? 130 : 90,
                Height = 34,
                Background = primary ? ThemeManager.Current.AccentBrush : ThemeManager.Current.PanelActiveBrush,
                Foreground = primary ? new SolidColorBrush(Color.FromRgb(0x15, 0x15, 0x15)) : ThemeManager.Current.TextBrush,
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                Cursor = Cursors.Hand
            };
            button.Template = UiFactory.RoundedButtonTemplate(primary);
            return button;
        }
    }
}
