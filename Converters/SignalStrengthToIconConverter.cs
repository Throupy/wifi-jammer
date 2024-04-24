using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JammerV1.Converters
{
    public class SignalStrengthToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int strength)
            {
                if (strength <= 34) return App.Current.FindResource("wifi_1_regular");
                if (strength <= 67) return App.Current.FindResource("wifi_2_regular");
                return App.Current.FindResource("wifi_3_regular");
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
