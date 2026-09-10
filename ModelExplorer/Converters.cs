using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ModelExplorer
{
    /// <summary>
    /// 把 <see cref="ModelKind"/> 映射为主题中的类型颜色。
    ///
    /// 替代 v2.4.1 中 <c>ModelFile.TypeBrush</c>：领域模型原先直接持有 WPF Brush，
    /// 使扫描逻辑无法脱离 UI 框架测试；现在颜色归属界面层。
    /// </summary>
    public sealed class KindBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ModelKind))
            {
                return ThemeManager.Current.PartBrush;
            }

            ModelKind kind = (ModelKind)value;
            if (kind == ModelKind.Assembly || kind == ModelKind.ThreeMf)
            {
                return ThemeManager.Current.AssemblyBrush;
            }
            if (kind == ModelKind.Stl)
            {
                return ThemeManager.Current.StlBrush;
            }
            return ThemeManager.Current.PartBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
