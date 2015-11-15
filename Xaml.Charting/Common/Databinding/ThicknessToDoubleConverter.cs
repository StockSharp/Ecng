using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Convert <see cref="Thickness"/> value to double by taking mean of its values
    /// </summary>
    public class ThicknessToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var thickness = (Thickness)value;

            var name = (string)parameter;
            switch (name)
            {
                case "Top":
                    return thickness.Top;
                case "Bottom":
                    return thickness.Bottom;
                case "Left":
                    return thickness.Left;
                case "Right":
                    return thickness.Right;
                default:
                    return (thickness.Left + thickness.Right + thickness.Top + thickness.Bottom) / 4;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
