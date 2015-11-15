using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace Ecng.Xaml.Charting
{
    public class DoubleArrayToDoubleCollectionConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var array = value as IEnumerable<double>;

            var result = new DoubleCollection();

            if (array != null)
            {
                foreach (var item in array)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
