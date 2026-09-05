using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace ModelExplorer
{
    public class FolderItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public class FolderPickerWindow : Window
    {
        private TextBox _pathBox;
        private ListBox _folderList;
        private string _currentPath;

        public string SelectedPath { get; private set; }

        public FolderPickerWindow(string initialPath)
        {
            SelectedPath = null;
            _currentPath = !string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath)
                ? initialPath
                : null;

            Title = "选择工程目录";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            Width = 620;
            Height = 620;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Icon = AppIcon.WindowIcon;

            BuildUi();
            RefreshFolders();
        }

        private void BuildUi()
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
                Text = "选择工程目录",
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

            Grid body = new Grid { Margin = new Thickness(18, 0, 18, 0) };
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.Children.Add(body);
            Grid.SetRow(root.Children[root.Children.Count - 1], 1);

            Grid pathRow = new Grid();
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _pathBox = new TextBox
            {
                Background = theme.PanelBrush,
                Foreground = theme.TextBrush,
                BorderBrush = theme.BorderBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 6, 8, 6),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_pathBox, 0);
            pathRow.Children.Add(_pathBox);

            Button upButton = MakeButton("上一级", Up_Click, false, 76, 32);
            Grid.SetColumn(upButton, 1);
            upButton.Margin = new Thickness(8, 0, 0, 0);
            pathRow.Children.Add(upButton);

            Button enterButton = MakeButton("进入", Enter_Click, false, 64, 32);
            Grid.SetColumn(enterButton, 2);
            enterButton.Margin = new Thickness(8, 0, 0, 0);
            pathRow.Children.Add(enterButton);

            Button chooseButton = MakeButton("选择此目录", Choose_Click, true, 96, 32);
            Grid.SetColumn(chooseButton, 3);
            chooseButton.Margin = new Thickness(8, 0, 0, 0);
            pathRow.Children.Add(chooseButton);
            body.Children.Add(pathRow);

            _folderList = new ListBox
            {
                Background = theme.PanelBrush,
                BorderBrush = theme.BorderBrush,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 12, 0, 0),
                ItemTemplate = CreateFolderTemplate(),
                ItemContainerStyle = CreateFolderItemStyle()
            };
            _folderList.MouseDoubleClick += FolderList_MouseDoubleClick;
            ScrollViewer.SetHorizontalScrollBarVisibility(_folderList, ScrollBarVisibility.Disabled);
            Grid.SetRow(_folderList, 1);
            body.Children.Add(_folderList);

            StackPanel footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(16, 0, 16, 0)
            };
            Button cancel = MakeButton("取消", delegate { Close(); }, false, 90, 34);
            cancel.Margin = new Thickness(0, 0, 10, 0);
            footer.Children.Add(cancel);
            Button ok = MakeButton("选择此目录", Choose_Click, true, 110, 34);
            footer.Children.Add(ok);
            root.Children.Add(footer);
            Grid.SetRow(root.Children[root.Children.Count - 1], 2);
        }

        private void RefreshFolders()
        {
            AppTheme theme = ThemeManager.Current;
            List<FolderItem> items = new List<FolderItem>();

            if (string.IsNullOrEmpty(_currentPath))
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    items.Add(new FolderItem { Name = drive.Name, Path = drive.RootDirectory.FullName });
                }
            }
            else
            {
                try
                {
                    string[] dirs = Directory.GetDirectories(_currentPath);
                    Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
                    foreach (string dir in dirs)
                    {
                        DirectoryInfo info = new DirectoryInfo(dir);
                        items.Add(new FolderItem { Name = info.Name, Path = info.FullName });
                    }
                }
                catch
                {
                }
            }

            _folderList.ItemsSource = items;
            _pathBox.Text = string.IsNullOrEmpty(_currentPath) ? "选择磁盘" : _currentPath;
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentPath))
            {
                return;
            }
            DirectoryInfo parent = Directory.GetParent(_currentPath);
            _currentPath = parent != null ? parent.FullName : null;
            RefreshFolders();
        }

        private void Enter_Click(object sender, RoutedEventArgs e)
        {
            string text = _pathBox.Text.Trim();
            if (Directory.Exists(text))
            {
                _currentPath = text;
                RefreshFolders();
            }
        }

        private void FolderList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FolderItem item = _folderList.SelectedItem as FolderItem;
            if (item != null)
            {
                _currentPath = item.Path;
                RefreshFolders();
            }
        }

        private void Choose_Click(object sender, RoutedEventArgs e)
        {
            FolderItem item = _folderList.SelectedItem as FolderItem;
            string path = item != null ? item.Path : _pathBox.Text.Trim();
            if (Directory.Exists(path))
            {
                SelectedPath = path;
                DialogResult = true;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private static DataTemplate CreateFolderTemplate()
        {
            string text = ColorToHex(ThemeManager.Current.Text);
            string muted = ColorToHex(ThemeManager.Current.Muted);
            string xaml =
                "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
                "<StackPanel Margin='6'>" +
                "<TextBlock Text='{Binding Name}' Foreground='" + text + "' FontFamily='Microsoft YaHei UI' FontSize='13' FontWeight='Bold'/>" +
                "<TextBlock Text='{Binding Path}' Foreground='" + muted + "' FontFamily='Consolas' FontSize='10' TextTrimming='CharacterEllipsis'/>" +
                "</StackPanel></DataTemplate>";
            return (DataTemplate)XamlReader.Parse(xaml);
        }

        private static Style CreateFolderItemStyle()
        {
            string active = ColorToHex(ThemeManager.Current.PanelActive);
            string accent = ColorToHex(ThemeManager.Current.Accent);
            string xaml =
                "<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ListBoxItem'>" +
                "<Setter Property='HorizontalContentAlignment' Value='Stretch'/>" +
                "<Setter Property='Margin' Value='0,2'/>" +
                "<Setter Property='Template'>" +
                "<Setter.Value><ControlTemplate TargetType='ListBoxItem'>" +
                "<Border x:Name='bd' Background='Transparent' CornerRadius='6' Padding='6'>" +
                "<ContentPresenter/>" +
                "</Border>" +
                "<ControlTemplate.Triggers>" +
                "<Trigger Property='IsMouseOver' Value='True'><Setter TargetName='bd' Property='Background' Value='" + active + "'/></Trigger>" +
                "<Trigger Property='IsSelected' Value='True'>" +
                "<Setter TargetName='bd' Property='Background' Value='" + active + "'/>" +
                "<Setter TargetName='bd' Property='BorderBrush' Value='" + accent + "'/>" +
                "<Setter TargetName='bd' Property='BorderThickness' Value='1'/>" +
                "</Trigger>" +
                "</ControlTemplate.Triggers>" +
                "</ControlTemplate></Setter.Value></Setter></Style>";
            return (Style)XamlReader.Parse(xaml);
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
                Cursor = Cursors.Hand
            };
            button.Template = UiFactory.RoundedButtonTemplate(primary);
            button.Click += handler;
            return button;
        }

        private static string ColorToHex(Color color)
        {
            return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }
    }
}
