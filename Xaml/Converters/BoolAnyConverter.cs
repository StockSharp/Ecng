using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Ecng.Xaml.Converters
{
	public class BoolAnyConverter : IMultiValueConverter
	{
		public bool Value { get; set; } = true;

		object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture) 
			=> values.Cast<bool>().Any(v => v == Value);

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}
}
