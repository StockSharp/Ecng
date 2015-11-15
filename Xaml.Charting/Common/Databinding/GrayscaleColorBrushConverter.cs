using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Ecng.Xaml.Charting.Common.Databinding
{
    public class GrayscaleColorBrushConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var initialColor = new Color();

            var brush = value as SolidColorBrush;
            if (brush != null)
            {
                initialColor = brush.Color;
            }
            else if (value is Color)
            {
                initialColor = (Color) value;
            }
            
            var greyScale = initialColor.R*0.299 + initialColor.G*0.587 + initialColor.B*0.114;
            var color = greyScale > 128 ? Colors.Black : Colors.White;

            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
