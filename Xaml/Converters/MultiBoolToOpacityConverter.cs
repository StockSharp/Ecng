namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Linq;
	using System.Windows.Data;

	using Ecng.Common;

	public class MultiBoolToOpacityConverter : IMultiValueConverter
	{
		public MultiBoolToOpacityConverter()
		{
			FalseOpacityValue = 1.0;
			TrueOpacityValue = 0.5;
		}

		public double TrueOpacityValue { get; set; }
		public double FalseOpacityValue { get; set; }

		object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var param = parameter == null || parameter.To<bool>();

			var val = values.OfType<bool>().All(v => v);

			return val == param ? TrueOpacityValue : FalseOpacityValue;
		}

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}