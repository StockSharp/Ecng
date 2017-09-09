namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;

	public static class TimeHelper
	{
		private static readonly Stopwatch _timer;
		private static readonly DateTime _start;
		private static DateTime _startWithOffset;

		static TimeHelper()
		{
			_timer = new Stopwatch();
			_start = DateTime.Now;
			_timer.Start();

			NowOffset = TimeSpan.Zero;
		}

		/// <summary>
		/// Текущее время.
		/// </summary>
		public static DateTime Now => _startWithOffset + _timer.Elapsed;

		public static DateTimeOffset NowWithOffset => Now.ApplyTimeZone(TimeZoneInfo.Local);

		private static TimeSpan _nowOffset;

		/// <summary>
		/// Временное смещение. Неоходимо устанавливать, когда торговая программа работает с неточными настройками локального времени.
		/// Значение <see cref="Now"/> будет корректироваться в зависимости от установленного значения.
		/// </summary>
		public static TimeSpan NowOffset
		{
			get => _nowOffset;
			set
			{
				_nowOffset = value;
				_startWithOffset = _start + value;
			}
		}

		/// <summary>
		/// Смещение временной зоны.
		/// </summary>
		public static TimeSpan TimeZoneOffset { get; set; } = TimeZoneInfo.Local.BaseUtcOffset;

		/// <summary>
		/// Синхронизировать <see cref="NowOffset"/> между локальным временем на компьютере и NTP сервером в интернете.
		/// </summary>
		/// <param name="timeout">Таймаут синхронизации в милисекундах.</param>
		public static void SyncMarketTime(int timeout = 5000)
		{
			var dtNow = _start + _timer.Elapsed;
			NowOffset = new NtpClient().GetLocalTime(TimeZoneInfo.Local, timeout).Subtract(dtNow);
		}

		/// <summary>
		/// Gets the weeks.
		/// </summary>
		/// <value>The weeks.</value>
		public static double TotalWeeks(this TimeSpan value)
		{
			return (double)value.Ticks / TicksPerWeek;
		}

		/// <summary>
		/// Gets the months.
		/// </summary>
		/// <value>The months.</value>
		public static double TotalMonths(this TimeSpan value)
		{
			return (double)value.Ticks / TicksPerMonth;
		}

		/// <summary>
		/// Gets the years.
		/// </summary>
		/// <value>The years.</value>
		public static double TotalYears(this TimeSpan value)
		{
			return (double)value.Ticks / TicksPerYear;
		}

		/// <summary>
		/// Gets the centuries.
		/// </summary>
		/// <value>The centuries.</value>
		public static double TotalCenturies(this TimeSpan value)
		{
			return (double)value.Ticks / TicksPerCentury;
		}

		/// <summary>
		/// Gets the milleniums.
		/// </summary>
		/// <value>The milleniums.</value>
		public static double TotalMilleniums(this TimeSpan value)
		{
			return (double)value.Ticks / TicksPerMillenium;
		}

		///// <summary>
		///// Represents the one tick.
		///// </summary>
		//public const int Tick = 1;

		/// <summary>
		/// Represents the number of ticks in 1 nanosecond. This field is constant.
		/// </summary>
		public const double TicksPerNanosecond = 1.0 / NanosecondsPerTick;

		/// <summary>
		/// Represents the number of nanoseconds in 1 tick. This field is constant.
		/// </summary>
		public const long NanosecondsPerTick = 100;

		/// <summary>
		/// Represents the number of ticks in 1 microsecond. This field is constant.
		/// </summary>
		public const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;

		/// <summary>
		/// Represents the number of ticks in 1 week. This field is constant.
		/// </summary>
		public const long TicksPerWeek = TimeSpan.TicksPerDay * 7;

		/// <summary>
		/// Represents the number of ticks in 1 month. This field is constant.
		/// </summary>
		public const long TicksPerMonth = TimeSpan.TicksPerDay * 30;

		/// <summary>
		/// Represents the number of ticks in 1 year. This field is constant.
		/// </summary>
		public const long TicksPerYear = TimeSpan.TicksPerDay * 365;

		/// <summary>
		/// Represents the number of ticks in 1 century. This field is constant.
		/// </summary>
		public const long TicksPerCentury = TicksPerYear * 100;

		/// <summary>
		/// Represents the number of ticks in 1 millenium. This field is constant.
		/// </summary>
		public const long TicksPerMillenium = TicksPerCentury * 10;

		public static readonly TimeSpan LessOneDay = TimeSpan.FromTicks(TimeSpan.TicksPerDay - 1);

		public static int GetMicroseconds(this TimeSpan ts)
		{
			return (int)(TicksToMicroseconds(ts.Ticks) % 1000);
		}

		public static int GetMicroseconds(this DateTime dt)
		{
			return (int)(TicksToMicroseconds(dt.Ticks) % 1000);
		}

		public static int GetNanoseconds(this TimeSpan ts)
		{
			return GetNanoseconds(ts.Ticks);
		}

		public static int GetNanoseconds(this DateTime dt)
		{
			return GetNanoseconds(dt.Ticks);
		}

		public static int GetNanoseconds(this long ticks)
		{
			return (int)((ticks % 10) * NanosecondsPerTick);
		}

		public static long ToNanoseconds(this TimeSpan ts)
		{
			return TicksToNanoseconds(ts.Ticks);
		}

		public static long ToNanoseconds(this DateTime dt)
		{
			return TicksToNanoseconds(dt.Ticks);
		}

		public static long NanosecondsToTicks(this long nanoseconds)
		{
			return nanoseconds / NanosecondsPerTick;
		}

		public static long TicksToNanoseconds(this long ticks)
		{
			return checked(ticks * NanosecondsPerTick);
		}

		public static TimeSpan AddNanoseconds(this TimeSpan t, long nanoseconds)
		{
			return t + TimeSpan.FromTicks(NanosecondsToTicks(nanoseconds));
		}

		public static DateTime AddNanoseconds(this DateTime dt, long nanoseconds)
		{
			return dt.AddTicks(NanosecondsToTicks(nanoseconds));
		}

		public static DateTimeOffset AddNanoseconds(this DateTimeOffset dto, long nanoseconds)
		{
			return dto.AddTicks(NanosecondsToTicks(nanoseconds));
		}

		public static long MicrosecondsToTicks(this long mcs)
		{
			return mcs * TicksPerMicrosecond;
		}

		public static long TicksToMicroseconds(this long ticks)
		{
			return ticks / TicksPerMicrosecond;
		}

		public static TimeSpan AddMicroseconds(this TimeSpan t, long microseconds)
		{
			return t + TimeSpan.FromTicks(MicrosecondsToTicks(microseconds));
		}

		public static DateTime AddMicroseconds(this DateTime dt, long microseconds)
		{
			return dt.AddTicks(MicrosecondsToTicks(microseconds));
		}

		public static DateTimeOffset AddMicroseconds(this DateTimeOffset dto, long microseconds)
		{
			return dto.AddTicks(MicrosecondsToTicks(microseconds));
		}

		public static DateTime Truncate(this DateTime time, long precision)
		{
			return time.AddTicks(-(time.Ticks % precision));
		}

		public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
		{
			return dateTime.Truncate(timeSpan.Ticks);
		}

		public static TimeSpan Truncate(this TimeSpan time, long precision)
		{
			return TimeSpan.FromTicks(time.Ticks - (time.Ticks % precision));
		}

		public static TimeSpan Truncate(this TimeSpan dateTime, TimeSpan timeSpan)
		{
			return dateTime.Truncate(timeSpan.Ticks);
		}

		public static IEnumerable<DateTime> Range(this DateTime from, DateTime to, TimeSpan interval)
		{
			while (from <= to)
			{
				yield return from;

				from += interval;
			}
		}

		public static int DaysInMonth(this DateTime date)
		{
			return DateTime.DaysInMonth(date.Year, date.Month);
		}

		public static DateTime ChangeKind(this DateTime date, DateTimeKind kind = DateTimeKind.Unspecified)
		{
			return DateTime.SpecifyKind(date, kind);
		}

		// http://stackoverflow.com/questions/38039/how-can-i-get-the-datetime-for-the-start-of-the-week
		public static DateTime StartOfWeek(this DateTime date, DayOfWeek startOfWeek)
		{
			var diff = date.DayOfWeek - startOfWeek;

			if (diff < 0)
				diff += 7;

			return date.AddDays(-1 * diff).Date;
		}

		public static DateTime EndOfDay(this DateTime dt)
		{
			return dt.Date + LessOneDay;
		}

		public static DateTimeOffset EndOfDay(this DateTimeOffset dto)
		{
			return new DateTimeOffset(dto.Date.EndOfDay(), dto.Offset);
		}

		public static readonly DateTime GregorianStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		public static readonly TimeZoneInfo Est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
		public static readonly TimeZoneInfo Moscow = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
		//public static readonly TimeZoneInfo Gmt = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

		public static DateTime To(this DateTime time, TimeZoneInfo source = null, TimeZoneInfo destination = null)
		{
			if (source == null)
				source = time.Kind == DateTimeKind.Utc ? TimeZoneInfo.Utc : TimeZoneInfo.Local;

			return TimeZoneInfo.ConvertTime(time, source, destination ?? TimeZoneInfo.Utc);
		}

		public static DateTime? TryToDateTime(this string value, string format, CultureInfo ci = null)
		{
			if (value.IsEmpty())
				return null;

			return value.ToDateTime(format, ci);
		}

		public static DateTime ToDateTime(this string value, string format, CultureInfo ci = null)
		{
			try
			{
				return DateTime.ParseExact(value, format, ci ?? CultureInfo.InvariantCulture);
			}
			catch (Exception ex)
			{
				throw new InvalidCastException("Cannot convert {0} with format {1} to {2}.".Put(value, format, typeof(DateTime).Name), ex);
			}
		}

		public static string FromDateTime(this DateTime dt, string format, CultureInfo ci = null)
		{
			return dt.ToString(format, ci ?? CultureInfo.InvariantCulture);
		}

		public static TimeSpan? TryToTimeSpan(this string value, string format, CultureInfo ci = null)
		{
			if (value.IsEmpty())
				return null;

			return value.ToTimeSpan(format, ci);
		}

		public static TimeSpan ToTimeSpan(this string value, string format, CultureInfo ci = null)
		{
			try
			{
				return TimeSpan.ParseExact(value, format, ci ?? CultureInfo.InvariantCulture);
			}
			catch (Exception ex)
			{
				throw new InvalidCastException("Cannot convert {0} with format {1} to {2}.".Put(value, format, typeof(TimeSpan).Name), ex);
			}
		}

		public static string FromTimeSpan(this TimeSpan ts, string format, CultureInfo ci = null)
		{
			return ts.ToString(format, ci ?? CultureInfo.InvariantCulture);
		}

		public static DateTimeOffset? TryToDateTimeOffset(this string value, string format, CultureInfo ci = null)
		{
			if (value.IsEmpty())
				return null;

			return value.ToDateTimeOffset(format, ci);
		}

		public static DateTimeOffset ToDateTimeOffset(this string value, string format, CultureInfo ci = null)
		{
			try
			{
				return DateTimeOffset.ParseExact(value, format, ci ?? CultureInfo.InvariantCulture);
			}
			catch (Exception ex)
			{
				throw new InvalidCastException("Cannot convert {0} with format {1} to {2}.".Put(value, format, typeof(DateTimeOffset).Name), ex);
			}
		}

		public static DateTimeOffset ToDateTimeOffset(this DateTime date, TimeZoneInfo zone)
		{
			if (zone == null)
				throw new ArgumentNullException(nameof(zone));

			return date.ToDateTimeOffset(zone.GetUtcOffset(date));
		}

		public static DateTimeOffset ToDateTimeOffset(this DateTime date, TimeSpan offset)
		{
			return new DateTimeOffset(date.ChangeKind() + offset, offset);
		}

		public static string FromDateTimeOffset(this DateTimeOffset dto, string format, CultureInfo ci = null)
		{
			return dto.ToString(format, ci ?? CultureInfo.InvariantCulture);
		}

		public static DateTimeOffset ApplyTimeZone(this DateTime dt, TimeZoneInfo zone)
		{
			if (zone == null)
				throw new ArgumentNullException(nameof(zone));

			return dt.ApplyTimeZone(zone.GetUtcOffset(dt.ChangeKind()));
		}

		public static DateTimeOffset ApplyTimeZone(this DateTime dt, TimeSpan offset)
		{
			try
			{
				return new DateTimeOffset(dt.ChangeKind(), offset);
			}
			catch (Exception ex)
			{
				throw new ArgumentException($"Cannot convert {dt} to {nameof(DateTimeOffset)}.", nameof(dt), ex);
			}
		}

		public static DateTime ToLocalTime(this DateTimeOffset dto, TimeZoneInfo zone)
		{
			return dto.Convert(zone).DateTime;
		}

		public static DateTimeOffset Convert(this DateTimeOffset dto, TimeZoneInfo zone)
		{
			return TimeZoneInfo.ConvertTime(dto, zone);
		}

		public static DateTimeOffset Truncate(this DateTimeOffset time, TimeSpan timeSpan)
		{
			return time.Truncate(timeSpan.Ticks);
		}

		public static DateTimeOffset Truncate(this DateTimeOffset time, long precision)
		{
			var offset = time.Offset;
			return new DateTimeOffset(time.UtcDateTime.Truncate(precision).ChangeKind() + offset, offset);
		}

		// https://stackoverflow.com/questions/11154673/get-the-correct-week-number-of-a-given-date
		public static int GetIso8601WeekOfYear(this DateTime time, CultureInfo ci = null)
		{
			// Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
			// be the same week# as whatever Thursday, Friday or Saturday are,
			// and we always get those right
			var calendar = (ci ?? CultureInfo.InvariantCulture).Calendar;

			var day = calendar.GetDayOfWeek(time);
			if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
			{
				time = time.AddDays(3);
			}

			// Return the week of our adjusted day
			return calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
		}

		public static double ToUnix(this DateTimeOffset time)
		{
			return time.UtcDateTime.ToUnix();
		}

		public static double ToUnix(this DateTime time)
		{
			if (time.Kind != DateTimeKind.Utc)
				throw new ArgumentException(nameof(time));

			var diff = time - GregorianStart;

			if (diff < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(time));

			return diff.TotalSeconds;
		}
		
		public static DateTime FromUnix(this long time, bool isSeconds = true)
		{
			return isSeconds ? ((double)time).FromUnix() : GregorianStart.AddMilliseconds(time);
		}

		public static DateTime FromUnix(this double time)
		{
			return GregorianStart.AddSeconds(time);
		}

		public static double UnixNowS => DateTime.UtcNow.ToUnix();
		public static double UnixNowMls => DateTime.UtcNow.ToUnix();

		//private const string _timeFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'"; // "yyyy-MM-dd'T'HH:mm:ss.fffffffff'Z'"

		//public static string ToRfc3339(this DateTimeOffset time)
		//{
		//	var str = time.ToString(_timeFormat);
		//	return str.Insert(str.IndexOf('.') + 8, "00");
		//}

		//public static DateTimeOffset FromRfc3339(this string time)
		//{
		//	if (time.IsEmpty())
		//		throw new ArgumentNullException(nameof(time));

		//	// cannot parse nanoseconds
		//	var dt = time.Remove(time.IndexOf('.') + 8, 2).ToDateTime(_timeFormat);
		//	//var dt = time.ToDateTime(_timeFormat);
		//	return dt.ChangeKind(DateTimeKind.Utc);
		//}
	}
}