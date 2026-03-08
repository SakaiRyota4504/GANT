using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TaskManager.Models;

namespace TaskManager.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskStatus status)
            {
                switch (status)
                {
                    case TaskStatus.Pending:    return new SolidColorBrush(Color.FromRgb(0xB2, 0xBE, 0xC3)); // グレー
                    case TaskStatus.InProgress: return new SolidColorBrush(Color.FromRgb(0x34, 0x98, 0xDB)); // ブルー
                    case TaskStatus.Paused:     return new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12)); // オレンジ
                    case TaskStatus.Completed:  return new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60)); // グリーン
                    case TaskStatus.Skipped:    return new SolidColorBrush(Color.FromRgb(0xD6, 0x3D, 0x3D)); // レッド
                    default:                    return Brushes.Transparent;
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
