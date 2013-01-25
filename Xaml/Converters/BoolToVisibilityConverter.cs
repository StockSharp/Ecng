namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

	using Ecng.Common;

	public class BoolToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var param = parameter == null ? true : parameter.To<bool>();
			var val = (bool)value;

			return val == param ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((Visibility)value == Visibility.Visible);
		}
	}
}