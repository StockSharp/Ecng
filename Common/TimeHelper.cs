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
			_start = DateTime.Now;
			_timer = Stopwatch.StartNew();

			NowOffset = TimeSpan.Zero;
		}

		/// <summary>
		/// Текущее время.
		/// </summary>
		public static DateTime Now => _startWithOffset + _timer.Elapsed;

		public static DateTimeOffset NowWithOffset => Now.ApplyLocal();

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

		public static readonly TimeSpan Minute1 = TimeSpan.FromMinutes(1);
		public static readonly TimeSpan Minute5 = TimeSpan.FromMinutes(5);
		public static readonly TimeSpan Minute10 = TimeSpan.FromMinutes(10);
		public static readonly TimeSpan Minute15 = TimeSpan.FromMinutes(15);
		public static readonly TimeSpan Hour = TimeSpan.FromHours(1);
		public static readonly TimeSpan Day = TimeSpan.FromDays(1);
		public static readonly TimeSpan Week = TimeSpan.FromTicks(TicksPerWeek);
		public static readonly TimeSpan Month = TimeSpan.FromTicks(TicksPerMonth);
		public static readonly TimeSpan Year = TimeSpan.FromTicks(TicksPerYear);

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

		public static DateTime UtcKind(this DateTime date)
		{
			return date.ChangeKind(DateTimeKind.Utc);
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

		public static readonly DateTime GregorianStart = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		public static readonly TimeZoneInfo Est = "Eastern Standard Time".To<TimeZoneInfo>();
		public static readonly TimeZoneInfo Cst = "Central Standard Time".To<TimeZoneInfo>();
		public static readonly TimeZoneInfo Moscow = "Russian Standard Time".To<TimeZoneInfo>();
		public static readonly TimeZoneInfo Gmt = "GMT Standard Time".To<TimeZoneInfo>();
		public static readonly TimeZoneInfo Fle = "FLE Standard Time".To<TimeZoneInfo>();
		public static readonly TimeZoneInfo China = "China Standard Time".To<TimeZoneInfo>();
		public static readonly TimeZoneInfo Korea = "Korea Standard Time".To<TimeZoneInfo>();
		public static readonly TimeZoneInfo Tokyo = "Tokyo Standard Time".To<TimeZoneInfo>();
		public static readonly TimeZoneInfo Tunisia = "W. Central Africa Standard Time".To<TimeZoneInfo>();

		public static DateTime To(this DateTime time, TimeZoneInfo source = null, TimeZoneInfo destination = null)
		{
			if (source is null)
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
				throw new InvalidCastException($"Cannot convert {value} with format {format} to {typeof(DateTime).Name}.", ex);
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
				throw new InvalidCastException($"Cannot convert {value} with format {format} to {typeof(TimeSpan).Name}.", ex);
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
				throw new InvalidCastException($"Cannot convert {value} with format {format} to {typeof(DateTimeOffset).Name}.", ex);
			}
		}

		public static DateTimeOffset ToDateTimeOffset(this DateTime date, TimeZoneInfo zone)
		{
			if (zone is null)
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

		public static DateTimeOffset ApplyLocal(this DateTime dt)
		{
			return dt.ApplyTimeZone(TimeZoneInfo.Local);
		}

		public static DateTimeOffset ApplyUtc(this DateTime dt)
		{
			return dt.ApplyTimeZone(TimeZoneInfo.Utc);
		}

		public static DateTimeOffset ApplyChina(this DateTime dt)
		{
			return dt.ApplyTimeZone(China);
		}

		public static DateTimeOffset ApplyEst(this DateTime dt)
		{
			return dt.ApplyTimeZone(Est);
		}

		public static DateTimeOffset ApplyMoscow(this DateTime dt)
		{
			return dt.ApplyTimeZone(Moscow);
		}

		public static DateTimeOffset ApplyTimeZone(this DateTime dt, TimeZoneInfo zone)
		{
			if (zone is null)
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

		public static DateTimeOffset ConvertToChina(this DateTimeOffset dto)
		{
			return TimeZoneInfo.ConvertTime(dto, China);
		}

		public static DateTimeOffset ConvertToEst(this DateTimeOffset dto)
		{
			return TimeZoneInfo.ConvertTime(dto, Est);
		}

		public static DateTimeOffset ConvertToMoscow(this DateTimeOffset dto)
		{
			return TimeZoneInfo.ConvertTime(dto, Moscow);
		}

		public static DateTimeOffset ConvertToUtc(this DateTimeOffset dto)
		{
			return TimeZoneInfo.ConvertTime(dto, TimeZoneInfo.Utc);
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

		public static DateTime FromIso8601(this string str, IFormatProvider provider = null)
		{
			return DateTime.Parse(str, provider, DateTimeStyles.RoundtripKind).UtcKind();
		}

		public static string ToIso8601(this DateTime dt, IFormatProvider provider = null)
		{
			return dt.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", provider);
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

		public static double ToUnix(this DateTimeOffset time, bool isSeconds = true)
		{
			return time.UtcDateTime.ToUnix(isSeconds);
		}

		public static double ToUnix(this DateTime time, bool isSeconds = true)
		{
			var diff = time.GetUnixDiff();

			return isSeconds ? diff.TotalSeconds : diff.TotalMilliseconds;
		}

		public static TimeSpan GetUnixDiff(this DateTime time)
		{
			if (time.Kind != DateTimeKind.Utc)
			{
				time = time.ToUniversalTime();
				//throw new ArgumentException(nameof(time));
			}

			var diff = time - GregorianStart;

			if (diff < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(time));

			return diff;
		}

		public static DateTime FromUnix(this long time, bool isSeconds = true)
		{
			return isSeconds ? GregorianStart.AddSeconds(time) : GregorianStart.AddMilliseconds(time);
		}

		public static DateTime FromUnix(this double time, bool isSeconds = true)
		{
			return isSeconds ? GregorianStart.AddSeconds(time) : GregorianStart.AddMilliseconds(time);
		}

		public static DateTime? TryFromUnix(this long time, bool isSeconds = true)
		{
			if (time == 0)
				return null;

			return time.FromUnix(isSeconds);
		}

		public static DateTime? TryFromUnix(this double time, bool isSeconds = true)
		{
			if (Math.Abs(time) < double.Epsilon)
				return null;

			return time.FromUnix(isSeconds);
		}

		public static DateTime FromUnixMcs(this long mcs)
		{
			return GregorianStart.AddMicroseconds(mcs);
		}

		public static DateTime FromUnixMcs(this double mcs)
		{
			return FromUnixMcs((long)mcs);
		}

		public static long ToUnixMcs(this DateTime time)
		{
			return time.GetUnixDiff().Ticks.TicksToMicroseconds();
		}

		public static double UnixNowS => DateTime.UtcNow.ToUnix();
		public static double UnixNowMls => DateTime.UtcNow.ToUnix(false);

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

		public static bool IsDateTime(this Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			return type == typeof(DateTimeOffset) || type == typeof(DateTime);
		}

		public static bool IsDateOrTime(this Type type)
		{
			return type.IsDateTime() || type == typeof(TimeSpan);
		}

		public static bool IsWeekday(this DateTimeOffset date)
			=> date.DayOfWeek.IsWeekday();

		public static bool IsWeekend(this DateTimeOffset date)
			=> date.DayOfWeek.IsWeekend();

		public static bool IsWeekday(this DateTime date)
			=> date.DayOfWeek.IsWeekday();

		public static bool IsWeekend(this DateTime date)
			=> date.DayOfWeek.IsWeekend();

		public static bool IsWeekday(this DayOfWeek dow)
			=> !dow.IsWeekend();

		public static bool IsWeekend(this DayOfWeek dow)
			=> dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday;
	}
}