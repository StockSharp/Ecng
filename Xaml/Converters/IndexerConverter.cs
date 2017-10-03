namespace Ecng.Xaml.Converters
{
	using System;
	using System.Collections;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

	public class IndexerConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is IList list))
				return DependencyProperty.UnsetValue;

			if (!(parameter is int index))
				return DependencyProperty.UnsetValue;

			if (index >= list.Count)
				return DependencyProperty.UnsetValue;

			return list[index];
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}