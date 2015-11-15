using System;
using System.Globalization;
using System.Windows.Data;

namespace Ecng.Xaml.Charting.Common.Databinding
{
    public class IsLastPointInViewportToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool) value ? 1 : 0.7;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}