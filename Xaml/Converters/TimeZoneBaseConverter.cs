namespace Ecng.Xaml.Converters
{
	using System;

	using Ecng.Common;

	public abstract class TimeZoneBaseConverter
	{
		private TimeZoneInfo _tz;
		
		public bool ConvertToLocal
		{
			get => _tz == TimeZoneInfo.Local;
			set => _tz = value ? TimeZoneInfo.Local : null;
		}

		public string TimeZoneId
		{
			get => _tz.To<string>();
			set => _tz = value.To<TimeZoneInfo>();
		}

		protected TimeZoneInfo TryGetTimeZone() => _tz ?? XamlHelper.GlobalTimeZone;
	}
}