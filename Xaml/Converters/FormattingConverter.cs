namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	using Ecng.Common;

	[ValueConversion(typeof(object), typeof(string))]
	public class FormattingConverter : TimeZoneBaseConverter, IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var tz = TryGetTimeZone();

			if (tz is object)
			{
				if (value is DateTime dt)
					value = dt.ToLocalTime();
				else if (value is DateTimeOffset dto)
					value = dto.ToLocalTime();
			}

			return parameter is string s ? string.Format(culture, s, value) : value.To<string>();
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}