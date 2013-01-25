namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	[ValueConversion(typeof(object), typeof(string))]
	public class FormattingConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (parameter is string)
				return string.Format(culture, (string)parameter, value);
			else
				return value.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}