namespace Ecng.Xaml.Converters
{
	using System;

	using Ecng.Common;

	public class DateTimeOffsetConverter : DateTimeBaseConverter<DateTimeOffset>
	{
		protected override DateTimeOffset ToLocalTime(DateTimeOffset input, TimeZoneInfo tz) => input.Convert(tz);
	}
}