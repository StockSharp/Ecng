namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Net;
	using System.Windows.Data;

	using Ecng.Common;

	public class IpAddressConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value.To<string>();
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value.To<IPAddress>();
		}
	}
}