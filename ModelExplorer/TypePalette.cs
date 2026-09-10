using System;
using System.Windows.Media;

namespace ModelExplorer
{
    /// <summary>
    /// 类型徽标配色。
    ///
    /// v3.1.0 起类型徽标由“淡色底 + 彩色字”改为“实心色块 + 自动对比色文字”
    /// （与黑白灰黄视觉体系一致）：填充色取自主题，文字颜色按填充色亮度自动
    /// 取深色或浅色，因此任何主题下都不会出现低对比度文字。
    /// </summary>
    public static class TypePalette
    {
        /// <summary>亮度阈值：高于该值用深色文字，低于则用浅色文字。</summary>
        private const double LightFillThreshold = 0.55;

        public static Color FillColor(ModelKind kind)
        {
            AppTheme theme = ThemeManager.Current;
            if (kind == ModelKind.Assembly || kind == ModelKind.ThreeMf)
            {
                return theme.AssemblyColor;
            }
            if (kind == ModelKind.Stl)
            {
                return theme.StlColor;
            }
            return theme.PartColor;
        }

        public static Color TextColor(Color fill)
        {
            AppTheme theme = ThemeManager.Current;
            return RelativeLuminance(fill) > LightFillThreshold ? theme.OnAccent : theme.SidebarText;
        }

        public static Brush FillFor(ModelKind kind)
        {
            return AppTheme.MakeBrush(FillColor(kind));
        }

        public static Brush TextFor(ModelKind kind)
        {
            return AppTheme.MakeBrush(TextColor(FillColor(kind)));
        }

        /// <summary>工程名整理弹窗按“零件 / 装配体导出 / STL”文本区分类型。</summary>
        public static ModelKind KindForFileType(string fileType)
        {
            if (fileType == "零件")
            {
                return ModelKind.Part;
            }
            if (fileType == "装配体导出")
            {
                return ModelKind.Assembly;
            }
            return ModelKind.Stl;
        }

        public static Brush FillForFileType(string fileType)
        {
            return FillFor(KindForFileType(fileType));
        }

        public static Brush TextForFileType(string fileType)
        {
            return TextFor(KindForFileType(fileType));
        }

        /// <summary>感知亮度（sRGB 加权），用于挑选对比文字色。</summary>
        public static double RelativeLuminance(Color color)
        {
            return (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;
        }
    }
}
