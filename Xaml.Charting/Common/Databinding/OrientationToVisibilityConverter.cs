using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Ecng.Xaml.Charting
{
    public class OrientationToVisibilityConverter:IValueConverter
    {
        private const string InvertionFlag = "INVERSE";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var orienation = (Orientation) value;
            var stringParam = parameter as string;

            var isInverse = string.Equals(stringParam, InvertionFlag, StringComparison.InvariantCultureIgnoreCase); ;
            
            var onHorizontal = isInverse ? Visibility.Collapsed : Visibility.Visible;
            var onVertical = isInverse ? Visibility.Visible : Visibility.Collapsed;

            return orienation == Orientation.Horizontal ? onHorizontal : onVertical;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
