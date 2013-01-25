namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	using Ecng.Common;

	public class StringConverter : IMultiValueConverter
	{
		object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var str = (string)parameter;
			return str == null ? null : str.Put(values);
		}

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}