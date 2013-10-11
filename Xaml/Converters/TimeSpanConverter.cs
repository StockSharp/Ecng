namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	[ValueConversion(typeof(TimeSpan), typeof(string))]
	public class TimeSpanConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var ts = (TimeSpan)value;
			return parameter == null ? ts.ToString() : ts.ToString((string)parameter, culture);
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}