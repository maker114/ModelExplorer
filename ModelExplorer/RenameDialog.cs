using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ModelExplorer
{
    public class RenameDialog : Window
    {
        private readonly string _sourcePath;
        private readonly string _extension;
        private TextBox _nameBox;
        private TextBlock _errorText;

        public bool Confirmed { get; private set; }
        public string TargetPath { get; private set; }

        public RenameDialog(string sourcePath)
        {
            Confirmed = false;
            TargetPath = null;
            _sourcePath = sourcePath;
            _extension = Path.GetExtension(sourcePath);

            Title = "重命名模型";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            Width = 430;
            Height = 240;
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
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(66) });

            Content = UiFactory.CreateWindowChrome(root, theme);
            ((Border)Content).MouseLeftButtonDown += Window_MouseLeftButtonDown;

            TextBlock title = new TextBlock
            {
                Text = "重命名模型",
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid header = new Grid { Margin = new Thickness(24, 0, 10, 0) };
            header.Children.Add(title);
            Button close = UiFactory.MakeChromeButton("×", delegate { Close(); });
            close.HorizontalAlignment = HorizontalAlignment.Right;
            close.VerticalAlignment = VerticalAlignment.Center;
            header.Children.Add(close);
            root.Children.Add(header);
            Grid.SetRow(root.Children[root.Children.Count - 1], 0);

            StackPanel body = new StackPanel
            {
                Margin = new Thickness(24, 8, 24, 8)
            };

            body.Children.Add(new TextBlock
            {
                Text = "新文件名",
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            });

            _nameBox = new TextBox
            {
                Text = Path.GetFileNameWithoutExtension(_sourcePath),
                Background = Brushes.Transparent,
                Foreground = theme.TextBrush,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                CaretBrush = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 13,
                VerticalContentAlignment = VerticalAlignment.Center,
                MaxLength = 128
            };
            _nameBox.SelectAll();

            Border inputShell = new Border
            {
                Background = theme.PanelBrush,
                BorderBrush = theme.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 7, 0, 6),
                Child = _nameBox
            };
            body.Children.Add(inputShell);

            body.Children.Add(new TextBlock
            {
                Text = "扩展名将保持为 " + _extension,
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11
            });

            _errorText = new TextBlock
            {
                Foreground = theme.ErrorBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            };
            body.Children.Add(_errorText);
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

            Button ok = MakeButton("重命名", true);
            ok.Click += Ok_Click;
            footer.Children.Add(ok);
            root.Children.Add(footer);
            Grid.SetRow(root.Children[root.Children.Count - 1], 2);

            Loaded += delegate
            {
                _nameBox.Focus();
                _nameBox.SelectAll();
            };
            _nameBox.KeyDown += NameBox_KeyDown;
        }

        private void NameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Ok_Click(sender, e);
                e.Handled = true;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            string enteredName = _nameBox.Text.Trim();
            if (enteredName.Length == 0)
            {
                _errorText.Text = "文件名不能为空。";
                return;
            }

            if (Path.HasExtension(enteredName))
            {
                enteredName = Path.GetFileNameWithoutExtension(enteredName);
            }

            if (enteredName.Length == 0 ||
                enteredName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                _errorText.Text = "文件名包含系统不允许的字符。";
                return;
            }

            string directory = Path.GetDirectoryName(_sourcePath);
            string targetPath = Path.Combine(directory, enteredName + _extension);
            if (string.Equals(targetPath, _sourcePath, StringComparison.OrdinalIgnoreCase))
            {
                _errorText.Text = "文件名没有变化。";
                return;
            }
            if (File.Exists(targetPath))
            {
                _errorText.Text = "目标位置已存在同名文件。";
                return;
            }

            Confirmed = true;
            TargetPath = targetPath;
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
                Width = 100,
                Height = 34,
                Background = primary ? ThemeManager.Current.AccentBrush : ThemeManager.Current.PanelActiveBrush,
                Foreground = primary ? ThemeManager.Current.OnAccentBrush : ThemeManager.Current.TextBrush,
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
