namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	public class BoolToStringConverter : IValueConverter
	{
		public string FalseValue { get; set; }

		public string TrueValue { get; set; }

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value as bool?) == true ? TrueValue : FalseValue;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (string)value == TrueValue;
		}
	}
}
