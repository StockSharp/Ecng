using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Ecng.Xaml.Converters
{
	public class BoolAllConverter : IMultiValueConverter
	{
		public bool Value { get; set; } = true;

		object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture) 
			=> values.Cast<bool>().All(v => v == Value);

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}
}
