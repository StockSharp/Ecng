namespace Ecng.Xaml.Converters
{
	using System;
	using System.Windows.Data;
	using System.Windows.Media;

	public class ColorToBrushConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var brush = value as SolidColorBrush;
			return brush == null ? Colors.Black : brush.Color;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return new SolidColorBrush((Color)value);
		}
	}
}