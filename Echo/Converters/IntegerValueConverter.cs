using System;
using System.Globalization;
using System.Windows.Data;

namespace Echo.Converters
{
    public class IntegerValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Convert 'value' to double
            if (!double.TryParse(value?.ToString(), out double baseValue))
            {
                // fallback or default value if parse fails
                baseValue = 12.0;
            }

            // Convert 'parameter' to double
            if (!double.TryParse(parameter?.ToString(), out double offset))
            {
                offset = 0.0;
            }

            return baseValue + offset;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}