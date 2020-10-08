namespace Ecng.Xaml.Converters
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Data;
	using System.Globalization;

	public class ValueConverterGroup : List<IValueConverter>, IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> this.Aggregate(value, (current, converter) => converter.Convert(current, targetType, parameter, culture));

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}
}
