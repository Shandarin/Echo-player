using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Echo.Converters
{
    public class StringEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;
            return value.ToString().Equals(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果是从复选框状态转换回来
            if (value is bool boolValue && boolValue && parameter != null)
            {
                // 当选中时返回参数值
                return parameter.ToString();
            }

            // 当未选中时返回null或默认值
            return null;
        }
    }
}
