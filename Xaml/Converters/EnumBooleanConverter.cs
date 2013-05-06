namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

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

			var parameterValue = Enum.Parse(value.GetType(), parameterString);

			return parameterValue.Equals(value);
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var parameterString = parameter as string;

			if (parameterString == null)
				return DependencyProperty.UnsetValue;

			if (DefaultValueWhenUnchecked && !(bool)value)
				return Enum.GetValues(targetType).GetValue(0);

			return Enum.Parse(targetType, parameterString);
		}
	}
}