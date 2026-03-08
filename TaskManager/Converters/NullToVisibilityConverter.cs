using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TaskManager.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null || (value is string s && string.IsNullOrWhiteSpace(s));
            bool inverse = parameter?.ToString() == "inverse";
            bool visible = inverse ? isNull : !isNull;
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
