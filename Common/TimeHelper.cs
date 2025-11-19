namespace Ecng.Common;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>
/// Provides various helper methods and properties for working with dates, times, and time offsets.
/// </summary>
public static class TimeHelper
{
	private static readonly Stopwatch _timer;
	private static readonly DateTime _start;
	private static DateTime _startWithOffset;

	static TimeHelper()
	{
		_start = DateTime.UtcNow;
		_timer = Stopwatch.StartNew();

		NowOffset = TimeSpan.Zero;
	}

	/// <summary>
	/// Gets the current time including the configured offset.
	/// </summary>
	public static DateTime Now => _startWithOffset + _timer.Elapsed;

	/// <summary>
	/// Gets the current time as a <see cref="DateTimeOffset"/> including the local offset.
	/// </summary>
	public static DateTimeOffset NowWithOffset => Now.ApplyLocal();

	private static TimeSpan _nowOffset;

	/// <summary>
	/// Gets or sets the offset applied to the current time when retrieving <see cref="Now"/>.
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
	/// Gets or sets the time zone offset used for calculations.
	/// </summary>
	public static TimeSpan TimeZoneOffset { get; set; } = TimeZoneInfo.Local.BaseUtcOffset;

	/// <summary>
	/// Synchronizes the current offset by comparing local time with an NTP server.
	/// </summary>
	/// <param name="timeout">The synchronization timeout in milliseconds.</param>
	public static void SyncMarketTime(int timeout = 5000)
	{
		var dtNow = _start + _timer.Elapsed;
		NowOffset = new NtpClient().GetLocalTime(TimeZoneInfo.Local, timeout).Subtract(dtNow);
	}

	/// <summary>
	/// Returns total weeks in the specified <see cref="TimeSpan"/>.
	/// </summary>
	public static double TotalWeeks(this TimeSpan value)
	{
		return (double)value.Ticks / TicksPerWeek;
	}

	/// <summary>
	/// Returns total months in the specified <see cref="TimeSpan"/>.
	/// </summary>
	public static double TotalMonths(this TimeSpan value)
	{
		return (double)value.Ticks / TicksPerMonth;
	}

	/// <summary>
	/// Returns total years in the specified <see cref="TimeSpan"/>.
	/// </summary>
	public static double TotalYears(this TimeSpan value)
	{
		return (double)value.Ticks / TicksPerYear;
	}

	/// <summary>
	/// Returns total centuries in the specified <see cref="TimeSpan"/>.
	/// </summary>
	public static double TotalCenturies(this TimeSpan value)
	{
		return (double)value.Ticks / TicksPerCentury;
	}

	/// <summary>
	/// Returns total millenniums in the specified <see cref="TimeSpan"/>.
	/// </summary>
	public static double TotalMilleniums(this TimeSpan value)
	{
		return (double)value.Ticks / TicksPerMillenium;
	}

	/// <summary>
	/// Represents the number of ticks in 1 nanosecond.
	/// </summary>
	public const double TicksPerNanosecond = 1.0 / NanosecondsPerTick;

	/// <summary>
	/// Represents the number of nanoseconds in 1 tick.
	/// </summary>
	public const long NanosecondsPerTick = 100;

	/// <summary>
	/// Represents the number of ticks in 1 microsecond.
	/// </summary>
	public const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;

	/// <summary>
	/// Represents the number of ticks in 1 week.
	/// </summary>
	public const long TicksPerWeek = TimeSpan.TicksPerDay * 7;

	/// <summary>
	/// Represents the number of ticks in 1 month.
	/// </summary>
	public const long TicksPerMonth = TimeSpan.TicksPerDay * 30;

	/// <summary>
	/// Represents the number of ticks in 1 year.
	/// </summary>
	public const long TicksPerYear = TimeSpan.TicksPerDay * 365;

	/// <summary>
	/// Represents the number of ticks in 1 century.
	/// </summary>
	public const long TicksPerCentury = TicksPerYear * 100;

	/// <summary>
	/// Represents the number of ticks in 1 millenium.
	/// </summary>
	public const long TicksPerMillenium = TicksPerCentury * 10;

	/// <summary>
	/// A <see cref="TimeSpan"/> of one minute.
	/// </summary>
	public static readonly TimeSpan Minute1 = TimeSpan.FromMinutes(1);

	/// <summary>
	/// A <see cref="TimeSpan"/> of five minutes.
	/// </summary>
	public static readonly TimeSpan Minute5 = TimeSpan.FromMinutes(5);

	/// <summary>
	/// A <see cref="TimeSpan"/> of ten minutes.
	/// </summary>
	public static readonly TimeSpan Minute10 = TimeSpan.FromMinutes(10);

	/// <summary>
	/// A <see cref="TimeSpan"/> of fifteen minutes.
	/// </summary>
	public static readonly TimeSpan Minute15 = TimeSpan.FromMinutes(15);

	/// <summary>
	/// A <see cref="TimeSpan"/> of one hour.
	/// </summary>
	public static readonly TimeSpan Hour = TimeSpan.FromHours(1);

	/// <summary>
	/// A <see cref="TimeSpan"/> of one day.
	/// </summary>
	public static readonly TimeSpan Day = TimeSpan.FromDays(1);

	/// <summary>
	/// A <see cref="TimeSpan"/> of one week (7 days).
	/// </summary>
	public static readonly TimeSpan Week = TimeSpan.FromTicks(TicksPerWeek);

	/// <summary>
	/// A <see cref="TimeSpan"/> of one month (30 days).
	/// </summary>
	public static readonly TimeSpan Month = TimeSpan.FromTicks(TicksPerMonth);

	/// <summary>
	/// A <see cref="TimeSpan"/> of one year (365 days).
	/// </summary>
	public static readonly TimeSpan Year = TimeSpan.FromTicks(TicksPerYear);

	/// <summary>
	/// A <see cref="TimeSpan"/> that is one tick less than a day.
	/// </summary>
	public static readonly TimeSpan LessOneDay = TimeSpan.FromTicks(TimeSpan.TicksPerDay - 1);

	/// <summary>
	/// Gets the microseconds component from a <see cref="TimeSpan"/>.
	/// </summary>
	public static int GetMicroseconds(this TimeSpan ts)
	{
		return (int)(TicksToMicroseconds(ts.Ticks) % 1000);
	}

	/// <summary>
	/// Gets the microseconds component from a <see cref="DateTime"/>.
	/// </summary>
	public static int GetMicroseconds(this DateTime dt)
	{
		return (int)(TicksToMicroseconds(dt.Ticks) % 1000);
	}

	/// <summary>
	/// Gets the nanoseconds component from a <see cref="TimeSpan"/>.
	/// </summary>
	public static int GetNanoseconds(this TimeSpan ts)
	{
		return GetNanoseconds(ts.Ticks);
	}

	/// <summary>
	/// Gets the nanoseconds component from a <see cref="DateTime"/>.
	/// </summary>
	public static int GetNanoseconds(this DateTime dt)
	{
		return GetNanoseconds(dt.Ticks);
	}

	/// <summary>
	/// Gets the nanoseconds component from the specified number of ticks.
	/// </summary>
	public static int GetNanoseconds(this long ticks)
	{
		return (int)((ticks % 10) * NanosecondsPerTick);
	}

	/// <summary>
	/// Converts a <see cref="TimeSpan"/> to the total number of nanoseconds.
	/// </summary>
	public static long ToNanoseconds(this TimeSpan ts)
	{
		return TicksToNanoseconds(ts.Ticks);
	}

	/// <summary>
	/// Converts a <see cref="DateTime"/> to the total number of nanoseconds.
	/// </summary>
	[Obsolete("Use ToNanoseconds extension method for TimeSpan instead.")]
	public static long ToNanoseconds(this DateTime dt)
	{
		return TicksToNanoseconds(dt.Ticks);
	}

	/// <summary>
	/// Converts nanoseconds to ticks.
	/// </summary>
	public static long NanosecondsToTicks(this long nanoseconds)
	{
		return nanoseconds / NanosecondsPerTick;
	}

	/// <summary>
	/// Converts ticks to nanoseconds.
	/// </summary>
	public static long TicksToNanoseconds(this long ticks)
	{
		return checked(ticks * NanosecondsPerTick);
	}

	/// <summary>
	/// Adds the specified number of nanoseconds to a <see cref="TimeSpan"/>.
	/// </summary>
	public static TimeSpan AddNanoseconds(this TimeSpan t, long nanoseconds)
	{
		return t + TimeSpan.FromTicks(NanosecondsToTicks(nanoseconds));
	}

	/// <summary>
	/// Adds the specified number of nanoseconds to a <see cref="DateTime"/>.
	/// </summary>
	public static DateTime AddNanoseconds(this DateTime dt, long nanoseconds)
	{
		return dt.AddTicks(NanosecondsToTicks(nanoseconds));
	}

	/// <summary>
	/// Adds the specified number of nanoseconds to a <see cref="DateTimeOffset"/>.
	/// </summary>
	public static DateTimeOffset AddNanoseconds(this DateTimeOffset dto, long nanoseconds)
	{
		return dto.AddTicks(NanosecondsToTicks(nanoseconds));
	}

	/// <summary>
	/// Converts microseconds to ticks.
	/// </summary>
	public static long MicrosecondsToTicks(this long mcs)
	{
		return mcs * TicksPerMicrosecond;
	}

	/// <summary>
	/// Converts ticks to microseconds.
	/// </summary>
	public static long TicksToMicroseconds(this long ticks)
	{
		return ticks / TicksPerMicrosecond;
	}

	/// <summary>
	/// Adds the specified number of microseconds to a <see cref="TimeSpan"/>.
	/// </summary>
	public static TimeSpan AddMicroseconds(this TimeSpan t, long microseconds)
	{
		return t + TimeSpan.FromTicks(MicrosecondsToTicks(microseconds));
	}

	/// <summary>
	/// Adds the specified number of microseconds to a <see cref="DateTime"/>.
	/// </summary>
	public static DateTime AddMicroseconds(this DateTime dt, long microseconds)
	{
		return dt.AddTicks(MicrosecondsToTicks(microseconds));
	}

	/// <summary>
	/// Adds the specified number of microseconds to a <see cref="DateTimeOffset"/>.
	/// </summary>
	public static DateTimeOffset AddMicroseconds(this DateTimeOffset dto, long microseconds)
	{
		return dto.AddTicks(MicrosecondsToTicks(microseconds));
	}

	/// <summary>
	/// Truncates a <see cref="DateTime"/> to the specified ticks precision.
	/// </summary>
	public static DateTime Truncate(this DateTime time, long precision)
	{
		return time.AddTicks(-(time.Ticks % precision));
	}

	/// <summary>
	/// Truncates a <see cref="DateTime"/> to the specified time span precision.
	/// </summary>
	public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
	{
		return dateTime.Truncate(timeSpan.Ticks);
	}

	/// <summary>
	/// Truncates a <see cref="TimeSpan"/> to the specified ticks precision.
	/// </summary>
	public static TimeSpan Truncate(this TimeSpan time, long precision)
	{
		return TimeSpan.FromTicks(time.Ticks - (time.Ticks % precision));
	}

	/// <summary>
	/// Truncates a <see cref="TimeSpan"/> to the specified time span precision.
	/// </summary>
	public static TimeSpan Truncate(this TimeSpan dateTime, TimeSpan timeSpan)
	{
		return dateTime.Truncate(timeSpan.Ticks);
	}

	/// <summary>
	/// Generates a sequence of <see cref="DateTime"/> values from a start to an end with a given interval.
	/// </summary>
	/// <param name="from">Start date.</param>
	/// <param name="to">End date.</param>
	/// <param name="interval">The interval between generated dates.</param>
	/// <returns>An enumerable of dates.</returns>
	public static IEnumerable<DateTime> Range(this DateTime from, DateTime to, TimeSpan interval)
	{
		if (interval <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(interval), interval, "Invalid value.");

		while (from <= to)
		{
			yield return from;

			from += interval;
		}
	}

	/// <summary>
	/// Gets the number of days in the month of the given <see cref="DateTime"/>.
	/// </summary>
	public static int DaysInMonth(this DateTime date)
	{
		return DateTime.DaysInMonth(date.Year, date.Month);
	}

	/// <summary>
	/// Changes the <see cref="DateTimeKind"/> of a <see cref="DateTime"/>.
	/// </summary>
	public static DateTime ChangeKind(this DateTime date, DateTimeKind kind = DateTimeKind.Unspecified)
	{
		return DateTime.SpecifyKind(date, kind);
	}

	/// <summary>
	/// Converts a <see cref="DateTime"/> to UTC kind.
	/// </summary>
	public static DateTime UtcKind(this DateTime date)
	{
		return date.ChangeKind(DateTimeKind.Utc);
	}

	// http://stackoverflow.com/questions/38039/how-can-i-get-the-datetime-for-the-start-of-the-week

	/// <summary>
	/// Gets the start of the week for the specified <see cref="DateTime"/>, based on a chosen <see cref="System.DayOfWeek"/>.
	/// </summary>
	public static DateTime StartOfWeek(this DateTime date, DayOfWeek startOfWeek)
	{
		var diff = date.DayOfWeek - startOfWeek;

		if (diff < 0)
			diff += 7;

		return date.AddDays(-1 * diff).Date;
	}

	/// <summary>
	/// Gets the end of the day for the specified <see cref="DateTime"/>, just before midnight.
	/// </summary>
	public static DateTime EndOfDay(this DateTime dt)
	{
		return dt.Date + LessOneDay;
	}

	/// <summary>
	/// Gets the end of the day for the specified <see cref="DateTimeOffset"/>, just before midnight.
	/// </summary>
	public static DateTimeOffset EndOfDay(this DateTimeOffset dto)
	{
		return new DateTimeOffset(dto.Date.EndOfDay(), dto.Offset);
	}

	/// <summary>
	/// Represents a reference start date (1/1/1970, UTC).
	/// </summary>
	public static readonly DateTime GregorianStart = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

	/// <summary>
	/// Represents the Eastern Standard Time zone.
	/// </summary>
	public static readonly TimeZoneInfo Est = "Eastern Standard Time".To<TimeZoneInfo>();

	/// <summary>
	/// Represents the Central Standard Time zone.
	/// </summary>
	public static readonly TimeZoneInfo Cst = "Central Standard Time".To<TimeZoneInfo>();

	/// <summary>
	/// Represents the Russian Standard Time zone (Moscow).
	/// </summary>
	public static readonly TimeZoneInfo Moscow = "Russian Standard Time".To<TimeZoneInfo>();

	/// <summary>
	/// Represents the GMT Standard Time zone.
	/// </summary>
	public static readonly TimeZoneInfo Gmt = "GMT Standard Time".To<TimeZoneInfo>();

	/// <summary>
	/// Represents the FLE (Helsinki, Kyiv, Riga, Sofia, Tallinn, Vilnius) Standard Time zone.
	/// </summary>
	public static readonly TimeZoneInfo Fle = "FLE Standard Time".To<TimeZoneInfo>();

	/// <summary>
	/// Represents the China Standard Time zone.
	/// </summary>
	public static readonly TimeZoneInfo China = "China Standard Time".To<TimeZoneInfo>();

	/// <summary>
	/// Represents the Korea Standard Time zone.
	/// </summary>
	public static readonly TimeZoneInfo Korea = "Korea Standard Time".To<TimeZoneInfo>();

	/// <summary>
	/// Represents the Tokyo Standard Time zone.
	/// </summary>
	public static readonly TimeZoneInfo Tokyo = "Tokyo Standard Time".To<TimeZoneInfo>();

	/// <summary>
	/// Represents the West Central Africa Standard Time zone (Tunisia).
	/// </summary>
	public static readonly TimeZoneInfo Tunisia = "W. Central Africa Standard Time".To<TimeZoneInfo>();

	/// <summary>
	/// Converts a <see cref="DateTime"/> between time zones.
	/// </summary>
	public static DateTime To(this DateTime time, TimeZoneInfo source = null, TimeZoneInfo destination = null)
	{
		if (source is null)
			source = time.Kind == DateTimeKind.Utc ? TimeZoneInfo.Utc : TimeZoneInfo.Local;

		return TimeZoneInfo.ConvertTime(time, source, destination ?? TimeZoneInfo.Utc);
	}

	/// <summary>
	/// Attempts to convert a string to a <see cref="DateTime"/> using the provided format.
	/// </summary>
	public static DateTime? TryToDateTime(this string value, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string format, CultureInfo ci = null)
	{
		if (value.IsEmpty())
			return null;

		return value.ToDateTime(format, ci);
	}

	/// <summary>
	/// Converts a string to a <see cref="DateTime"/> using the provided format.
	/// </summary>
	public static DateTime ToDateTime(this string value, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string format, CultureInfo ci = null)
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

	/// <summary>
	/// Formats a <see cref="DateTime"/> to a string using the provided format.
	/// </summary>
	public static string FromDateTime(this DateTime dt, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string format, CultureInfo ci = null)
	{
		return dt.ToString(format, ci ?? CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Attempts to convert a string to a <see cref="TimeSpan"/> using the provided format.
	/// </summary>
	public static TimeSpan? TryToTimeSpan(this string value, [StringSyntax(StringSyntaxAttribute.TimeSpanFormat)] string format, CultureInfo ci = null)
	{
		if (value.IsEmpty())
			return null;

		return value.ToTimeSpan(format, ci);
	}

	/// <summary>
	/// Converts a string to a <see cref="TimeSpan"/> using the provided format.
	/// </summary>
	public static TimeSpan ToTimeSpan(this string value, [StringSyntax(StringSyntaxAttribute.TimeSpanFormat)] string format, CultureInfo ci = null)
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

	/// <summary>
	/// Formats a <see cref="TimeSpan"/> to a string using the provided format.
	/// </summary>
	public static string FromTimeSpan(this TimeSpan ts, [StringSyntax(StringSyntaxAttribute.TimeSpanFormat)] string format, CultureInfo ci = null)
	{
		return ts.ToString(format, ci ?? CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Attempts to convert a string to a <see cref="DateTimeOffset"/> using the provided format.
	/// </summary>
	public static DateTimeOffset? TryToDateTimeOffset(this string value, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string format, CultureInfo ci = null)
	{
		if (value.IsEmpty())
			return null;

		return value.ToDateTimeOffset(format, ci);
	}

	/// <summary>
	/// Converts a string to a <see cref="DateTimeOffset"/> using the provided format.
	/// </summary>
	public static DateTimeOffset ToDateTimeOffset(this string value, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string format, CultureInfo ci = null)
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

	/// <summary>
	/// Converts a <see cref="DateTime"/> and a time zone to a <see cref="DateTimeOffset"/>.
	/// </summary>
	public static DateTimeOffset ToDateTimeOffset(this DateTime date, TimeZoneInfo zone)
	{
		if (zone is null)
			throw new ArgumentNullException(nameof(zone));

		return date.ToDateTimeOffset(zone.GetUtcOffset(date));
	}

	/// <summary>
	/// Converts a <see cref="DateTime"/> and a specific offset to a <see cref="DateTimeOffset"/>.
	/// </summary>
	public static DateTimeOffset ToDateTimeOffset(this DateTime date, TimeSpan offset)
	{
		return new DateTimeOffset(date.ChangeKind() + offset, offset);
	}

	/// <summary>
	/// Formats a <see cref="DateTimeOffset"/> to a string using the provided format.
	/// </summary>
	public static string FromDateTimeOffset(this DateTimeOffset dto, [StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string format, CultureInfo ci = null)
	{
		return dto.ToString(format, ci ?? CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Creates a <see cref="DateTimeOffset"/> from a <see cref="DateTime"/> using the local time zone.
	/// </summary>
	public static DateTimeOffset ApplyLocal(this DateTime dt)
	{
		return dt.ApplyTimeZone(TimeZoneInfo.Local);
	}

	/// <summary>
	/// Creates a <see cref="DateTimeOffset"/> from a <see cref="DateTime"/> using UTC.
	/// </summary>
	public static DateTimeOffset ApplyUtc(this DateTime dt)
	{
		return dt.ApplyTimeZone(TimeZoneInfo.Utc);
	}

	/// <summary>
	/// Creates a <see cref="DateTimeOffset"/> from a <see cref="DateTime"/> in China Standard Time.
	/// </summary>
	public static DateTimeOffset ApplyChina(this DateTime dt)
	{
		return dt.ApplyTimeZone(China);
	}

	/// <summary>
	/// Creates a <see cref="DateTimeOffset"/> from a <see cref="DateTime"/> in Eastern Standard Time.
	/// </summary>
	public static DateTimeOffset ApplyEst(this DateTime dt)
	{
		return dt.ApplyTimeZone(Est);
	}

	/// <summary>
	/// Creates a <see cref="DateTimeOffset"/> from a <see cref="DateTime"/> in Russian Standard Time (Moscow).
	/// </summary>
	public static DateTimeOffset ApplyMoscow(this DateTime dt)
	{
		return dt.ApplyTimeZone(Moscow);
	}

	/// <summary>
	/// Creates a <see cref="DateTimeOffset"/> from a <see cref="DateTime"/> in the specified time zone.
	/// </summary>
	public static DateTimeOffset ApplyTimeZone(this DateTime dt, TimeZoneInfo zone)
	{
		if (zone is null)
			throw new ArgumentNullException(nameof(zone));

		return dt.ApplyTimeZone(zone.GetUtcOffset(dt.ChangeKind()));
	}

	/// <summary>
	/// Creates a <see cref="DateTimeOffset"/> from a <see cref="DateTime"/> with the specified offset.
	/// </summary>
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

	/// <summary>
	/// Converts a <see cref="DateTimeOffset"/> to a <see cref="DateTime"/> in the specified time zone.
	/// </summary>
	public static DateTime ToLocalTime(this DateTimeOffset dto, TimeZoneInfo zone)
	{
		return dto.Convert(zone).DateTime;
	}

	/// <summary>
	/// Converts a <see cref="DateTimeOffset"/> to China Standard Time zone.
	/// </summary>
	public static DateTimeOffset ConvertToChina(this DateTimeOffset dto)
	{
		return TimeZoneInfo.ConvertTime(dto, China);
	}

	/// <summary>
	/// Converts the specified <see cref="DateTimeOffset"/> to Eastern Standard Time.
	/// </summary>
	/// <param name="dto">The <see cref="DateTimeOffset"/> to convert.</param>
	/// <returns>A new <see cref="DateTimeOffset"/> in Eastern Standard Time.</returns>
	public static DateTimeOffset ConvertToEst(this DateTimeOffset dto)
	{
		return TimeZoneInfo.ConvertTime(dto, Est);
	}

	/// <summary>
	/// Converts the specified <see cref="DateTimeOffset"/> to Moscow Time.
	/// </summary>
	/// <param name="dto">The <see cref="DateTimeOffset"/> to convert.</param>
	/// <returns>A new <see cref="DateTimeOffset"/> in Moscow Time.</returns>
	public static DateTimeOffset ConvertToMoscow(this DateTimeOffset dto)
	{
		return TimeZoneInfo.ConvertTime(dto, Moscow);
	}

	/// <summary>
	/// Converts the specified <see cref="DateTimeOffset"/> to UTC.
	/// </summary>
	/// <param name="dto">The <see cref="DateTimeOffset"/> to convert.</param>
	/// <returns>A new <see cref="DateTimeOffset"/> in UTC.</returns>
	public static DateTimeOffset ConvertToUtc(this DateTimeOffset dto)
	{
		return TimeZoneInfo.ConvertTime(dto, TimeZoneInfo.Utc);
	}

	/// <summary>
	/// Converts the specified <see cref="DateTimeOffset"/> to the provided time zone.
	/// </summary>
	/// <param name="dto">The <see cref="DateTimeOffset"/> to convert.</param>
	/// <param name="zone">The target <see cref="TimeZoneInfo"/>.</param>
	/// <returns>A <see cref="DateTimeOffset"/> in the specified time zone.</returns>
	public static DateTimeOffset Convert(this DateTimeOffset dto, TimeZoneInfo zone)
	{
		return TimeZoneInfo.ConvertTime(dto, zone);
	}

	/// <summary>
	/// Truncates the specified <see cref="DateTimeOffset"/> to the given <see cref="TimeSpan"/>.
	/// </summary>
	/// <param name="time">The <see cref="DateTimeOffset"/> to truncate.</param>
	/// <param name="timeSpan">The <see cref="TimeSpan"/> precision.</param>
	/// <returns>The truncated <see cref="DateTimeOffset"/>.</returns>
	public static DateTimeOffset Truncate(this DateTimeOffset time, TimeSpan timeSpan)
	{
		return time.Truncate(timeSpan.Ticks);
	}

	/// <summary>
	/// Truncates the specified <see cref="DateTimeOffset"/> to the given precision in ticks.
	/// </summary>
	/// <param name="time">The <see cref="DateTimeOffset"/> to truncate.</param>
	/// <param name="precision">The precision in ticks.</param>
	/// <returns>The truncated <see cref="DateTimeOffset"/>.</returns>
	public static DateTimeOffset Truncate(this DateTimeOffset time, long precision)
	{
		var offset = time.Offset;
		return new DateTimeOffset(time.UtcDateTime.Truncate(precision).ChangeKind() + offset, offset);
	}

	/// <summary>
	/// Parses the specified string as an ISO8601 date/time.
	/// </summary>
	/// <param name="str">The string to parse.</param>
	/// <param name="provider">An optional <see cref="IFormatProvider"/>.</param>
	/// <returns>A <see cref="DateTime"/> parsed from the string, in UTC.</returns>
	public static DateTime FromIso8601(this string str, IFormatProvider provider = null)
	{
		return DateTime.Parse(str, provider, DateTimeStyles.RoundtripKind).UtcKind();
	}

	/// <summary>
	/// Formats the specified <see cref="DateTime"/> as an ISO8601 string.
	/// </summary>
	/// <param name="dt">The <see cref="DateTime"/> to format.</param>
	/// <param name="provider">An optional <see cref="IFormatProvider"/>.</param>
	/// <returns>An ISO8601-formatted string.</returns>
	public static string ToIso8601(this DateTime dt, IFormatProvider provider = null)
	{
		return dt.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", provider);
	}

	// https://stackoverflow.com/questions/11154673/get-the-correct-week-number-of-a-given-date

	/// <summary>
	/// Calculates the ISO8601 week number of the specified date.
	/// </summary>
	/// <param name="time">The <see cref="DateTime"/> to evaluate.</param>
	/// <param name="ci">An optional <see cref="CultureInfo"/>.</param>
	/// <returns>The ISO8601 week of the year.</returns>
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

	/// <summary>
	/// Converts the specified <see cref="DateTimeOffset"/> to Unix time in seconds or milliseconds.
	/// </summary>
	/// <param name="time">The <see cref="DateTimeOffset"/> to convert.</param>
	/// <param name="isSeconds">If set to true, returns seconds; otherwise milliseconds.</param>
	/// <returns>A double representing the Unix time.</returns>
	public static double ToUnix(this DateTimeOffset time, bool isSeconds = true)
	{
		return time.UtcDateTime.ToUnix(isSeconds);
	}

	/// <summary>
	/// Converts the specified <see cref="DateTime"/> to Unix time in seconds or milliseconds.
	/// </summary>
	/// <param name="time">The <see cref="DateTime"/> to convert.</param>
	/// <param name="isSeconds">If set to true, returns seconds; otherwise milliseconds.</param>
	/// <returns>A double representing the Unix time.</returns>
	public static double ToUnix(this DateTime time, bool isSeconds = true)
	{
		var diff = time.GetUnixDiff();

		return isSeconds ? diff.TotalSeconds : diff.TotalMilliseconds;
	}

	/// <summary>
	/// Gets the difference between the specified <see cref="DateTime"/> and the <see cref="GregorianStart"/>.
	/// </summary>
	/// <param name="time">The <see cref="DateTime"/> to evaluate.</param>
	/// <returns>A <see cref="TimeSpan"/> representing the difference.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the time is earlier than <see cref="GregorianStart"/>.</exception>
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

	/// <summary>
	/// Creates a <see cref="DateTime"/> from a Unix time in seconds or milliseconds.
	/// </summary>
	/// <param name="time">The Unix time to convert.</param>
	/// <param name="isSeconds">If set to true, interprets time as seconds; otherwise milliseconds.</param>
	/// <returns>A <see cref="DateTime"/> in UTC.</returns>
	public static DateTime FromUnix(this long time, bool isSeconds = true)
	{
		return isSeconds ? GregorianStart.AddSeconds(time) : GregorianStart.AddMilliseconds(time);
	}

	/// <summary>
	/// Creates a <see cref="DateTime"/> from a Unix time in seconds or milliseconds.
	/// </summary>
	/// <param name="time">The Unix time to convert.</param>
	/// <param name="isSeconds">If set to true, interprets time as seconds; otherwise milliseconds.</param>
	/// <returns>A <see cref="DateTime"/> in UTC.</returns>
	public static DateTime FromUnix(this double time, bool isSeconds = true)
	{
		return isSeconds ? GregorianStart.AddSeconds(time) : GregorianStart.AddMilliseconds(time);
	}

	/// <summary>
	/// Tries to create a <see cref="DateTime"/> from a Unix time in seconds or milliseconds. Returns null if time is 0.
	/// </summary>
	/// <param name="time">The Unix time to convert.</param>
	/// <param name="isSeconds">If set to true, interprets time as seconds; otherwise milliseconds.</param>
	/// <returns>A <see cref="DateTime"/> in UTC, or null if 0.</returns>
	public static DateTime? TryFromUnix(this long time, bool isSeconds = true)
	{
		if (time == 0)
			return null;

		return time.FromUnix(isSeconds);
	}

	/// <summary>
	/// Tries to create a <see cref="DateTime"/> from a Unix time in seconds or milliseconds. Returns null if time is near zero.
	/// </summary>
	/// <param name="time">The Unix time to convert.</param>
	/// <param name="isSeconds">If set to true, interprets time as seconds; otherwise milliseconds.</param>
	/// <returns>A <see cref="DateTime"/> in UTC, or null if near zero.</returns>
	public static DateTime? TryFromUnix(this double time, bool isSeconds = true)
	{
		if (Math.Abs(time) < double.Epsilon)
			return null;

		return time.FromUnix(isSeconds);
	}

	/// <summary>
	/// Creates a <see cref="DateTime"/> from Microseconds since <see cref="GregorianStart"/>.
	/// </summary>
	/// <param name="mcs">Microseconds to convert.</param>
	/// <returns>A <see cref="DateTime"/> in UTC.</returns>
	public static DateTime FromUnixMcs(this long mcs)
	{
		return GregorianStart.AddMicroseconds(mcs);
	}

	/// <summary>
	/// Creates a <see cref="DateTime"/> from Microseconds since <see cref="GregorianStart"/>.
	/// </summary>
	/// <param name="mcs">Microseconds to convert.</param>
	/// <returns>A <see cref="DateTime"/> in UTC.</returns>
	public static DateTime FromUnixMcs(this double mcs)
	{
		return FromUnixMcs((long)mcs);
	}

	/// <summary>
	/// Converts the specified <see cref="DateTime"/> to Unix Microseconds since <see cref="GregorianStart"/>.
	/// </summary>
	/// <param name="time">The <see cref="DateTime"/> to convert.</param>
	/// <returns>A long representing the Unix time in microseconds.</returns>
	public static long ToUnixMcs(this DateTime time)
	{
		return time.GetUnixDiff().Ticks.TicksToMicroseconds();
	}

	/// <summary>
	/// Gets the current Unix time in seconds.
	/// </summary>
	public static double UnixNowS => DateTime.UtcNow.ToUnix();

	/// <summary>
	/// Gets the current Unix time in milliseconds.
	/// </summary>
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

	/// <summary>
	/// Checks if the given <see cref="Type"/> is a date/time type.
	/// </summary>
	/// <param name="type">The <see cref="Type"/> to check.</param>
	/// <returns>True if the type is a <see cref="DateTime"/> or <see cref="DateTimeOffset"/>; otherwise false.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is null.</exception>
	public static bool IsDateTime(this Type type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		return type == typeof(DateTimeOffset) || type == typeof(DateTime);
	}

	/// <summary>
	/// Checks if the given <see cref="Type"/> is a date or time type.
	/// </summary>
	/// <param name="type">The <see cref="Type"/> to check.</param>
	/// <returns>True if the type is a date/time or <see cref="TimeSpan"/>; otherwise false.</returns>
	public static bool IsDateOrTime(this Type type)
	{
		return type.IsDateTime() || type == typeof(TimeSpan);
	}

	/// <summary>
	/// Checks if the given <see cref="DateTimeOffset"/> falls on a weekday (Monday through Friday).
	/// </summary>
	/// <param name="date">The <see cref="DateTimeOffset"/> to check.</param>
	/// <returns>True if it is a weekday; otherwise false.</returns>
	public static bool IsWeekday(this DateTimeOffset date)
		=> date.DayOfWeek.IsWeekday();

	/// <summary>
	/// Checks if the given <see cref="DateTimeOffset"/> falls on a weekend (Saturday or Sunday).
	/// </summary>
	/// <param name="date">The <see cref="DateTimeOffset"/> to check.</param>
	/// <returns>True if it is a weekend; otherwise false.</returns>
	public static bool IsWeekend(this DateTimeOffset date)
		=> date.DayOfWeek.IsWeekend();

	/// <summary>
	/// Checks if the given <see cref="DateTime"/> falls on a weekday (Monday through Friday).
	/// </summary>
	/// <param name="date">The <see cref="DateTime"/> to check.</param>
	/// <returns>True if it is a weekday; otherwise false.</returns>
	public static bool IsWeekday(this DateTime date)
		=> date.DayOfWeek.IsWeekday();

	/// <summary>
	/// Checks if the given <see cref="DateTime"/> falls on a weekend (Saturday or Sunday).
	/// </summary>
	/// <param name="date">The <see cref="DateTime"/> to check.</param>
	/// <returns>True if it is a weekend; otherwise false.</returns>
	public static bool IsWeekend(this DateTime date)
		=> date.DayOfWeek.IsWeekend();

	/// <summary>
	/// Checks if the given <see cref="DayOfWeek"/> represents a weekday (Monday through Friday).
	/// </summary>
	/// <param name="dow">The <see cref="DayOfWeek"/>.</param>
	/// <returns>True if it is a weekday; otherwise false.</returns>
	public static bool IsWeekday(this DayOfWeek dow)
		=> !dow.IsWeekend();

	/// <summary>
	/// Checks if the given <see cref="DayOfWeek"/> represents a weekend (Saturday or Sunday).
	/// </summary>
	/// <param name="dow">The <see cref="DayOfWeek"/>.</param>
	/// <returns>True if it is a weekend; otherwise false.</returns>
	public static bool IsWeekend(this DayOfWeek dow)
		=> dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday;

	/// <summary>
	/// Determines the lunar phase for the specified date.
	/// </summary>
	/// <param name="date">The <see cref="DateTime"/> for which to determine the phase.</param>
	/// <returns>A <see cref="LunarPhases"/> value representing the phase of the moon.</returns>
	public static LunarPhases GetLunarPhase(this DateTime date)
	{
		// Convert the date to Julian Date
		var julianDate = ToJulianDate(date);

		// Calculate days since the last known new moon (Jan 6, 2000)
		var daysSinceNew = julianDate - 2451549.5;

		// Calculate the number of lunar cycles since the reference date
		var newMoons = daysSinceNew / 29.53; // 29.53 is the length of a lunar cycle in days

		// Get the current position in the lunar cycle (0 to 1)
		var phase = newMoons - Math.Floor(newMoons);

		// Convert the phase (0 to 1) to one of 8 moon phases (0 to 7)
		var phaseIndex = (LunarPhases)Math.Floor(phase * 8);

		return phaseIndex;
	}

	/// <summary>
	/// Converts the specified <see cref="DateTime"/> to a Julian date.
	/// </summary>
	/// <param name="date">The <see cref="DateTime"/> to convert.</param>
	/// <returns>A double representing the Julian date.</returns>
	public static double ToJulianDate(this DateTime date)
	{
		return date.ToOADate() + 2415018.5;
	}
}