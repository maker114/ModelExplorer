using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ModelExplorer
{
    /// <summary>
    /// 应用内毛玻璃：在每个窗口的实色底之上铺一层「极光」背景，把这一层整体模糊掉，
    /// 半透明的玻璃面板压在上面，就得到磨砂玻璃的通透感。
    ///
    /// 为什么不用系统亚克力 / Mica：主窗口是 AllowsTransparency=True 的分层窗口
    /// （MainWindow.xaml 第 11～14 行），DWM 的背景模糊对它不生效，那套接口还会
    /// 逼我们放弃逐像素透明、把桌面内容透进来干扰阅读。
    ///
    /// 为什么可以放心用 BlurEffect：极光层是静态的（只有渐变光斑和柔光带），
    /// 模糊只算一次并被 WPF 缓存；玻璃面板本身完全不参与模糊。逐帧重算的实时模糊
    /// （VisualBrush + Effect）在软件渲染的分层窗口下代价太高，这里刻意不做。
    ///
    /// 面板的玻璃感由 <see cref="AppTheme"/> 的半透明画刷提供，本类只负责「被透过去的那层背景」。
    /// </summary>
    public static class Glass
    {
        /// <summary>窗口圆角，与 MainWindow.xaml / UiFactory.CreateWindowChrome 的 14 保持一致。</summary>
        private const double CornerRadius = 14;

        /// <summary>用来在视觉树里认领自己插入的背景层，避免重复叠加。</summary>
        private const string BackdropTag = "ModelExplorer.GlassBackdrop";

        /// <summary>颗粒贴图边长，同时也是平铺步长。</summary>
        private const int GrainSize = 96;

        private static ImageBrush _grainBrush;

        /// <summary>
        /// 把毛玻璃背景层插入宿主（宿主第一个子元素应当是窗口的不透明底色）。
        /// 等级为 0 时只负责移除旧层，观感回到 v3.1.3。
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

            // 插到不透明底色之上、边框描边之下：描边必须留在最上层，否则窗口外框会被糊掉
            host.Children.Insert(Math.Min(1, host.Children.Count), CreateBackdrop(theme, theme.GlassLevel));
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

        private static FrameworkElement CreateBackdrop(AppTheme theme, int level)
        {
            Grid backdrop = new Grid { ClipToBounds = true, IsHitTestVisible = false };
            backdrop.Tag = BackdropTag;

            double radius = BlurRadius(level);

            // 光斑整体放大到窗口之外：模糊会让图层边缘向内淡出，不外扩就会在窗口四边留下暗边
            Grid aurora = new Grid { ClipToBounds = false, Margin = new Thickness(-radius * 2) };

            // 底色：左上略亮、右下压暗的斜向渐变，先给画面一个光源方向
            aurora.Background = new LinearGradientBrush(
                AppTheme.Blend(theme.Bg, Colors.White, 0.07),
                AppTheme.Blend(theme.Bg, Colors.Black, 0.25),
                new Point(0.10, 0),
                new Point(0.90, 1));

            double scale = AuroraScale(level);

            // 中性雾面：磨砂玻璃的散射本身是无色的，这两片撑起「雾」的部分。
            // 刻意比主题色斑更淡——近黑底上直接铺饱和色会糊成脏黄 / 脏绿。
            aurora.Children.Add(MakeBlob(Colors.White, new Point(0.20, -0.10), 0.90, 0.80, 0.055 * scale));
            aurora.Children.Add(MakeBlob(Colors.White, new Point(0.88, 1.08), 0.85, 0.75, 0.045 * scale));

            // 主题色极光：只做色相点缀，透明度压得比中性雾面还低。
            // 对话框那种小窗口里，光斑中心会正好落在可见区域内（主窗口则是被面板盖住的），
            // 所以这里的透明度必须按「最坏情况直接暴露」来定，否则设置窗顶部会糊成一片脏黄。
            aurora.Children.Add(MakeBlob(theme.AuroraPrimary, new Point(0.22, 0.04), 0.62, 0.50, 0.055 * scale));
            aurora.Children.Add(MakeBlob(theme.AuroraSecondary, new Point(0.92, 0.62), 0.60, 0.60, 0.055 * scale));
            aurora.Children.Add(MakeBlob(theme.AuroraTertiary, new Point(0.58, 1.05), 0.58, 0.45, 0.045 * scale));

            aurora.Children.Add(MakeRibbons(theme, scale));

            aurora.Effect = new BlurEffect
            {
                Radius = radius,
                KernelType = KernelType.Gaussian,
                RenderingBias = RenderingBias.Performance
            };

            backdrop.Children.Add(aurora);
            backdrop.Children.Add(new Rectangle
            {
                Fill = GrainBrush,
                Opacity = GrainOpacity(level),
                IsHitTestVisible = false
            });

            ApplyRoundedClip(backdrop);
            return backdrop;
        }

        /// <summary>
        /// 一个柔光圆斑。中心不透明度刻意压得很低：极光是给玻璃当背衬的，
        /// 一旦显眼就会盖过面板里的文字。
        /// </summary>
        private static Rectangle MakeBlob(Color color, Point center, double radiusX, double radiusY, double alpha)
        {
            RadialGradientBrush brush = new RadialGradientBrush
            {
                MappingMode = BrushMappingMode.RelativeToBoundingBox,
                Center = center,
                GradientOrigin = center,
                RadiusX = radiusX,
                RadiusY = radiusY
            };
            brush.GradientStops.Add(new GradientStop(AppTheme.WithAlpha(color, alpha), 0));
            brush.GradientStops.Add(new GradientStop(AppTheme.WithAlpha(color, alpha * 0.45), 0.55));
            brush.GradientStops.Add(new GradientStop(AppTheme.WithAlpha(color, 0), 1));
            brush.Freeze();

            return new Rectangle { Fill = brush };
        }

        /// <summary>
        /// 几道斜向光带。给模糊提供一点「有形状」的输入：柔和的圆斑无论模糊与否看起来都一样，
        /// 而宽约 60px 的窄带被半径 26～56 的高斯糊过之后会变成柔和光晕，
        /// 面板压上去才看得出「透过去的是被糊过的光」。
        /// </summary>
        private static Grid MakeRibbons(AppTheme theme, double scale)
        {
            Grid ribbons = new Grid { ClipToBounds = false, Opacity = scale };
            ribbons.Children.Add(MakeRibbon(theme.AuroraPrimary, 0.07, -22, 0.24));
            ribbons.Children.Add(MakeRibbon(Colors.White, 0.05, -22, 0.52));
            ribbons.Children.Add(MakeRibbon(theme.AuroraSecondary, 0.06, -22, 0.80));
            return ribbons;
        }

        /// <summary>一道斜向光带：中心 4% 宽度是实体，两侧各留 10% 作为柔和过渡。</summary>
        private static Rectangle MakeRibbon(Color color, double alpha, double degrees, double center)
        {
            LinearGradientBrush brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            brush.GradientStops.Add(new GradientStop(AppTheme.WithAlpha(color, 0), Math.Max(0, center - 0.12)));
            brush.GradientStops.Add(new GradientStop(AppTheme.WithAlpha(color, alpha), center - 0.02));
            brush.GradientStops.Add(new GradientStop(AppTheme.WithAlpha(color, alpha), center + 0.02));
            brush.GradientStops.Add(new GradientStop(AppTheme.WithAlpha(color, 0), Math.Min(1, center + 0.12)));
            brush.Freeze();

            return new Rectangle
            {
                Fill = brush,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(degrees)
            };
        }

        /// <summary>
        /// 细腻颗粒。真实磨砂玻璃的散射不是纯模糊，还有一层极细的噪点；
        /// 固定随机种子，保证每次启动颗粒一致，截图比对才有意义。
        /// </summary>
        private static ImageBrush GrainBrush
        {
            get
            {
                if (_grainBrush == null)
                {
                    byte[] pixels = new byte[GrainSize * GrainSize * 4];
                    Random random = new Random(20260911);
                    for (int i = 0; i < GrainSize * GrainSize; i++)
                    {
                        byte value = (byte)random.Next(0, 256);
                        pixels[i * 4] = value;
                        pixels[i * 4 + 1] = value;
                        pixels[i * 4 + 2] = value;
                        pixels[i * 4 + 3] = 0xFF;
                    }

                    BitmapSource source = BitmapSource.Create(
                        GrainSize,
                        GrainSize,
                        96,
                        96,
                        PixelFormats.Bgra32,
                        null,
                        pixels,
                        GrainSize * 4);

                    ImageBrush brush = new ImageBrush(source)
                    {
                        TileMode = TileMode.Tile,
                        ViewportUnits = BrushMappingMode.Absolute,
                        Viewport = new Rect(0, 0, GrainSize, GrainSize),
                        Stretch = Stretch.None
                    };
                    brush.Freeze();
                    _grainBrush = brush;
                }

                return _grainBrush;
            }
        }

        private static double BlurRadius(int level)
        {
            return level <= 1 ? 26 : (level == 2 ? 40 : 56);
        }

        private static double AuroraScale(int level)
        {
            return level <= 1 ? 0.7 : (level == 2 ? 1.0 : 1.25);
        }

        private static double GrainOpacity(int level)
        {
            return level <= 1 ? 0.02 : (level == 2 ? 0.03 : 0.04);
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
