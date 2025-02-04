using Echo.Mappers;
using Echo.Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Echo.Converters
{
    public class AutoDetectLanguageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var language = value as string;
            if (string.IsNullOrWhiteSpace(language))
            {
                return LangResx.Auto_Detect;
            }

            string displayName = LanguageMapper.GetLanguageDisplayName(language);
            return $"{LangResx.Auto_Detect} ({displayName})";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
