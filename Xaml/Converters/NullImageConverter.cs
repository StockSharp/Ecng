namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

	public class NullImageConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value ?? DependencyProperty.UnsetValue;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}