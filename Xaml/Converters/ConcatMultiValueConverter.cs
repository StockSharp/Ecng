namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Linq;
	using System.Windows.Data;

	using Ecng.Common;

	public class ConcatMultiValueConverter : IMultiValueConverter
	{
		object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			return values.Select(v => v.To<string>()).Join(" ");
		}

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}