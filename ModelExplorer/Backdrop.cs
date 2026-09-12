using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ModelExplorer
{
    /// <summary>背景图适配方式。</summary>
    public enum BackdropFit
    {
        /// <summary>等比放大到铺满窗口，超出部分裁掉（默认，不留空）。</summary>
        Cover,

        /// <summary>等比缩放到完整可见，四周用极光底色补齐。</summary>
        Fill,

        /// <summary>不缩放，原尺寸居中，四周用极光底色补齐。</summary>
        Center,

        /// <summary>非等比拉伸铺满，可能变形。</summary>
        Stretch
    }

    /// <summary>背景渲染参数，只装已经归一化过的值（来自 AppConfig 或设置窗口的实时预览）。</summary>
    public class BackdropSettings
    {
        public string ImagePath { get; set; }
        public BackdropFit Fit { get; set; }
        public int Blur { get; set; }
        public int Darken { get; set; }

        public static BackdropSettings FromConfig(AppConfig config)
        {
            BackdropSettings settings = new BackdropSettings();
            settings.Fit = BackdropFit.Cover;
            settings.Blur = AppConfig.DefaultBackgroundBlur;
            settings.Darken = AppConfig.DefaultBackgroundDarken;

            if (config == null)
            {
                return settings;
            }

            settings.ImagePath = string.IsNullOrEmpty(config.BackgroundImage)
                ? null
                : config.BackgroundImage.Trim();
            settings.Fit = ParseFit(config.BackgroundFitValue);
            settings.Blur = config.BackgroundBlurValue;
            settings.Darken = config.BackgroundDarkenValue;
            return settings;
        }

        public static BackdropFit ParseFit(string token)
        {
            if (token == "fill")
            {
                return BackdropFit.Fill;
            }
            if (token == "center")
            {
                return BackdropFit.Center;
            }
            if (token == "stretch")
            {
                return BackdropFit.Stretch;
            }
            return BackdropFit.Cover;
        }

        public static string FitToken(BackdropFit fit)
        {
            if (fit == BackdropFit.Fill)
            {
                return "fill";
            }
            if (fit == BackdropFit.Center)
            {
                return "center";
            }
            if (fit == BackdropFit.Stretch)
            {
                return "stretch";
            }
            return "cover";
        }
    }

    /// <summary>
    /// 把「底色 + 自定义背景图 + 极光」合成为**一张冻结位图**，模糊、暗化与颗粒都在像素上一次性做完。
    ///
    /// 为什么不再用挂在视觉树上的 <c>BlurEffect</c>（v3.2.0 的做法）：模糊挂在视觉树上时，
    /// 窗口每次重排重绘都会重算。实测连续改尺寸时主程序占满一个核（单核 107%），
    /// 而这些开销换来的只是一层静态背景。烘焙成位图后，每帧只是把一张位图贴上去，
    /// 重绘开销与「模糊半径、光斑数量」彻底无关；背景图也因此可以塞进同一条管线。
    ///
    /// 代价与取舍：
    ///   - 窗口尺寸变化后需要重新烘焙（<see cref="Glass"/> 里按 120ms 防抖），
    ///     拖动过程中先拉伸旧位图——对一张模糊过的背景来说，肉眼分辨不出。
    ///   - 烘焙按 DIP 分辨率（96dpi）出图，高分屏下会被放大 1.5 倍左右；
    ///     背景本身是虚化的，这点软化换来的是一半的像素量与内存。
    /// </summary>
    public static class Backdrop
    {
        /// <summary>解码时的最大边长：比这更大的图先降采样，避免为一张壁纸吃掉几十 MB。</summary>
        private const int MaxDecodeEdge = 2560;

        /// <summary>
        /// 暗化下限的目标峰值亮度（sRGB 0～255）。
        ///
        /// 按**最严的一处**反推：对话框里的 11px 说明小字（#B0B0B0）是直接压在背景层上的，
        /// 不在面板里。要让它的对比度守住 4.5:1，背景的相对亮度必须 ≤ 0.0576，
        /// 折成 sRGB 灰阶约 68。这里取 56 而不是 68，是为了给「压暗之后还要叠极光与颗粒」
        /// 留出余量：实测最终背景峰值落在 61 左右，对比度 5.1:1。
        /// 面板上的文字比这更宽松（面板还会再压一层），所以满足这一条，面板必然也满足。
        /// </summary>
        private const double ReadablePeak = 56;

        /// <summary>取分位而不是最大值：极少数镜面高光不该把整张壁纸拖黑。</summary>
        private const double ReadablePercentile = 0.995;

        private static readonly object CacheLock = new object();
        private static string _cachedPath;
        private static int _cachedEdge;
        private static BitmapSource _cachedImage;

        /// <summary>
        /// 最近一次烘焙失败的原因，成功后自动清空。界面在首次布局完成后读它写日志
        /// （用「读快照」而不是事件，静态事件持有窗口引用会拖住已关闭的窗口）。
        /// </summary>
        public static string LastError { get; private set; }

        private static void ReportError(string message)
        {
            LastError = message;
        }

        /// <summary>
        /// 烘焙一张窗口背景位图。返回的位图是冻结的，可跨线程共享。
        /// </summary>
        public static BitmapSource Render(double widthDip, double heightDip, AppTheme theme, BackdropSettings settings)
        {
            if (theme == null || settings == null || widthDip < 4 || heightDip < 4)
            {
                return null;
            }

            return Render(widthDip, heightDip, theme, settings, theme.GlassLevel);
        }

        private static BitmapSource Render(
            double widthDip,
            double heightDip,
            AppTheme theme,
            BackdropSettings settings,
            int glassLevel)
        {
            // 每次烘焙都从「无错误」开始：上一次的失败原因只在本次仍然失败时留下
            LastError = null;
            BitmapSource image = LoadImage(settings.ImagePath);

            // 有背景图时用用户给的模糊值；没有图时沿用按玻璃档位推出的模糊，
            // 这样「不设背景图」的观感与 v3.2.0 完全一致，滑杆也不至于影响不到的画面。
            int blur = settings.Blur;
            if (image == null)
            {
                blur = LevelBlur(glassLevel);
            }

            // 模糊时降采样一半：像素量变四分之一，模糊后放大回去肉眼看不出差别
            int divisor = blur > 0 ? 2 : 1;
            int pixelWidth = Math.Max(1, (int)Math.Round(widthDip / divisor));
            int pixelHeight = Math.Max(1, (int)Math.Round(heightDip / divisor));
            Rect bounds = new Rect(0, 0, pixelWidth, pixelHeight);

            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                DrawAurora(dc, bounds, theme, glassLevel, image != null);
                DrawImage(dc, bounds, image, settings.Fit);
            }

            RenderTargetBitmap target = new RenderTargetBitmap(
                pixelWidth,
                pixelHeight,
                96,
                96,
                PixelFormats.Pbgra32);
            target.Render(visual);

            int stride = pixelWidth * 4;
            byte[] pixels = new byte[stride * pixelHeight];
            target.CopyPixels(pixels, stride, 0);

            int radius = divisor > 1 ? Math.Max(1, blur / divisor) : blur;
            BoxBlur(pixels, pixelWidth, pixelHeight, radius);
            ApplyDarkenAndGrain(pixels, settings.Darken, glassLevel, pixelWidth * pixelHeight);

            BitmapSource result = BitmapSource.Create(
                pixelWidth,
                pixelHeight,
                96,
                96,
                PixelFormats.Pbgra32,
                null,
                pixels,
                stride);
            result.Freeze();
            return result;
        }

        /// <summary>未设置背景图时的模糊半径：沿用 v3.2.0 按玻璃档位取值的那一套。</summary>
        private static int LevelBlur(int glassLevel)
        {
            return glassLevel <= 1 ? 26 : (glassLevel == 2 ? 40 : 56);
        }

        // ------------------------------------------------------------------ 绘制

        private static void DrawAurora(DrawingContext dc, Rect bounds, AppTheme theme, int glassLevel, bool hasImage)
        {
            // 有背景图时极光退成淡淡一层色相点缀：图才是主角，极光只负责把界面和主题绑在一起
            double scale = AuroraScale(glassLevel) * (hasImage ? 0.6 : 1.0);

            dc.DrawRectangle(
                new LinearGradientBrush(
                    AppTheme.Blend(theme.Bg, Colors.White, 0.07),
                    AppTheme.Blend(theme.Bg, Colors.Black, 0.25),
                    new Point(0.10, 0),
                    new Point(0.90, 1)),
                null,
                bounds);

            dc.DrawRectangle(MakeBlob(Colors.White, new Point(0.20, -0.10), 0.90, 0.80, 0.055 * scale), null, bounds);
            dc.DrawRectangle(MakeBlob(Colors.White, new Point(0.88, 1.08), 0.85, 0.75, 0.045 * scale), null, bounds);
            dc.DrawRectangle(MakeBlob(theme.AuroraPrimary, new Point(0.22, 0.04), 0.62, 0.50, 0.055 * scale), null, bounds);
            dc.DrawRectangle(MakeBlob(theme.AuroraSecondary, new Point(0.92, 0.62), 0.60, 0.60, 0.055 * scale), null, bounds);
            dc.DrawRectangle(MakeBlob(theme.AuroraTertiary, new Point(0.58, 1.05), 0.58, 0.45, 0.045 * scale), null, bounds);

            DrawRibbon(dc, bounds, theme.AuroraPrimary, 0.07 * scale, -22, 0.24);
            DrawRibbon(dc, bounds, Colors.White, 0.05 * scale, -22, 0.52);
            DrawRibbon(dc, bounds, theme.AuroraSecondary, 0.06 * scale, -22, 0.80);
        }

        private static Brush MakeBlob(Color color, Point center, double radiusX, double radiusY, double alpha)
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
            return brush;
        }

        /// <summary>
        /// 一道斜向光带。带体比 v3.2.0 更宽：现在不一定有模糊兜底（壁纸模糊可以拉到 0），
        /// 光带本身就要足够柔，否则会在窗口上留下一条可见的直边。
        /// </summary>
        private static void DrawRibbon(DrawingContext dc, Rect bounds, Color color, double alpha, double degrees, double center)
        {
            LinearGradientBrush brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            brush.GradientStops.Add(new GradientStop(AppTheme.WithAlpha(color, 0), Math.Max(0, center - 0.20)));
            brush.GradientStops.Add(new GradientStop(AppTheme.WithAlpha(color, alpha), center - 0.04));
            brush.GradientStops.Add(new GradientStop(AppTheme.WithAlpha(color, alpha), center + 0.04));
            brush.GradientStops.Add(new GradientStop(AppTheme.WithAlpha(color, 0), Math.Min(1, center + 0.20)));
            brush.Freeze();

            // 旋转后的矩形盖不满画布，先放大再转，四角才不会露出上一层
            Rect expanded = bounds;
            expanded.Inflate(bounds.Width * 0.4, bounds.Height * 0.4);
            dc.PushTransform(new RotateTransform(degrees, bounds.Width / 2, bounds.Height / 2));
            dc.DrawRectangle(brush, null, expanded);
            dc.Pop();
        }

        /// <summary>按适配方式把背景图画到目标区域；四周留白由极光底色补上。</summary>
        private static void DrawImage(DrawingContext dc, Rect bounds, BitmapSource image, BackdropFit fit)
        {
            if (image == null)
            {
                return;
            }

            double sourceWidth = image.Width;
            double sourceHeight = image.Height;
            if (sourceWidth < 1 || sourceHeight < 1)
            {
                return;
            }

            Rect destination;
            if (fit == BackdropFit.Stretch)
            {
                destination = bounds;
            }
            else
            {
                double scale = fit == BackdropFit.Center
                    ? 1.0
                    : (fit == BackdropFit.Cover
                        ? Math.Max(bounds.Width / sourceWidth, bounds.Height / sourceHeight)
                        : Math.Min(bounds.Width / sourceWidth, bounds.Height / sourceHeight));

                double width = sourceWidth * scale;
                double height = sourceHeight * scale;
                destination = new Rect(
                    (bounds.Width - width) / 2,
                    (bounds.Height - height) / 2,
                    width,
                    height);
            }

            // 覆盖模式会超出画布，裁剪掉溢出部分（DrawingContext 不会自己裁）
            dc.PushClip(new RectangleGeometry(bounds));
            dc.DrawImage(image, destination);
            dc.Pop();
        }

        // ------------------------------------------------------------------ 像素处理

        /// <summary>
        /// 三遍分离式盒式模糊。三遍盒模糊已经非常接近高斯，但每遍都是 O(n) 的滑动窗口，
        /// 比真正的高斯卷积快一个量级；边缘用钳位采样，因此不会像 BlurEffect 那样在窗口四边淡出。
        /// </summary>
        private static void BoxBlur(byte[] pixels, int width, int height, int radius)
        {
            if (radius <= 0 || width < 2 || height < 2)
            {
                return;
            }

            int window = radius * 2 + 1;
            byte[] scratch = new byte[pixels.Length];
            for (int pass = 0; pass < 3; pass++)
            {
                BlurHorizontal(pixels, scratch, width, height, radius, window);
                BlurVertical(scratch, pixels, width, height, radius, window);
            }
        }

        private static void BlurHorizontal(byte[] src, byte[] dst, int width, int height, int radius, int window)
        {
            int stride = width * 4;
            for (int y = 0; y < height; y++)
            {
                int row = y * stride;
                int sumB = 0, sumG = 0, sumR = 0, sumA = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int x = k < 0 ? 0 : (k >= width ? width - 1 : k);
                    int i = row + x * 4;
                    sumB += src[i];
                    sumG += src[i + 1];
                    sumR += src[i + 2];
                    sumA += src[i + 3];
                }

                for (int x = 0; x < width; x++)
                {
                    int o = row + x * 4;
                    dst[o] = (byte)(sumB / window);
                    dst[o + 1] = (byte)(sumG / window);
                    dst[o + 2] = (byte)(sumR / window);
                    dst[o + 3] = (byte)(sumA / window);

                    int outX = x - radius;
                    int inX = x + radius + 1;
                    int oi = row + (outX < 0 ? 0 : outX) * 4;
                    int ii = row + (inX >= width ? width - 1 : inX) * 4;
                    sumB += src[ii] - src[oi];
                    sumG += src[ii + 1] - src[oi + 1];
                    sumR += src[ii + 2] - src[oi + 2];
                    sumA += src[ii + 3] - src[oi + 3];
                }
            }
        }

        private static void BlurVertical(byte[] src, byte[] dst, int width, int height, int radius, int window)
        {
            int stride = width * 4;
            for (int x = 0; x < width; x++)
            {
                int column = x * 4;
                int sumB = 0, sumG = 0, sumR = 0, sumA = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int y = k < 0 ? 0 : (k >= height ? height - 1 : k);
                    int i = column + y * stride;
                    sumB += src[i];
                    sumG += src[i + 1];
                    sumR += src[i + 2];
                    sumA += src[i + 3];
                }

                for (int y = 0; y < height; y++)
                {
                    int o = column + y * stride;
                    dst[o] = (byte)(sumB / window);
                    dst[o + 1] = (byte)(sumG / window);
                    dst[o + 2] = (byte)(sumR / window);
                    dst[o + 3] = (byte)(sumA / window);

                    int outY = y - radius;
                    int inY = y + radius + 1;
                    int oi = column + (outY < 0 ? 0 : outY) * stride;
                    int ii = column + (inY >= height ? height - 1 : inY) * stride;
                    sumB += src[ii] - src[oi];
                    sumG += src[ii + 1] - src[oi + 1];
                    sumR += src[ii + 2] - src[oi + 2];
                    sumA += src[ii + 3] - src[oi + 3];
                }
            }
        }

        /// <summary>
        /// 暗化 + 可读性下限 + 颗粒，一趟扫描做完。
        ///
        /// 下限的由来见 <see cref="ReadablePeak"/>：用户把暗化拉到 0 时，这里按亮度直方图的
        /// 高分位自动补足，所以亮色壁纸也不会让小字糊掉；用户设得比下限更暗时以用户的为准，
        /// 设置项因此永远是「只会更暗」。
        /// </summary>
        private static void ApplyDarkenAndGrain(byte[] pixels, int darkenPercent, int glassLevel, int pixelCount)
        {
            int[] histogram = new int[256];
            for (int i = 0; i < pixelCount; i++)
            {
                int o = i * 4;
                histogram[Luma(pixels[o + 2], pixels[o + 1], pixels[o])]++;
            }

            int threshold = (int)Math.Round(pixelCount * ReadablePercentile);
            int running = 0;
            int peak = 255;
            for (int level = 0; level < 256; level++)
            {
                running += histogram[level];
                if (running >= threshold)
                {
                    peak = level;
                    break;
                }
            }

            double gain = 1.0 - darkenPercent / 100.0;
            if (peak > ReadablePeak)
            {
                double autoGain = ReadablePeak / peak;
                if (autoGain < gain)
                {
                    gain = autoGain;
                }
            }

            // 颗粒：固定种子的噪声，让大面积色块不至于出现色带；幅度按玻璃档位
            int amplitude = glassLevel <= 1 ? 3 : (glassLevel == 2 ? 4 : 5);
            Random random = new Random(20260912);
            for (int i = 0; i < pixelCount; i++)
            {
                int o = i * 4;
                int noise = random.Next(-amplitude, amplitude + 1);
                pixels[o] = Scale(pixels[o], gain, noise);
                pixels[o + 1] = Scale(pixels[o + 1], gain, noise);
                pixels[o + 2] = Scale(pixels[o + 2], gain, noise);
            }
        }

        private static int Luma(byte r, byte g, byte b)
        {
            return (r * 299 + g * 587 + b * 114) / 1000;
        }

        private static byte Scale(byte value, double gain, int noise)
        {
            int result = (int)Math.Round(value * gain) + noise;
            if (result < 0)
            {
                return 0;
            }
            if (result > 255)
            {
                return 255;
            }
            return (byte)result;
        }

        private static double AuroraScale(int glassLevel)
        {
            return glassLevel <= 1 ? 0.7 : (glassLevel == 2 ? 1.0 : 1.25);
        }

        // ------------------------------------------------------------------ 背景图解码

        /// <summary>
        /// 解码背景图。同一个路径 + 同一个解码尺寸只留一份缓存：六个窗口共用一张解码结果，
        /// 换图签才会重新解码。图不存在或解码失败时返回 null，并记录原因。
        /// </summary>
        private static BitmapSource LoadImage(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                if (!File.Exists(path))
                {
                    ReportError("背景图不存在：" + path);
                    return null;
                }

                int edge = DecodeEdge(path);
                lock (CacheLock)
                {
                    if (_cachedImage != null && _cachedPath == path && _cachedEdge == edge)
                    {
                        return _cachedImage;
                    }
                }

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path, UriKind.Absolute);
                // OnLoad：解码后立刻释放文件句柄，不锁住用户的图片
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                if (edge > 0)
                {
                    image.DecodePixelWidth = edge;
                }
                image.EndInit();
                image.Freeze();

                lock (CacheLock)
                {
                    _cachedPath = path;
                    _cachedEdge = edge;
                    _cachedImage = image;
                }

                return image;
            }
            catch (Exception ex)
            {
                ReportError("背景图读取失败：" + ex.Message);
                return null;
            }
        }

        /// <summary>算解码时用的目标宽度：只按长边限制，避免竖图被解码成巨大的竖条。</summary>
        private static int DecodeEdge(string path)
        {
            try
            {
                BitmapFrame frame = BitmapFrame.Create(
                    new Uri(path, UriKind.Absolute),
                    BitmapCreateOptions.DelayCreation,
                    BitmapCacheOption.None);
                int width = frame.PixelWidth;
                int height = frame.PixelHeight;
                int longest = Math.Max(width, height);
                if (longest <= MaxDecodeEdge || longest <= 0)
                {
                    return 0;
                }

                // 返回的是「宽」方向的解码尺寸：竖图按比例折算，长边才会落在 MaxDecodeEdge
                return Math.Max(1, (int)Math.Round(width * (double)MaxDecodeEdge / longest));
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>仅测试用：丢掉解码缓存。</summary>
        public static void ResetCache()
        {
            lock (CacheLock)
            {
                _cachedPath = null;
                _cachedImage = null;
                _cachedEdge = 0;
            }
        }
    }
}
