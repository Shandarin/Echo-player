using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Echo.Converters
{
    public class SentenceButtonVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string errorMessage = values[0] as string;
            if (!string.IsNullOrEmpty(errorMessage))
            {
                return Visibility.Collapsed;
            }

            IEnumerable contentLines = values[1] as IEnumerable;
            if (contentLines == null || !contentLines.Cast<object>().Any())
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
