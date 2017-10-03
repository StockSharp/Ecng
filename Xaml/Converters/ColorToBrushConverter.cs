namespace Ecng.Xaml.Converters
{
	using System;
	using System.Windows.Data;
	using System.Windows.Media;

	public class ColorToBrushConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value is SolidColorBrush brush ? brush.Color : Colors.Black;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return new SolidColorBrush((Color)value);
		}
	}
}