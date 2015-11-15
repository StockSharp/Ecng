using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;

namespace Ecng.Xaml.Charting
{
    public class BooleanToOrientationConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isHorizontal = (bool) value;

            return isHorizontal ? Orientation.Horizontal : Orientation.Vertical;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
