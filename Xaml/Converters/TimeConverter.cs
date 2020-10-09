namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	using Ecng.Common;

	public class TimeConverter : IValueConverter
	{
		private TimeZoneInfo _tz;

		public bool ConvertToLocal
		{
			get => TimeZoneInfo.Local.Equals(_tz);
			set => _tz = value ? TimeZoneInfo.Local : null;
		}

		public string TimeZoneId
		{
			get => _tz.To<string>();
			set => _tz = value.To<TimeZoneInfo>();
		}

		public TimeZoneInfo TimeZone => _tz ?? XamlHelper.GlobalTimeZone;

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var tz = TimeZone;

			switch (value)
			{
				case DateTimeOffset dto:
					return tz is null ? dto : TimeZoneInfo.ConvertTime(dto, tz);
				case DateTime dt:
					return tz is null ? dt : TimeZoneInfo.ConvertTime(dt, tz);
				default:
					return Binding.DoNothing;
			}
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}
}