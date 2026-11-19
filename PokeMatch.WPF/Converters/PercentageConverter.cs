using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PokeMatch.WPF.Converters
{
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d && parameter is string p && double.TryParse(p, NumberStyles.Any, CultureInfo.InvariantCulture, out double percentage))
            {
                return d * percentage;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
