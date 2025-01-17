using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;

namespace Echo.Converters
{
    public class SliderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double sliderValue)
            {
                // Get the parent element's actual width and calculate the proportional width
                if (parameter is double totalWidth)
                {
                    return (sliderValue / 100.0) * totalWidth;
                }
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

