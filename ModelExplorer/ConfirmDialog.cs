using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ModelExplorer
{
    public class ConfirmDialog : Window
    {
        public bool Confirmed { get; private set; }

        public ConfirmDialog(string message, string titleText)
        {
            Confirmed = false;
            Title = titleText;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            Width = 420;
            Height = 210;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Icon = AppIcon.WindowIcon;

            BuildUi(message, titleText);
            UiAnimation.FadeInOnOpen(this);
        }

        private void BuildUi(string message, string titleText)
        {
            AppTheme theme = ThemeManager.Current;
            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });

            Content = UiFactory.CreateWindowChrome(root, theme);
            ((Border)Content).MouseLeftButtonDown += Window_MouseLeftButtonDown;

            TextBlock title = new TextBlock
            {
                Text = titleText,
                Foreground = theme.TextBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid header = new Grid { Margin = new Thickness(18, 0, 10, 0) };
            header.Children.Add(title);
            Button close = UiFactory.MakeChromeButton("×", delegate { Close(); });
            close.HorizontalAlignment = HorizontalAlignment.Right;
            close.VerticalAlignment = VerticalAlignment.Center;
            header.Children.Add(close);
            root.Children.Add(header);
            Grid.SetRow(root.Children[root.Children.Count - 1], 0);

            TextBlock body = new TextBlock
            {
                Text = message,
                Foreground = theme.MutedBrush,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(18, 0, 18, 0)
            };
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
            Button ok = MakeButton("确定", true);
            ok.Click += delegate
            {
                Confirmed = true;
                Close();
            };
            footer.Children.Add(ok);
            root.Children.Add(footer);
            Grid.SetRow(root.Children[root.Children.Count - 1], 2);
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
