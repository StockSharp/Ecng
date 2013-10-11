namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	using Ecng.Common;

	public class BoolToOpacityConverter : IValueConverter
	{
		public BoolToOpacityConverter()
		{
			FalseOpacityValue = 1.0;
			TrueOpacityValue = 0.5;
		}

		public double TrueOpacityValue { get; set; }
		public double FalseOpacityValue { get; set; }

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var param = parameter == null || parameter.To<bool>();
			var val = (bool)value;

			return val == param ? TrueOpacityValue : FalseOpacityValue;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}