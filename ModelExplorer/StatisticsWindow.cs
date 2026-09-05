using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ModelExplorer
{
    public class FolderStat
    {
        public string Folder { get; set; }
        public int Parts { get; set; }
        public int Assemblies { get; set; }
        public int Stls { get; set; }
        public int ThreeMfs { get; set; }

        public int Total
        {
            get { return Parts + Assemblies + Stls + ThreeMfs; }
        }
    }

    public class StatisticsWindow : Window
    {
        public StatisticsWindow(List<FolderStat> stats)
        {
            Title = "详细统计";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            Width = 760;
            Height = 560;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Icon = AppIcon.WindowIcon;

            BuildUi(stats);
            UiAnimation.FadeInOnOpen(this);
        }

        private void BuildUi(List<FolderStat> stats)
        {
            AppTheme theme = ThemeManager.Current;

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });

            Content = UiFactory.CreateWindowChrome(root, theme);
            ((Border)Content).MouseLeftButtonDown += Window_MouseLeftButtonDown;

            Grid header = new Grid { Margin = new Thickness(18, 0, 10, 0) };
            TextBlock title = new TextBlock
            {
                Text = "详细统计",
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            header.Children.Add(title);
            Button close = UiFactory.MakeChromeButton("×", delegate { Close(); });
            close.HorizontalAlignment = HorizontalAlignment.Right;
            close.VerticalAlignment = VerticalAlignment.Center;
            header.Children.Add(close);
            root.Children.Add(header);
            Grid.SetRow(root.Children[root.Children.Count - 1], 0);

            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(18, 0, 18, 0)
            };
            StackPanel panel = new StackPanel();
            scroll.Content = panel;
            root.Children.Add(scroll);
            Grid.SetRow(root.Children[root.Children.Count - 1], 1);

            panel.Children.Add(BuildHeaderRow());
            foreach (FolderStat stat in stats)
            {
                panel.Children.Add(BuildStatRow(stat));
            }

            StackPanel footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(16, 0, 16, 0)
            };
            Button ok = MakeButton("关闭", delegate { Close(); }, 90, 34);
            footer.Children.Add(ok);
            root.Children.Add(footer);
            Grid.SetRow(root.Children[root.Children.Count - 1], 2);
        }

        private static Border BuildHeaderRow()
        {
            Border row = new Border
            {
                Background = ThemeManager.Current.PanelActiveBrush,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 4, 0, 4)
            };
            Grid grid = new Grid();
            for (int i = 0; i < 6; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            string[] labels = { "文件夹", "零件", "装配体", "STL", "3MF", "总计" };
            for (int i = 0; i < labels.Length; i++)
            {
                TextBlock text = new TextBlock
                {
                    Text = labels[i],
                    Foreground = ThemeManager.Current.TextBrush,
                    FontFamily = new FontFamily("Microsoft YaHei UI"),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(text, i);
                grid.Children.Add(text);
            }
            row.Child = grid;
            return row;
        }

        private static Border BuildStatRow(FolderStat stat)
        {
            Border row = new Border
            {
                Background = ThemeManager.Current.PanelBrush,
                BorderBrush = ThemeManager.Current.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 4, 0, 4)
            };
            Grid grid = new Grid();
            for (int i = 0; i < 6; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            AddCell(grid, 0, stat.Folder, true);
            AddCell(grid, 1, stat.Parts.ToString(), false);
            AddCell(grid, 2, stat.Assemblies.ToString(), false);
            AddCell(grid, 3, stat.Stls.ToString(), false);
            AddCell(grid, 4, stat.ThreeMfs.ToString(), false);
            AddCell(grid, 5, stat.Total.ToString(), true);
            row.Child = grid;
            return row;
        }

        private static void AddCell(Grid grid, int column, string text, bool bold)
        {
            TextBlock label = new TextBlock
            {
                Text = text,
                Foreground = ThemeManager.Current.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(label, column);
            grid.Children.Add(label);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private static Button MakeButton(string text, RoutedEventHandler handler, double width, double height)
        {
            Button button = new Button
            {
                Content = text,
                Width = width,
                Height = height,
                Background = ThemeManager.Current.AccentBrush,
                Foreground = new SolidColorBrush(Color.FromRgb(0x15, 0x15, 0x15)),
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };
            button.Template = UiFactory.RoundedButtonTemplate(true);
            button.Click += handler;
            return button;
        }
    }
}
