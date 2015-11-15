using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Ecng.Xaml.Charting.Common.Databinding
{
    public class AllTrueMultiConverter: IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
           return values.OfType<Boolean>().Any(val => !val);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
