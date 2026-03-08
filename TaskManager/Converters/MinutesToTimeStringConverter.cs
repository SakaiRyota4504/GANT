using System;
using System.Globalization;
using System.Windows.Data;

namespace TaskManager.Converters
{
    public class MinutesToTimeStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int minutes && minutes > 0)
            {
                var h = minutes / 60;
                var m = minutes % 60;
                return h > 0 ? $"{h}時間{m:D2}分" : $"{m}分";
            }
            return "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
