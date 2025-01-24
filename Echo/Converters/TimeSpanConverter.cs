using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Echo.Converters
{
    class TimeSpanConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                // For videos under 1 hour, show MM:SS
                if (timeSpan.Hours == 0)
                {
                    return timeSpan.ToString(@"mm\:ss");
                }
                // For videos over 1 hour, show HH:MM:SS
                return timeSpan.ToString(@"hh\:mm\:ss");
            }
            return "00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
