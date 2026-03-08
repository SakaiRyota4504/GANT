using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TaskManager.Converters
{
    /// <summary>
    /// Bool → Visibility。
    /// ConverterParameter="notempty" にすると string の非空判定になる。
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool visible;

            if (parameter?.ToString() == "notempty")
                visible = !string.IsNullOrWhiteSpace(value as string);
            else
                visible = value is bool b && b;

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v == Visibility.Visible;
    }
}
