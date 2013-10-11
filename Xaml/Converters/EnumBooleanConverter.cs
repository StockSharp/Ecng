namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Linq;
	using System.Windows;
	using System.Windows.Data;

	using Ecng.Common;

	public class EnumBooleanConverter : IValueConverter
	{
		public bool DefaultValueWhenUnchecked { get; set; }

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var parameterString = parameter as string;

			if (parameterString == null)
				return DependencyProperty.UnsetValue;

			if (Enum.IsDefined(value.GetType(), value) == false)
				return DependencyProperty.UnsetValue;

			var parameterValue = parameterString.To(value.GetType());

			return parameterValue.Equals(value);
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var parameterString = parameter as string;

			if (parameterString == null)
				return DependencyProperty.UnsetValue;

			if (DefaultValueWhenUnchecked && !(bool)value)
				return targetType.GetValues().ElementAt(0);

			return parameterString.To(targetType);
		}
	}
}