using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting
{
    public class AxisAlignmentToTransformOriginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var alignment = (AxisAlignment) value;

            switch (alignment)
            {
                case AxisAlignment.Bottom:
                    return new Point(0, 0);
                case AxisAlignment.Top:
                    return new Point(0, 1);
                case AxisAlignment.Right:
                    return new Point(0, 0);
                case AxisAlignment.Left:
                    return new Point(1, 0);
                default:
                    return new Point();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
