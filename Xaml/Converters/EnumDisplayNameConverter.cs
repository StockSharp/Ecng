namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

	using Ecng.ComponentModel;

	public class EnumDisplayNameConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == DependencyProperty.UnsetValue)
				return Binding.DoNothing;

			if (value == null)
				return string.Empty;

			if (!(value is Enum))
				return Binding.DoNothing;

			return value.GetDisplayName();
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}