namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;

	using Ecng.Common.TimeZoneConverter;

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
		public static readonly TimeZoneInfo Cst = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
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

		public static double ToUnix(this DateTimeOffset time, bool isSeconds = true)
		{
			return time.UtcDateTime.ToUnix(isSeconds);
		}

		public static double ToUnix(this DateTime time, bool isSeconds = true)
		{
			if (time.Kind != DateTimeKind.Utc)
				throw new ArgumentException(nameof(time));

			var diff = time - GregorianStart;

			if (diff < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(time));

			return isSeconds ? diff.TotalSeconds : diff.TotalMilliseconds;
		}
		
		public static DateTime FromUnix(this long time, bool isSeconds = true)
		{
			return isSeconds ? GregorianStart.AddSeconds(time) : GregorianStart.AddMilliseconds(time);
		}

		public static DateTime FromUnix(this double time, bool isSeconds = true)
		{
			return isSeconds ? GregorianStart.AddSeconds(time) : GregorianStart.AddMilliseconds(time);
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

		// https://en.wikipedia.org/wiki/List_of_time_zone_abbreviations

		private static readonly Lazy<Dictionary<string, TimeZoneInfo>> _tzAbbrs = new Lazy<Dictionary<string, TimeZoneInfo>>(() => new Dictionary<string, TimeZoneInfo>(StringComparer.InvariantCultureIgnoreCase)
		{
			// Australian Central Daylight Savings Time
			{ "ACDT", TimeZoneInfo.FindSystemTimeZoneById("Lord Howe Standard Time") },
			// Australian Central Standard Time
			{ "ACST", TimeZoneInfo.FindSystemTimeZoneById("AUS Central Standard Time") },
			// Acre Time
			{ "ACT", TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time") },
			// Australian Central Western Standard Time (unofficial)
			{ "ACWST", TimeZoneInfo.FindSystemTimeZoneById("Aus Central W. Standard Time") },
			// Atlantic Daylight Time
			{ "ADT", TimeZoneInfo.FindSystemTimeZoneById("Tocantins Standard Time") },
			// Australian Eastern Daylight Savings Time
			{ "AEDT", TimeZoneInfo.FindSystemTimeZoneById("Central Pacific Standard Time") },
			// Australian Eastern Standard Time
			{ "AEST", TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time") },
			// Afghanistan Time
			{ "AFT", TimeZoneInfo.FindSystemTimeZoneById("Afghanistan Standard Time") },
			// Alaska Daylight Time
			{ "AKDT", TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time") },
			// Alaska Standard Time
			{ "AKST", TimeZoneInfo.FindSystemTimeZoneById("Alaskan Standard Time") },
			// Amazon Summer Time (Brazil)[1]
			{ "AMST", TimeZoneInfo.FindSystemTimeZoneById("SA Eastern Standard Time") },
			// Amazon Time (Brazil)[2]
			{ "AMT", TimeZoneInfo.FindSystemTimeZoneById("Central Brazilian Standard Time") },
			// Armenia Time
			//{ "AMT", TimeZoneInfo.FindSystemTimeZoneById("Caucasus Standard Time") },
			// Argentina Time
			{ "ART", TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time") },
			// Arabia Standard Time
			//{ "AST", TimeZoneInfo.FindSystemTimeZoneById("Arabic Standard Time") },
			// Atlantic Standard Time
			{ "AST", TimeZoneInfo.FindSystemTimeZoneById("Atlantic Standard Time") },
			// Australian Western Standard Time
			{ "AWST", TimeZoneInfo.FindSystemTimeZoneById("W. Australia Standard Time") },
			// Azores Summer Time
			{ "AZOST", TimeZoneInfo.FindSystemTimeZoneById("Greenwich Standard Time") },
			// Azores Standard Time
			{ "AZOT", TimeZoneInfo.FindSystemTimeZoneById("Azores Standard Time") },
			// Azerbaijan Time
			{ "AZT", TimeZoneInfo.FindSystemTimeZoneById("Azerbaijan Standard Time") },
			// Brunei Time
			{ "BDT", TimeZoneInfo.FindSystemTimeZoneById("North Asia East Standard Time") },
			// British Indian Ocean Time
			{ "BIOT", TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time") },
			// Baker Island Time
			{ "BIT", TimeZoneInfo.FindSystemTimeZoneById("Dateline Standard Time") },
			// Bolivia Time
			{ "BOT", TimeZoneInfo.FindSystemTimeZoneById("Paraguay Standard Time") },
			// Brasília Summer Time
			{ "BRST", TimeZoneInfo.FindSystemTimeZoneById("Mid-Atlantic Standard Time") },
			// Brasilia Time
			{ "BRT", TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time") },
			// Bangladesh Standard Time
			{ "BST", TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time") },
			// Bougainville Standard Time[3]
			//{ "BST", TimeZoneInfo.FindSystemTimeZoneById("Bougainville Standard Time") },
			// British Summer Time (British Standard Time from Feb 1968 to Oct 1971)
			//{ "BST", TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time") },
			// Bhutan Time
			{ "BTT", TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time") },
			// Central Africa Time
			{ "CAT", TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time") },
			// Cocos Islands Time
			{ "CCT", TimeZoneInfo.FindSystemTimeZoneById("Myanmar Standard Time") },
			// Central Daylight Time (North America)
			{ "CDT", TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time") },
			// Cuba Daylight Time[4]
			//{ "CDT", TimeZoneInfo.FindSystemTimeZoneById("Cuba Standard Time") },
			// Central European Summer Time (Cf. HAEC)
			{ "CEST", TimeZoneInfo.FindSystemTimeZoneById("E. Europe Standard Time") },
			// Central European Time
			{ "CET", TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time") },
			// Chatham Standard Time
			{ "CHAST", TimeZoneInfo.FindSystemTimeZoneById("Chatham Islands Standard Time") },
			// Choibalsan Standard Time
			{ "CHOT", TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time") },
			// Choibalsan Summer Time
			{ "CHOST", TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time") },
			// Chamorro Standard Time
			{ "CHST", TimeZoneInfo.FindSystemTimeZoneById("E. Australia Standard Time") },
			// Chuuk Time
			{ "CHUT", TimeZoneInfo.FindSystemTimeZoneById("E. Australia Standard Time") },
			// Clipperton Island Standard Time
			{ "CIST", TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time") },
			// Central Indonesia Time
			{ "CIT", TimeZoneInfo.FindSystemTimeZoneById("China Standard Time") },
			// Cook Island Time
			{ "CKT", TimeZoneInfo.FindSystemTimeZoneById("Hawaiian Standard Time") },
			// Chile Summer Time
			{ "CLST", TimeZoneInfo.FindSystemTimeZoneById("Saint Pierre Standard Time") },
			// Chile Standard Time
			{ "CLT", TimeZoneInfo.FindSystemTimeZoneById("Atlantic Standard Time") },
			// Colombia Summer Time
			{ "COST", TimeZoneInfo.FindSystemTimeZoneById("Atlantic Standard Time") },
			// Colombia Time
			{ "COT", TimeZoneInfo.FindSystemTimeZoneById("US Eastern Standard Time") },
			// Central Standard Time (North America)
			{ "CST", Cst },
			// China Standard Time
			//{ "CST", TimeZoneInfo.FindSystemTimeZoneById("China Standard Time") },
			// Cuba Standard Time
			//{ "CST", TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time") },
			// China time
			{ "CT", TimeZoneInfo.FindSystemTimeZoneById("China Standard Time") },
			// Cape Verde Time
			{ "CVT", TimeZoneInfo.FindSystemTimeZoneById("Cape Verde Standard Time") },
			// Central Western Standard Time (Australia) unofficial
			{ "CWST", TimeZoneInfo.FindSystemTimeZoneById("Aus Central W. Standard Time") },
			// Christmas Island Time
			{ "CXT", TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time") },
			// Davis Time
			{ "DAVT", TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time") },
			// Dumont d'Urville Time
			{ "DDUT", TimeZoneInfo.FindSystemTimeZoneById("E. Australia Standard Time") },
			// AIX-specific equivalent of Central European Time[5]
			{ "DFT", TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time") },
			// Easter Island Summer Time
			{ "EASST", TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time (Mexico)") },
			// Easter Island Standard Time
			{ "EAST", TimeZoneInfo.FindSystemTimeZoneById("Easter Island Standard Time") },
			// East Africa Time
			{ "EAT", TimeZoneInfo.FindSystemTimeZoneById("E. Africa Standard Time") },
			// Eastern Caribbean Time (does not recognise DST)
			//{ "ECT", TimeZoneInfo.FindSystemTimeZoneById("SA Western Standard Time") },
			// Ecuador Time
			{ "ECT", TimeZoneInfo.FindSystemTimeZoneById("Cuba Standard Time") },
			// Eastern Daylight Time (North America)
			{ "EDT", TimeZoneInfo.FindSystemTimeZoneById("Venezuela Standard Time") },
			// Eastern European Summer Time
			{ "EEST", TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time") },
			// Eastern European Time
			{ "EET", TimeZoneInfo.FindSystemTimeZoneById("E. Europe Standard Time") },
			// Eastern Greenland Summer Time
			{ "EGST", TimeZoneInfo.FindSystemTimeZoneById("UTC") },
			// Eastern Greenland Time
			{ "EGT", TimeZoneInfo.FindSystemTimeZoneById("Cape Verde Standard Time") },
			// Eastern Indonesian Time
			{ "EIT", TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time") },
			// Eastern Standard Time (North America)
			{ "EST", TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time") },
			// Further-eastern European Time
			{ "FET", TimeZoneInfo.FindSystemTimeZoneById("Arabic Standard Time") },
			// Fiji Time
			{ "FJT", TimeZoneInfo.FindSystemTimeZoneById("Fiji Standard Time") },
			// Falkland Islands Summer Time
			{ "FKST", TimeZoneInfo.FindSystemTimeZoneById("Tocantins Standard Time") },
			// Falkland Islands Time
			{ "FKT", TimeZoneInfo.FindSystemTimeZoneById("Paraguay Standard Time") },
			// Fernando de Noronha Time
			{ "FNT", TimeZoneInfo.FindSystemTimeZoneById("Mid-Atlantic Standard Time") },
			// Galápagos Time
			{ "GALT", TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time") },
			// Gambier Islands Time
			{ "GAMT", TimeZoneInfo.FindSystemTimeZoneById("Alaskan Standard Time") },
			// Georgia Standard Time
			{ "GET", TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time") },
			// French Guiana Time
			{ "GFT", TimeZoneInfo.FindSystemTimeZoneById("Saint Pierre Standard Time") },
			// Gilbert Island Time
			{ "GILT", TimeZoneInfo.FindSystemTimeZoneById("Kamchatka Standard Time") },
			// Gambier Island Time
			{ "GIT", TimeZoneInfo.FindSystemTimeZoneById("Alaskan Standard Time") },
			// Greenwich Mean Time
			{ "GMT", TimeZoneInfo.FindSystemTimeZoneById("Greenwich Standard Time") },
			// South Georgia and the South Sandwich Islands Time
			//{ "GST", TimeZoneInfo.FindSystemTimeZoneById("Mid-Atlantic Standard Time") },
			// Gulf Standard Time
			{ "GST", TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time") },
			// Guyana Time
			{ "GYT", TimeZoneInfo.FindSystemTimeZoneById("Paraguay Standard Time") },
			// Hawaii–Aleutian Daylight Time
			{ "HDT", TimeZoneInfo.FindSystemTimeZoneById("Alaskan Standard Time") },
			// Heure Avancée d'Europe Centrale French-language name for CEST
			{ "HAEC", TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time") },
			// Hawaii–Aleutian Standard Time
			{ "HST", TimeZoneInfo.FindSystemTimeZoneById("Hawaiian Standard Time") },
			// Hong Kong Time
			{ "HKT", TimeZoneInfo.FindSystemTimeZoneById("China Standard Time") },
			// Heard and McDonald Islands Time
			{ "HMT", TimeZoneInfo.FindSystemTimeZoneById("West Asia Standard Time") },
			// Khovd Summer Time
			{ "HOVST", TimeZoneInfo.FindSystemTimeZoneById("China Standard Time") },
			// Khovd Standard Time
			{ "HOVT", TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time") },
			// Indochina Time
			{ "ICT", TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time") },
			// Israel Daylight Time
			{ "IDT", TimeZoneInfo.FindSystemTimeZoneById("Arabic Standard Time") },
			// Indian Ocean Time
			{ "IOT", TimeZoneInfo.FindSystemTimeZoneById("Arabic Standard Time") },
			// Iran Daylight Time
			{ "IRDT", TimeZoneInfo.FindSystemTimeZoneById("Afghanistan Standard Time") },
			// Irkutsk Time
			{ "IRKT", TimeZoneInfo.FindSystemTimeZoneById("Ulaanbaatar Standard Time") },
			// Iran Standard Time
			{ "IRST", TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time") },
			// Indian Standard Time
			{ "IST", TimeZoneInfo.FindSystemTimeZoneById("India Standard Time") },
			// Irish Standard Time[6]
			//{ "IST", TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time") },
			// Israel Standard Time
			//{ "IST", TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time") },
			// Japan Standard Time
			{ "JST", TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time") },
			// Kyrgyzstan Time
			{ "KGT", TimeZoneInfo.FindSystemTimeZoneById("Central Asia Standard Time") },
			// Kosrae Time
			{ "KOST", TimeZoneInfo.FindSystemTimeZoneById("Bougainville Standard Time") },
			// Krasnoyarsk Time
			{ "KRAT", TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time") },
			// Korea Standard Time
			{ "KST", TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time") },
			// Lord Howe Standard Time
			{ "LHST", TimeZoneInfo.FindSystemTimeZoneById("Lord Howe Standard Time") },
			// Lord Howe Summer Time
			//{ "LHST", TimeZoneInfo.FindSystemTimeZoneById("Lord Howe Standard Time") },
			// Line Islands Time
			{ "LINT", TimeZoneInfo.FindSystemTimeZoneById("Line Islands Standard Time") },
			// Magadan Time
			{ "MAGT", TimeZoneInfo.FindSystemTimeZoneById("Magadan Standard Time") },
			// Mawson Station Time
			{ "MAWT", TimeZoneInfo.FindSystemTimeZoneById("West Asia Standard Time") },
			// Mountain Daylight Time (North America)
			{ "MDT", TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time") },
			// Middle European Time Same zone as CET
			{ "MET", TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time") },
			// Middle European Summer Time Same zone as CEST
			{ "MEST", TimeZoneInfo.FindSystemTimeZoneById("Middle East Standard Time") },
			// Marshall Islands Time
			{ "MHT", TimeZoneInfo.FindSystemTimeZoneById("Kamchatka Standard Time") },
			// Macquarie Island Station Time
			{ "MIST", TimeZoneInfo.FindSystemTimeZoneById("Central Pacific Standard Time") },
			// Myanmar Standard Time
			{ "MMT", TimeZoneInfo.FindSystemTimeZoneById("Myanmar Standard Time") },
			// Moscow Time
			{ "MSK", Moscow },
			// Malaysia Standard Time
			{ "MST", TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time") },
			// Mountain Standard Time (North America)
			//{ "MST", TimeZoneInfo.FindSystemTimeZoneById("US Mountain Standard Time") },
			// Mauritius Time
			{ "MUT", TimeZoneInfo.FindSystemTimeZoneById("Mauritius Standard Time") },
			// Maldives Time
			{ "MVT", TimeZoneInfo.FindSystemTimeZoneById("West Asia Standard Time") },
			// Malaysia Time
			{ "MYT", TimeZoneInfo.FindSystemTimeZoneById("China Standard Time") },
			// New Caledonia Time
			{ "NCT", TimeZoneInfo.FindSystemTimeZoneById("Bougainville Standard Time") },
			// Norfolk Island Time
			{ "NFT", TimeZoneInfo.FindSystemTimeZoneById("Bougainville Standard Time") },
			// Nepal Time
			{ "NPT", TimeZoneInfo.FindSystemTimeZoneById("Nepal Standard Time") },
			// Niue Time
			{ "NUT", TimeZoneInfo.FindSystemTimeZoneById("UTC-11") },
			// New Zealand Daylight Time
			{ "NZDT", TimeZoneInfo.FindSystemTimeZoneById("UTC+13") },
			// New Zealand Standard Time
			{ "NZST", TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time") },
			// Omsk Time
			{ "OMST", TimeZoneInfo.FindSystemTimeZoneById("Omsk Standard Time") },
			// Oral Time
			{ "ORAT", TimeZoneInfo.FindSystemTimeZoneById("West Asia Standard Time") },
			// Pacific Daylight Time (North America)
			{ "PDT", TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time") },
			// Peru Time
			{ "PET", TimeZoneInfo.FindSystemTimeZoneById("Cuba Standard Time") },
			// Kamchatka Time
			{ "PETT", TimeZoneInfo.FindSystemTimeZoneById("Kamchatka Standard Time") },
			// Papua New Guinea Time
			{ "PGT", TimeZoneInfo.FindSystemTimeZoneById("E. Australia Standard Time") },
			// Phoenix Island Time
			{ "PHOT", TimeZoneInfo.FindSystemTimeZoneById("Samoa Standard Time") },
			// Philippine Time
			{ "PHT", TimeZoneInfo.FindSystemTimeZoneById("China Standard Time") },
			// Pakistan Standard Time
			{ "PKT", TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time") },
			// Saint Pierre and Miquelon Daylight Time
			{ "PMDT", TimeZoneInfo.FindSystemTimeZoneById("Saint Pierre Standard Time") },
			// Saint Pierre and Miquelon Standard Time
			{ "PMST", TimeZoneInfo.FindSystemTimeZoneById("Saint Pierre Standard Time") },
			// Pohnpei Standard Time
			{ "PONT", TimeZoneInfo.FindSystemTimeZoneById("Central Pacific Standard Time") },
			// Pacific Standard Time (North America)
			{ "PST", TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time") },
			// Philippine Standard Time
			//{ "PST", TimeZoneInfo.FindSystemTimeZoneById("China Standard Time") },
			// Paraguay Summer Time[7]
			{ "PYST", TimeZoneInfo.FindSystemTimeZoneById("Paraguay Standard Time") },
			// Paraguay Time[8]
			{ "PYT", TimeZoneInfo.FindSystemTimeZoneById("Paraguay Standard Time") },
			// Réunion Time
			{ "RET", TimeZoneInfo.FindSystemTimeZoneById("Caucasus Standard Time") },
			// Rothera Research Station Time
			{ "ROTT", TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time") },
			// Sakhalin Island Time
			{ "SAKT", TimeZoneInfo.FindSystemTimeZoneById("Sakhalin Standard Time") },
			// Samara Time
			{ "SAMT", TimeZoneInfo.FindSystemTimeZoneById("Saratov Standard Time") },
			// South African Standard Time
			{ "SAST", TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time") },
			// Solomon Islands Time
			{ "SBT", TimeZoneInfo.FindSystemTimeZoneById("Central Pacific Standard Time") },
			// Seychelles Time
			{ "SCT", TimeZoneInfo.FindSystemTimeZoneById("Caucasus Standard Time") },
			// Samoa Daylight Time
			{ "SDT", TimeZoneInfo.FindSystemTimeZoneById("Hawaiian Standard Time") },
			// Singapore Time
			{ "SGT", TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time") },
			// Sri Lanka Standard Time
			{ "SLST", TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time") },
			// Srednekolymsk Time
			{ "SRET", TimeZoneInfo.FindSystemTimeZoneById("Sakhalin Standard Time") },
			// Suriname Time
			{ "SRT", TimeZoneInfo.FindSystemTimeZoneById("Tocantins Standard Time") },
			// Samoa Standard Time
			//{ "SST", TimeZoneInfo.FindSystemTimeZoneById("UTC-11") },
			// Singapore Standard Time
			{ "SST", TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time") },
			// Showa Station Time
			{ "SYOT", TimeZoneInfo.FindSystemTimeZoneById("Arabic Standard Time") },
			// Tahiti Time
			{ "TAHT", TimeZoneInfo.FindSystemTimeZoneById("Hawaiian Standard Time") },
			// Thailand Standard Time
			{ "THA", TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time") },
			// Indian/Kerguelen
			{ "TFT", TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time") },
			// Tajikistan Time
			{ "TJT", TimeZoneInfo.FindSystemTimeZoneById("West Asia Standard Time") },
			// Tokelau Time
			{ "TKT", TimeZoneInfo.FindSystemTimeZoneById("Samoa Standard Time") },
			// Timor Leste Time
			{ "TLT", TimeZoneInfo.FindSystemTimeZoneById("Transbaikal Standard Time") },
			// Turkmenistan Time
			{ "TMT", TimeZoneInfo.FindSystemTimeZoneById("West Asia Standard Time") },
			// Turkey Time
			{ "TRT", TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time") },
			// Tonga Time
			{ "TOT", TimeZoneInfo.FindSystemTimeZoneById("Tonga Standard Time") },
			// Tuvalu Time
			{ "TVT", TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time") },
			// Ulaanbaatar Summer Time
			{ "ULAST", TimeZoneInfo.FindSystemTimeZoneById("Yakutsk Standard Time") },
			// Ulaanbaatar Standard Time
			{ "ULAT", TimeZoneInfo.FindSystemTimeZoneById("Ulaanbaatar Standard Time") },
			// Kaliningrad Time
			{ "USZ1", TimeZoneInfo.FindSystemTimeZoneById("Kaliningrad Standard Time") },
			// Coordinated Universal Time
			{ "UTC", TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time") },
			// Uruguay Summer Time
			{ "UYST", TimeZoneInfo.FindSystemTimeZoneById("Mid-Atlantic Standard Time") },
			// Uruguay Standard Time
			{ "UYT", TimeZoneInfo.FindSystemTimeZoneById("Bahia Standard Time") },
			// Uzbekistan Time
			{ "UZT", TimeZoneInfo.FindSystemTimeZoneById("West Asia Standard Time") },
			// Venezuelan Standard Time
			{ "VET", TimeZoneInfo.FindSystemTimeZoneById("Venezuela Standard Time") },
			// Vladivostok Time
			{ "VLAT", TimeZoneInfo.FindSystemTimeZoneById("Vladivostok Standard Time") },
			// Volgograd Time
			{ "VOLT", TimeZoneInfo.FindSystemTimeZoneById("Saratov Standard Time") },
			// Vostok Station Time
			{ "VOST", TimeZoneInfo.FindSystemTimeZoneById("Omsk Standard Time") },
			// Vanuatu Time
			{ "VUT", TimeZoneInfo.FindSystemTimeZoneById("Bougainville Standard Time") },
			// Wake Island Time
			{ "WAKT", TimeZoneInfo.FindSystemTimeZoneById("Kamchatka Standard Time") },
			// West Africa Summer Time
			{ "WAST", TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time") },
			// West Africa Time
			{ "WAT", TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time") },
			// Western European Summer Time
			{ "WEST", TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time") },
			// Western European Time
			{ "WET", TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time") },
			// Western Indonesian Time
			{ "WIT", TimeZoneInfo.FindSystemTimeZoneById("N. Central Asia Standard Time") },
			// Western Standard Time
			{ "WST", TimeZoneInfo.FindSystemTimeZoneById("China Standard Time") },
			// Yakutsk Time
			{ "YAKT", TimeZoneInfo.FindSystemTimeZoneById("Yakutsk Standard Time") },
			// Yekaterinburg Time
			{ "YEKT", TimeZoneInfo.FindSystemTimeZoneById("Ekaterinburg Standard Time") },
		});

		public static TimeZoneInfo GetTimeZoneByAbbr(string tzAbbr)
		{
			_tzAbbrs.Value.TryGetValue(tzAbbr, out var tz);
			return tz;
		}

		/// <summary>
		/// Retrieves a <see cref="TimeZoneInfo"/>  object given a valid Windows or IANA time zone idenfifier,
		/// regardless of which platform the application is running on.
		/// </summary>
		/// <param name="iana">A valid IANA time zone identifier.</param>
		/// <returns>A <see cref="TimeZoneInfo"/> object.</returns>
		public static TimeZoneInfo IanaToTimeZone(string iana)
		{
			if (iana.CompareIgnoreCase("MSK"))
				return Moscow;

			if (iana.CompareIgnoreCase("CST"))
				return Cst;

			if (iana.CompareIgnoreCase("EST"))
				return Est;

			return TimeZoneInfo.FindSystemTimeZoneById(TZConvert.IanaToWindows(iana));
		}
	}
}