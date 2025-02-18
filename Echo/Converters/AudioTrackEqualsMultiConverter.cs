using Echo.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Echo.Converters
{
    public class AudioTrackEqualsMultiConverter : IMultiValueConverter
    {
        // values[0]为 SelectedAudioTrack，values[1] 为当前菜单项对应的 AudioTrackInfo
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is AudioTrackInfo selected && values[1] is AudioTrackInfo current)
            {
                return selected.Id == current.Id;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
