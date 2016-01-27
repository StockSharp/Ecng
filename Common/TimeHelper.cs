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
			get { return _nowOffset; }
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

		/// <summary>
		/// Represents the one tick.
		/// </summary>
		public const int Tick = 1;

		/// <summary>
		/// Represents the number of ticks in 1 nanosecond. This field is constant.
		/// </summary>
		public const long TicksPerNanosecond = 1000;

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
			return (int)((ts.Ticks / (TimeSpan.TicksPerMillisecond / 1000)) % 1000);
		}

		public static TimeSpan AddNanoseconds(this TimeSpan t, long nanoseconds)
		{
			return t + TimeSpan.FromTicks(nanoseconds * TicksPerNanosecond);
		}

		public static DateTime AddNanoseconds(this DateTime dt, long nanoseconds)
		{
			return dt.AddTicks(nanoseconds * TicksPerNanosecond);
		}

		public static DateTimeOffset AddNanoseconds(this DateTimeOffset dto, long nanoseconds)
		{
			return dto.AddTicks(nanoseconds * TicksPerNanosecond);
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

		public static DateTime? TryToDateTime(this string value, string format)
		{
			if (value.IsEmpty())
				return null;

			return value.ToDateTime(format);
		}

		public static DateTime ToDateTime(this string value, string format)
		{
			try
			{
				return DateTime.ParseExact(value, format, CultureInfo.InvariantCulture);
			}
			catch (Exception ex)
			{
				throw new InvalidCastException("Cannot convert {0} with format {1} to {2}.".Put(value, format, typeof(DateTime).Name), ex);
			}
		}

		public static string FromDateTime(this DateTime dt, string format)
		{
			return dt.ToString(format, CultureInfo.InvariantCulture);
		}

		public static TimeSpan? TryToTimeSpan(this string value, string format)
		{
			if (value.IsEmpty())
				return null;

			return value.ToTimeSpan(format);
		}

		public static TimeSpan ToTimeSpan(this string value, string format)
		{
			try
			{
				return TimeSpan.ParseExact(value, format, CultureInfo.InvariantCulture);
			}
			catch (Exception ex)
			{
				throw new InvalidCastException("Cannot convert {0} with format {1} to {2}.".Put(value, format, typeof(TimeSpan).Name), ex);
			}
		}

		public static string FromTimeSpan(this TimeSpan ts, string format)
		{
			return ts.ToString(format, CultureInfo.InvariantCulture);
		}

		public static DateTimeOffset? TryToDateTimeOffset(this string value, string format)
		{
			if (value.IsEmpty())
				return null;

			return value.ToDateTimeOffset(format);
		}

		public static DateTimeOffset ToDateTimeOffset(this string value, string format)
		{
			try
			{
				return DateTimeOffset.ParseExact(value, format, CultureInfo.InvariantCulture);
			}
			catch (Exception ex)
			{
				throw new InvalidCastException("Cannot convert {0} with format {1} to {2}.".Put(value, format, typeof(DateTimeOffset).Name), ex);
			}
		}

		public static DateTimeOffset ToDateTimeOffset(this DateTime date, TimeZoneInfo zone)
		{
			return date.ToDateTimeOffset(zone.GetUtcOffset(date));
		}

		public static DateTimeOffset ToDateTimeOffset(this DateTime date, TimeSpan offset)
		{
			return new DateTimeOffset(date.ChangeKind() + offset, offset);
		}

		public static string FromDateTimeOffset(this DateTimeOffset dto, string format)
		{
			return dto.ToString(format, CultureInfo.InvariantCulture);
		}

		public static DateTimeOffset ApplyTimeZone(this DateTime dt, TimeZoneInfo zone)
		{
			if (zone == null)
				throw new ArgumentNullException(nameof(zone));

			return dt.ApplyTimeZone(zone.GetUtcOffset(dt.ChangeKind()));
		}

		public static DateTimeOffset ApplyTimeZone(this DateTime dt, TimeSpan offset)
		{
			return new DateTimeOffset(dt.ChangeKind(), offset);
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
	}
}