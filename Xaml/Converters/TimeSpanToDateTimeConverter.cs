namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	public class TimeSpanToDateTimeConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DateTime.Today + (TimeSpan)value;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null ? TimeSpan.Zero : ((DateTime)value).TimeOfDay;
		}
	}
}