using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace D365POS.Converters
{
    public class BoolToGrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? Colors.LightGray : Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;
        }
    }
}
