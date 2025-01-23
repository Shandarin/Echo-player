using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Echo.Converters
{
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                return (index + 1).ToString(); // 索引从 1 开始
            }
            return "0"; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
