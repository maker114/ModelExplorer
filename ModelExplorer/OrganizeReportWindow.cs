using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ModelExplorer
{
    public class OrganizeMoveInfo
    {
        public string SourceFolder { get; set; }
        public string TargetFolder { get; set; }
        public int StlCount { get; set; }
        public int ThreeMfCount { get; set; }
    }

    public class OrganizeReportWindow : Window
    {
        public OrganizeReportWindow(List<OrganizeMoveInfo> moves, List<string> createdDirs, List<string> deletedDirs)
        {
            Title = "整理报告";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            Width = 720;
            Height = 520;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Icon = AppIcon.WindowIcon;

            BuildUi(moves, createdDirs, deletedDirs);
            UiAnimation.FadeInOnOpen(this);
        }

        private void BuildUi(List<OrganizeMoveInfo> moves, List<string> createdDirs, List<string> deletedDirs)
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
                Text = "整理报告",
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

            panel.Children.Add(BuildFolderListSection("新增文件夹", createdDirs));
            panel.Children.Add(BuildFolderListSection("删除空分类文件夹", deletedDirs));

            TextBlock moveTitle = new TextBlock
            {
                Text = "文件移动明细",
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 14, 0, 6)
            };
            panel.Children.Add(moveTitle);

            if (moves.Count == 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "没有文件被移动",
                    Foreground = theme.MutedBrush,
                    FontFamily = new FontFamily("Microsoft YaHei UI"),
                    FontSize = 14,
                    Margin = new Thickness(0, 24, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }
            else
            {
                Dictionary<string, List<OrganizeMoveInfo>> groups = new Dictionary<string, List<OrganizeMoveInfo>>(System.StringComparer.OrdinalIgnoreCase);
                foreach (OrganizeMoveInfo move in moves)
                {
                    List<OrganizeMoveInfo> group;
                    if (!groups.TryGetValue(move.SourceFolder, out group))
                    {
                        group = new List<OrganizeMoveInfo>();
                        groups[move.SourceFolder] = group;
                    }
                    group.Add(move);
                }
                foreach (KeyValuePair<string, List<OrganizeMoveInfo>> pair in groups)
                {
                    panel.Children.Add(BuildSourceGroup(pair.Key, pair.Value));
                }
            }

            StackPanel footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(16, 0, 16, 0)
            };
            Button ok = MakeButton("关闭");
            ok.Click += delegate { Close(); };
            ok.Width = 110;
            ok.Height = 36;
            footer.Children.Add(ok);
            root.Children.Add(footer);
            Grid.SetRow(root.Children[root.Children.Count - 1], 2);
        }

        private static Border BuildSourceGroup(string sourceFolder, List<OrganizeMoveInfo> moves)
        {
            AppTheme theme = ThemeManager.Current;
            int stlCount = 0;
            int threeMfCount = 0;
            string stlTarget = "";
            string threeMfTarget = "";

            foreach (OrganizeMoveInfo move in moves)
            {
                if (move.StlCount > 0)
                {
                    stlCount = move.StlCount;
                    stlTarget = move.TargetFolder;
                }
                if (move.ThreeMfCount > 0)
                {
                    threeMfCount = move.ThreeMfCount;
                    threeMfTarget = move.TargetFolder;
                }
            }

            Border row = new Border
            {
                Background = theme.PanelBrush,
                BorderBrush = theme.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 6, 0, 6)
            };

            Grid detail = new Grid();
            detail.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            detail.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            detail.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int typeRows = 0;
            if (stlCount > 0)
            {
                typeRows++;
            }
            if (threeMfCount > 0)
            {
                typeRows++;
            }
            int totalRows = typeRows + 1;
            for (int i = 0; i < totalRows; i++)
            {
                detail.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            TextBlock sourceText = new TextBlock
            {
                Text = sourceFolder,
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            Border sourceBox = new Border
            {
                Background = theme.PanelActiveBrush,
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(12, 10, 12, 10),
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = sourceText
            };
            Grid.SetRow(sourceBox, 0);
            Grid.SetColumn(sourceBox, 0);
            Grid.SetRowSpan(sourceBox, totalRows);
            detail.Children.Add(sourceBox);

            Grid rightPanel = new Grid
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 0, 0)
            };
            for (int i = 0; i < totalRows; i++)
            {
                rightPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            TextBlock parentText = new TextBlock
            {
                Text = GetTargetParent(stlTarget, threeMfTarget),
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Left,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 2, 0, 6)
            };
            Grid.SetRow(parentText, 0);
            Grid.SetColumn(parentText, 0);
            rightPanel.Children.Add(parentText);

            Border verticalLine = new Border
            {
                Width = 2,
                Background = theme.AccentBrush,
                CornerRadius = new CornerRadius(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0, 20, 0, 0)
            };
            Grid.SetRow(verticalLine, 0);
            Grid.SetColumn(verticalLine, 0);
            Grid.SetRowSpan(verticalLine, totalRows);
            rightPanel.Children.Add(verticalLine);

            rightPanel.SizeChanged += delegate
            {
                if (totalRows > 0)
                {
                    double lastRowHeight = rightPanel.RowDefinitions[totalRows - 1].ActualHeight;
                    double bottomMargin = lastRowHeight / 2 - 1;
                    if (bottomMargin < 0)
                    {
                        bottomMargin = 0;
                    }
                    verticalLine.Margin = new Thickness(0, 20, 0, bottomMargin);
                }
            };

            int rowIndex = 1;
            if (stlCount > 0)
            {
                AddArrowCell(detail, rowIndex, "STL文件", stlCount);
                UIElement stlChild = CreateTargetChildRow("STL文件夹");
                Grid.SetRow(stlChild, rowIndex);
                Grid.SetColumn(stlChild, 0);
                rightPanel.Children.Add(stlChild);
                rowIndex++;
            }
            if (threeMfCount > 0)
            {
                AddArrowCell(detail, rowIndex, "3MF文件", threeMfCount);
                UIElement threeMfChild = CreateTargetChildRow("3MF文件夹");
                Grid.SetRow(threeMfChild, rowIndex);
                Grid.SetColumn(threeMfChild, 0);
                rightPanel.Children.Add(threeMfChild);
                rowIndex++;
            }

            Border rightBox = new Border
            {
                Background = theme.PanelActiveBrush,
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(12, 10, 12, 10),
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 0),
                Child = rightPanel
            };
            Grid.SetRow(rightBox, 0);
            Grid.SetColumn(rightBox, 2);
            Grid.SetRowSpan(rightBox, totalRows);
            detail.Children.Add(rightBox);

            row.Child = detail;
            return row;
        }

        private static void AddArrowCell(Grid grid, int row, string typeName, int count)
        {
            AppTheme theme = ThemeManager.Current;
            StackPanel arrow = new StackPanel
            {
                Width = 180,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            arrow.Children.Add(new TextBlock
            {
                Text = typeName + " " + count + "个",
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 2)
            });

            Grid lineGrid = new Grid { Height = 10, Margin = new Thickness(0, 2, 0, 0) };
            Border line = new Border
            {
                Background = theme.AccentBrush,
                Height = 3,
                CornerRadius = new CornerRadius(2),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 16, 0)
            };
            lineGrid.Children.Add(line);
            TextBlock arrowHead = new TextBlock
            {
                Text = "▶",
                Foreground = theme.AccentBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            lineGrid.Children.Add(arrowHead);
            arrow.Children.Add(lineGrid);

            Grid.SetRow(arrow, row);
            Grid.SetColumn(arrow, 1);
            grid.Children.Add(arrow);
        }

        private static UIElement CreateTargetChildRow(string childName)
        {
            AppTheme theme = ThemeManager.Current;
            StackPanel row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 2)
            };
            row.Children.Add(new Border
            {
                Width = 20,
                Height = 2,
                Background = theme.AccentBrush,
                CornerRadius = new CornerRadius(1),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            });
            row.Children.Add(new TextBlock
            {
                Text = childName,
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11,
                TextAlignment = TextAlignment.Left,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 0)
            });
            return row;
        }

        private static string GetTargetParent(string stlTarget, string threeMfTarget)
        {
            string first = stlTarget;
            if (string.IsNullOrEmpty(first))
            {
                first = threeMfTarget;
            }
            if (string.IsNullOrEmpty(first))
            {
                return "目标文件夹";
            }

            string second = string.IsNullOrEmpty(threeMfTarget) ? stlTarget : threeMfTarget;
            if (!string.IsNullOrEmpty(second) &&
                !string.Equals(GetParentOf(first), GetParentOf(second), System.StringComparison.OrdinalIgnoreCase))
            {
                return GetParentOf(first);
            }
            return GetParentOf(first);
        }

        private static string GetParentOf(string path)
        {
            int index = path.LastIndexOf('\\');
            if (index > 0)
            {
                return path.Substring(0, index);
            }
            return path;
        }

        private static UIElement BuildFolderListSection(string title, List<string> folders)
        {
            AppTheme theme = ThemeManager.Current;
            StackPanel panel = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };
            panel.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 6, 0, 4)
            });

            if (folders == null || folders.Count == 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "无",
                    Foreground = theme.MutedBrush,
                    FontFamily = new FontFamily("Microsoft YaHei UI"),
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 0, 2)
                });
                return panel;
            }

            foreach (string folder in folders)
            {
                Border item = new Border
                {
                    Background = theme.PanelActiveBrush,
                    CornerRadius = new CornerRadius(0),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 3, 0, 3)
                };
                item.Child = new TextBlock
                {
                    Text = folder,
                    Foreground = theme.TextBrush,
                    FontFamily = new FontFamily("Microsoft YaHei UI"),
                    FontSize = 11
                };
                panel.Children.Add(item);
            }
            return panel;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private static Button MakeButton(string text)
        {
            Button button = new Button
            {
                Content = text,
                Width = 34,
                Height = 28,
                Background = ThemeManager.Current.PanelActiveBrush,
                Foreground = ThemeManager.Current.TextBrush,
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                Cursor = Cursors.Hand
            };
            button.Template = UiFactory.RoundedButtonTemplate(false);
            return button;
        }
    }
}
