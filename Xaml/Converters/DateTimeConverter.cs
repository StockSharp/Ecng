namespace Ecng.Xaml.Converters
{
	using System;

	public class DateTimeConverter : DateTimeBaseConverter<DateTime>
	{
		protected override DateTime ToLocalTime(DateTime input, TimeZoneInfo tz) => TimeZoneInfo.ConvertTime(input, tz);
	}
}