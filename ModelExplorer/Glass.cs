using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ModelExplorer
{
    /// <summary>
    /// 应用内毛玻璃：在每个窗口的实色底之上插一层**预烘焙的背景位图**（底色 + 极光 + 可选自定义背景图）。
    ///
    /// 背景的模糊、暗化与颗粒都在 <see cref="Backdrop"/> 里一次性算进位图，这一层只负责
    /// 「按窗口尺寸烘焙 / 尺寸变化后重建 / 圆角裁剪」，因此每帧的渲染成本就是贴一张位图。
    ///
    /// 挂载点只有两处：主窗口的 ShellRoot 与 <c>UiFactory.CreateWindowChrome</c>（六个对话框共用）。
    ///
    /// 关于开销的实测结论（见 README「毛玻璃」一节）：改尺寸时占满一个核的主因是
    /// AllowsTransparency=True 的分层窗口在软件渲染下整窗重绘，去掉模糊并不会减少它
    /// （无背景层的 3.1.2 同样是 ~109%）。预烘焙换来的是：效果开销与「模糊半径、光斑数量」
    /// 完全脱钩、背景图几乎零边际成本、内存占用可预期。
    /// </summary>
    public static class Glass
    {
        /// <summary>窗口圆角，与 MainWindow.xaml / UiFactory.CreateWindowChrome 的 14 保持一致。</summary>
        private const double CornerRadius = 14;

        /// <summary>用来在视觉树里认领自己插入的背景层，避免重复叠加。</summary>
        private const string BackdropTag = "ModelExplorer.GlassBackdrop";

        /// <summary>尺寸变化后的重建延迟：拖动过程中先拉伸旧位图，停手后再烘焙。</summary>
        private const int RebuildDelayMs = 120;

        // 弱引用列表：窗口关掉后对应项在下次遍历时被剔除，
        // 不会把窗口连同位图一起留在静态字段里（ConditionalWeakTable 只显式实现 IEnumerable，不便遍历）
        private static readonly List<WeakReference> Layers = new List<WeakReference>();

        private static BackdropSettings _settings = DefaultSettings();
        private static int _stamp;

        public static BackdropSettings Settings
        {
            get { return _settings; }
        }

        /// <summary>当前是否设置了自定义背景图。</summary>
        public static bool HasImage
        {
            get { return !string.IsNullOrEmpty(_settings.ImagePath); }
        }

        private static BackdropSettings DefaultSettings()
        {
            BackdropSettings settings = new BackdropSettings();
            settings.Fit = BackdropFit.Cover;
            settings.Blur = AppConfig.DefaultBackgroundBlur;
            settings.Darken = AppConfig.DefaultBackgroundDarken;
            return settings;
        }

        /// <summary>按配置设定背景图参数；主题与玻璃档位仍由 ThemeManager 负责。</summary>
        public static void Configure(AppConfig config)
        {
            _settings = BackdropSettings.FromConfig(config);
            _stamp++;
        }

        /// <summary>设置窗口做实时预览时直接给一组参数（不写配置）。</summary>
        public static void Configure(BackdropSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            _settings = settings;
            _stamp++;
        }

        /// <summary>参数变化后重建所有窗口的背景层（内部按 120ms 防抖）。</summary>
        public static void Invalidate()
        {
            for (int i = Layers.Count - 1; i >= 0; i--)
            {
                BackdropHost host = Layers[i].Target as BackdropHost;
                if (host == null)
                {
                    Layers.RemoveAt(i);
                    continue;
                }

                host.RequestRebuild(false);
            }
        }

        /// <summary>
        /// 把背景层插入宿主。宿主第一个子元素应当是窗口的不透明底色，
        /// 背景层插在它之上、边框描边之下（描边必须留在最上层，否则窗口外框会被糊掉）。
        /// 玻璃档位为 0 时只负责移除旧层，观感回到 v3.1.3 的纯色。
        /// </summary>
        public static void Apply(Grid host, AppTheme theme)
        {
            if (host == null || theme == null)
            {
                return;
            }

            RemoveBackdrop(host);

            if (theme.GlassLevel <= 0)
            {
                return;
            }

            BackdropHost layer = new BackdropHost(host, theme);
            host.Children.Insert(Math.Min(1, host.Children.Count), layer.Element);
            Layers.Add(new WeakReference(layer));
            // 宿主可能已经排过版（例如主题变更后重挂），此时不会再触发 SizeChanged，主动烘焙一次
            layer.RequestRebuild(true);
        }

        private static void RemoveBackdrop(Grid host)
        {
            for (int i = host.Children.Count - 1; i >= 0; i--)
            {
                FrameworkElement child = host.Children[i] as FrameworkElement;
                if (child != null && BackdropTag.Equals(child.Tag))
                {
                    host.Children.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 一个窗口的背景层：一张按窗口尺寸烘焙好的位图 + 防抖重建。
        ///
        /// 这里刻意用 Rectangle + ImageBrush，而不是 Image 元素：Image 在 Source 为 null 时
        /// 会被 Arrange 成 0×0（实测），于是「等有了尺寸再烘焙」与「先有 Source 才有尺寸」
        /// 互相死等，背景层永远不出现。Rectangle 即使没有 Fill 也会铺满所在单元格，
        /// 尺寸从一开始就是可用的。
        /// </summary>
        private sealed class BackdropHost
        {
            private readonly Grid _host;
            private readonly AppTheme _theme;
            private readonly Rectangle _surface;
            private readonly DispatcherTimer _timer;
            private double _builtWidth = -1;
            private double _builtHeight = -1;
            private int _builtStamp = -1;

            public BackdropHost(Grid host, AppTheme theme)
            {
                _host = host;
                _theme = theme;

                _surface = new Rectangle
                {
                    IsHitTestVisible = false,
                    SnapsToDevicePixels = true
                };
                _surface.Tag = BackdropTag;
                RenderOptions.SetBitmapScalingMode(_surface, BitmapScalingMode.LowQuality);
                _surface.SizeChanged += delegate { OnSizeChanged(); };
                _host.SizeChanged += delegate { OnSizeChanged(); };

                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(RebuildDelayMs)
                };
                _timer.Tick += delegate
                {
                    _timer.Stop();
                    Build();
                };

                ApplyRoundedClip(_surface);
            }

            public FrameworkElement Element
            {
                get { return _surface; }
            }

            public void RequestRebuild(bool immediate)
            {
                if (immediate && _surface.Fill == null)
                {
                    Build();
                    return;
                }

                _timer.Stop();
                _timer.Start();
            }

            private void OnSizeChanged()
            {
                // 首次拿到尺寸时立刻烘焙，避免开场先看到一帧被拉伸的空白背景
                RequestRebuild(_surface.Fill == null);
            }

            private void Build()
            {
                double width = _host.ActualWidth;
                double height = _host.ActualHeight;
                if (width < 4 || height < 4)
                {
                    // 宿主还没排过版（主窗口构造期间就是这样），等 SizeChanged 再来
                    width = _surface.ActualWidth;
                    height = _surface.ActualHeight;
                }

                if (width < 4 || height < 4)
                {
                    return;
                }

                if (_surface.Fill != null
                    && _builtStamp == _stamp
                    && Math.Abs(width - _builtWidth) < 0.5
                    && Math.Abs(height - _builtHeight) < 0.5)
                {
                    return;
                }

                BitmapSource bitmap = Backdrop.Render(width, height, _theme, _settings);
                if (bitmap == null)
                {
                    return;
                }

                ImageBrush brush = new ImageBrush(bitmap) { Stretch = Stretch.Fill };
                brush.Freeze();
                _surface.Fill = brush;
                _builtWidth = width;
                _builtHeight = height;
                _builtStamp = _stamp;
            }

            /// <summary>
            /// 按窗口圆角裁剪。窗口的圆角是画出来的（底色 Border 的 CornerRadius），
            /// 背景层却是方的，不裁就会把两个上角补成直角。
            /// </summary>
            private static void ApplyRoundedClip(FrameworkElement element)
            {
                EventHandler update = delegate
                {
                    // 插入时还没走过布局，ActualWidth 为 0；此时若把空矩形设成 Clip，
                    // 背景层会在第一次布局前整个不可见，因此尺寸为 0 时先不裁。
                    if (element.ActualWidth <= 0 || element.ActualHeight <= 0)
                    {
                        return;
                    }

                    element.Clip = new RectangleGeometry(
                        new Rect(0, 0, element.ActualWidth, element.ActualHeight),
                        CornerRadius,
                        CornerRadius);
                };

                element.SizeChanged += delegate { update(element, EventArgs.Empty); };
                update(element, EventArgs.Empty);
            }
        }
    }
}
