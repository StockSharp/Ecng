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
			var list = value as IList;

			if (list == null)
				return DependencyProperty.UnsetValue;

			var index = parameter as int?;

			if (index == null)
				return DependencyProperty.UnsetValue;

			if (index >= list.Count)
				return DependencyProperty.UnsetValue;

			return list[index.Value];
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}