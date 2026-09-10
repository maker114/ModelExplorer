using System;
using System.Globalization;
using System.Windows.Data;

namespace ModelExplorer
{
    /// <summary>
    /// 类型徽标的填充色：把 <see cref="ModelKind"/> 映射到当前主题的类型色块。
    /// 替代 v2.4.1 的 <c>ModelFile.TypeBrush</c>（领域模型反向依赖 WPF 的分层缺陷）。
    /// </summary>
    public sealed class KindBadgeFillConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is ModelKind ? TypePalette.FillFor((ModelKind)value) : TypePalette.FillFor(ModelKind.Part);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// 类型徽标的文字色：按填充色亮度自动取深色或浅色，保证始终可读
    /// （黄底用黑字，黑底/灰底用浅色字）。
    /// </summary>
    public sealed class KindBadgeTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is ModelKind ? TypePalette.TextFor((ModelKind)value) : TypePalette.TextFor(ModelKind.Part);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
