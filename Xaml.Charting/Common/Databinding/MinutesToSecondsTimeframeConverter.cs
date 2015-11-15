using System;
using System.Globalization;
using System.Windows.Data;

namespace Ecng.Xaml.Charting.Common.Databinding {
    public class MinutesToSecondsTimeframeConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if(!(value is int))
                return -1;

            var tfMinutes = (int)value;
            return tfMinutes * 60;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }
}
