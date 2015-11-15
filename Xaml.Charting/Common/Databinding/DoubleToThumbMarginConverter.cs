using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Ecng.Xaml.Charting
{
    public class DoubleToThumbMarginConverter : IValueConverter
    {
        private const string IsVertical = "VERTICAL";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var thumbSize = (double) value;
            var stringParam = parameter as string;

            var isVertical = string.Equals(stringParam, IsVertical, StringComparison.InvariantCultureIgnoreCase);

            var margin = thumbSize/2;

            return isVertical ? new Thickness(0, -margin, 0, -margin) : new Thickness(-margin, 0, -margin, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
