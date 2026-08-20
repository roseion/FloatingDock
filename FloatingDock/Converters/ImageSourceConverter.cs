using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FloatingDock.Converters
{
    public class ImageSourceConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value as ImageSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
