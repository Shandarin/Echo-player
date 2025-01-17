using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;

namespace Echo.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Debug.WriteLine("BooleanToVisibilityConverter: " + value);
            if (value is bool boolean)
            {
                //Debug.WriteLine("BooleanToVisibilityConverter: " + boolean);
                return boolean ? Visibility.Visible : Visibility.Collapsed;
            }
            //Debug.WriteLine("BooleanToVisibilityConverter: " + Visibility.Visible);
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return true;
        }
    }
}