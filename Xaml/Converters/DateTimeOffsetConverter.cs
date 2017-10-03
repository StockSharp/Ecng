namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

	public class DateTimeOffsetConverter : IMultiValueConverter
	{
		object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			//TODO: esper, временный фикс падения при использовании MultiBindings с DataGridTemplateColumn
			//При выделении ячейки обнуляется DataContext.
			if (values[0] == null || values[0] == DependencyProperty.UnsetValue)
				return Binding.DoNothing;

			if (!(values[0] is DateTimeOffset date))
				return Binding.DoNothing;

			return values[1] == DependencyProperty.UnsetValue
				? date.ToString()
				: date.ToString((string)values[1]);
		}

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}