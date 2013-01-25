namespace Ecng.Xaml.Converters
{
	using System;

	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

	public class VisibilityToBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (Visibility)value == Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (bool)value ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
