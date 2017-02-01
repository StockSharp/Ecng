namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;
	using System.Windows.Media;

	public class BoolToImageConverter : IValueConverter
	{
		public ImageSource TrueValue { get; set; }

		public ImageSource FalseValue { get; set; }

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value as bool?) == true ? TrueValue : FalseValue;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}