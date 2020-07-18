namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;
	using System.Linq;

	using Ecng.Common;

	public class FormattingMultiConverter : IMultiValueConverter
	{
		public string FormatString;

		object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (parameter != null && !(parameter is string))
				return Binding.DoNothing;

			if (values.Any(v => ReferenceEquals(v, DependencyProperty.UnsetValue)))
				return Binding.DoNothing;

			var formatString = parameter as string;

			if (formatString.IsEmpty())
				formatString = FormatString;

			return string.Format(culture, formatString, values);
		}

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}
}