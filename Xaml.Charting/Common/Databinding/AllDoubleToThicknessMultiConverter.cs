using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Ecng.Xaml.Charting.Common.Databinding
{
    public class AllDoubleToThicknessMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return new Thickness(values.OfType<double>().Sum());
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
