namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	[ValueConversion(typeof(TimeSpan), typeof(string))]
	public class TimeSpanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (parameter == null)
				return ((TimeSpan)value).ToString();
			else
				return ((TimeSpan)value).ToString((string)parameter, culture);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}