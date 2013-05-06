namespace Ecng.Xaml.Converters
{
	using System;
	using System.Windows.Data;
	using System.Windows.Media;

	public class ColorToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var brush = value as SolidColorBrush;

			if (brush == null)
				return Colors.Black;

			return brush.Color;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return new SolidColorBrush((Color)value);
		}
	}
}