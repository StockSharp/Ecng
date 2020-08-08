namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	public abstract class DateTimeBaseConverter<TDateTime> : TimeZoneBaseConverter, IMultiValueConverter
		where TDateTime : IFormattable
	{
		object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			//TODO: esper, временный фикс падения при использовании MultiBindings с DataGridTemplateColumn
			//При выделении ячейки обнуляется DataContext.
			if (!(values[0] is TDateTime date))
				return Binding.DoNothing;

			var tz = TryGetTimeZone();

			if (tz is object)
				date = ToLocalTime(date, tz);

			if (values[1] is string format)
				return date.ToString(format, DateTimeFormatInfo.CurrentInfo);

			return date.ToString();
		}

		protected abstract TDateTime ToLocalTime(TDateTime input, TimeZoneInfo tz);

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}