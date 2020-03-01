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

		public static readonly DateTime GregorianStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		public static readonly TimeZoneInfo Est = "Eastern Standard Time".To<TimeZoneInfo>();
		public static readonly TimeZoneInfo Cst = "Central Standard Time".To<TimeZoneInfo>();
		public static readonly TimeZoneInfo Moscow = "Russian Standard Time".To<TimeZoneInfo>();
		public static readonly TimeZoneInfo Gmt = "GMT Standard Time".To<TimeZoneInfo>();
		public static readonly TimeZoneInfo Fle = "FLE Standard Time".To<TimeZoneInfo>();
		public static readonly TimeZoneInfo China = "China Standard Time".To<TimeZoneInfo>();
		public static readonly TimeZoneInfo Korea = "Korea Standard Time".To<TimeZoneInfo>();
		public static readonly TimeZoneInfo Tokyo = "Tokyo Standard Time".To<TimeZoneInfo>();

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
			if (time.Kind != DateTimeKind.Utc)
			{
				time = time.ToUniversalTime();
				//throw new ArgumentException(nameof(time));
			}

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

		// https://en.wikipedia.org/wiki/List_of_time_zone_abbreviations

		private static readonly Dictionary<string, string> _tzAbbrs = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
		{
			// Australian Central Daylight Savings Time
			{ "ACDT", "Lord Howe Standard Time" },
			// Australian Central Standard Time
			{ "ACST", "AUS Central Standard Time" },
			// Acre Time
			{ "ACT", "SA Pacific Standard Time" },
			// Australian Central Western Standard Time (unofficial)
			{ "ACWST", "Aus Central W. Standard Time" },
			// Atlantic Daylight Time
			{ "ADT", "Tocantins Standard Time" },
			// Australian Eastern Daylight Savings Time
			{ "AEDT", "Central Pacific Standard Time" },
			// Australian Eastern Standard Time
			{ "AEST", "AUS Eastern Standard Time" },
			// Afghanistan Time
			{ "AFT", "Afghanistan Standard Time" },
			// Alaska Daylight Time
			{ "AKDT", "Pacific Standard Time" },
			// Alaska Standard Time
			{ "AKST", "Alaskan Standard Time" },
			// Amazon Summer Time (Brazil)[1]
			{ "AMST", "SA Eastern Standard Time" },
			// Amazon Time (Brazil)[2]
			{ "AMT", "Central Brazilian Standard Time" },
			// Armenia Time
			//{ "AMT", "Caucasus Standard Time" },
			// Argentina Time
			{ "ART", "Argentina Standard Time" },
			// Arabia Standard Time
			//{ "AST", "Arabic Standard Time" },
			// Atlantic Standard Time
			{ "AST", "Atlantic Standard Time" },
			// Australian Western Standard Time
			{ "AWST", "W. Australia Standard Time" },
			// Azores Summer Time
			{ "AZOST", "Greenwich Standard Time" },
			// Azores Standard Time
			{ "AZOT", "Azores Standard Time" },
			// Azerbaijan Time
			{ "AZT", "Azerbaijan Standard Time" },
			// Brunei Time
			{ "BDT", "North Asia East Standard Time" },
			// British Indian Ocean Time
			{ "BIOT", "Bangladesh Standard Time" },
			// Baker Island Time
			{ "BIT", "Dateline Standard Time" },
			// Bolivia Time
			{ "BOT", "Paraguay Standard Time" },
			// Brasília Summer Time
			{ "BRST", "Mid-Atlantic Standard Time" },
			// Brasilia Time
			{ "BRT", "Argentina Standard Time" },
			// Bangladesh Standard Time
			{ "BST", "Bangladesh Standard Time" },
			// Bougainville Standard Time[3]
			//{ "BST", "Bougainville Standard Time" },
			// British Summer Time (British Standard Time from Feb 1968 to Oct 1971)
			//{ "BST", "W. Europe Standard Time" },
			// Bhutan Time
			{ "BTT", "Bangladesh Standard Time" },
			// Central Africa Time
			{ "CAT", "South Africa Standard Time" },
			// Cocos Islands Time
			{ "CCT", "Myanmar Standard Time" },
			// Central Daylight Time (North America)
			{ "CDT", "SA Pacific Standard Time" },
			// Cuba Daylight Time[4]
			//{ "CDT", "Cuba Standard Time" },
			// Central European Summer Time (Cf. HAEC)
			{ "CEST", "E. Europe Standard Time" },
			// Central European Time
			{ "CET", "Central Europe Standard Time" },
			// Chatham Standard Time
			{ "CHAST", "Chatham Islands Standard Time" },
			// Choibalsan Standard Time
			{ "CHOT", "Korea Standard Time" },
			// Choibalsan Summer Time
			{ "CHOST", "Korea Standard Time" },
			// Chamorro Standard Time
			{ "CHST", "E. Australia Standard Time" },
			// Chuuk Time
			{ "CHUT", "E. Australia Standard Time" },
			// Clipperton Island Standard Time
			{ "CIST", "Pacific Standard Time" },
			// Central Indonesia Time
			{ "CIT", "China Standard Time" },
			// Cook Island Time
			{ "CKT", "Hawaiian Standard Time" },
			// Chile Summer Time
			{ "CLST", "Saint Pierre Standard Time" },
			// Chile Standard Time
			{ "CLT", "Atlantic Standard Time" },
			// Colombia Summer Time
			{ "COST", "Atlantic Standard Time" },
			// Colombia Time
			{ "COT", "US Eastern Standard Time" },
			// Central Standard Time (North America)
			{ "CST", Cst.Id },
			// China Standard Time
			//{ "CST", "China Standard Time" },
			// Cuba Standard Time
			//{ "CST", "SA Pacific Standard Time" },
			// China time
			{ "CT", "China Standard Time" },
			// Cape Verde Time
			{ "CVT", "Cape Verde Standard Time" },
			// Central Western Standard Time (Australia) unofficial
			{ "CWST", "Aus Central W. Standard Time" },
			// Christmas Island Time
			{ "CXT", "SE Asia Standard Time" },
			// Davis Time
			{ "DAVT", "SE Asia Standard Time" },
			// Dumont d'Urville Time
			{ "DDUT", "E. Australia Standard Time" },
			// AIX-specific equivalent of Central European Time[5]
			{ "DFT", "W. Europe Standard Time" },
			// Easter Island Summer Time
			{ "EASST", "Eastern Standard Time (Mexico)" },
			// Easter Island Standard Time
			{ "EAST", "Easter Island Standard Time" },
			// East Africa Time
			{ "EAT", "E. Africa Standard Time" },
			// Eastern Caribbean Time (does not recognise DST)
			//{ "ECT", "SA Western Standard Time" },
			// Ecuador Time
			{ "ECT", "Cuba Standard Time" },
			// Eastern Daylight Time (North America)
			{ "EDT", "Venezuela Standard Time" },
			// Eastern European Summer Time
			{ "EEST", "Russian Standard Time" },
			// Eastern European Time
			{ "EET", "E. Europe Standard Time" },
			// Eastern Greenland Summer Time
			{ "EGST", "UTC" },
			// Eastern Greenland Time
			{ "EGT", "Cape Verde Standard Time" },
			// Eastern Indonesian Time
			{ "EIT", "Korea Standard Time" },
			// Eastern Standard Time (North America)
			{ "EST", "Eastern Standard Time" },
			// Further-eastern European Time
			{ "FET", "Arabic Standard Time" },
			// Fiji Time
			{ "FJT", "Fiji Standard Time" },
			// Falkland Islands Summer Time
			{ "FKST", "Tocantins Standard Time" },
			// Falkland Islands Time
			{ "FKT", "Paraguay Standard Time" },
			// Fernando de Noronha Time
			{ "FNT", "Mid-Atlantic Standard Time" },
			// Galápagos Time
			{ "GALT", "Central America Standard Time" },
			// Gambier Islands Time
			{ "GAMT", "Alaskan Standard Time" },
			// Georgia Standard Time
			{ "GET", "Georgian Standard Time" },
			// French Guiana Time
			{ "GFT", "Saint Pierre Standard Time" },
			// Gilbert Island Time
			{ "GILT", "Kamchatka Standard Time" },
			// Gambier Island Time
			{ "GIT", "Alaskan Standard Time" },
			// Greenwich Mean Time
			{ "GMT", "GMT Standard Time" },
			// South Georgia and the South Sandwich Islands Time
			//{ "GST", "Mid-Atlantic Standard Time" },
			// Gulf Standard Time
			{ "GST", "Arabian Standard Time" },
			// Guyana Time
			{ "GYT", "Paraguay Standard Time" },
			// Hawaii–Aleutian Daylight Time
			{ "HDT", "Alaskan Standard Time" },
			// Heure Avancée d'Europe Centrale French-language name for CEST
			{ "HAEC", "Jordan Standard Time" },
			// Hawaii–Aleutian Standard Time
			{ "HST", "Hawaiian Standard Time" },
			// Hong Kong Time
			{ "HKT", "China Standard Time" },
			// Heard and McDonald Islands Time
			{ "HMT", "West Asia Standard Time" },
			// Khovd Summer Time
			{ "HOVST", "China Standard Time" },
			// Khovd Standard Time
			{ "HOVT", "SE Asia Standard Time" },
			// Indochina Time
			{ "ICT", "SE Asia Standard Time" },
			// Israel Daylight Time
			{ "IDT", "Arabic Standard Time" },
			// Indian Ocean Time
			{ "IOT", "Arabic Standard Time" },
			// Iran Daylight Time
			{ "IRDT", "Afghanistan Standard Time" },
			// Irkutsk Time
			{ "IRKT", "Ulaanbaatar Standard Time" },
			// Iran Standard Time
			{ "IRST", "Iran Standard Time" },
			// Indian Standard Time
			{ "IST", "India Standard Time" },
			// Irish Standard Time[6]
			//{ "IST", "W. Europe Standard Time" },
			// Israel Standard Time
			//{ "IST", "Israel Standard Time" },
			// Japan Standard Time
			{ "JST", "Tokyo Standard Time" },
			// Kyrgyzstan Time
			{ "KGT", "Central Asia Standard Time" },
			// Kosrae Time
			{ "KOST", "Bougainville Standard Time" },
			// Krasnoyarsk Time
			{ "KRAT", "SE Asia Standard Time" },
			// Korea Standard Time
			{ "KST", "Korea Standard Time" },
			// Lord Howe Standard Time
			{ "LHST", "Lord Howe Standard Time" },
			// Lord Howe Summer Time
			//{ "LHST", "Lord Howe Standard Time" },
			// Line Islands Time
			{ "LINT", "Line Islands Standard Time" },
			// Magadan Time
			{ "MAGT", "Magadan Standard Time" },
			// Mawson Station Time
			{ "MAWT", "West Asia Standard Time" },
			// Mountain Daylight Time (North America)
			{ "MDT", "Central America Standard Time" },
			// Middle European Time Same zone as CET
			{ "MET", "Central Europe Standard Time" },
			// Middle European Summer Time Same zone as CEST
			{ "MEST", "Middle East Standard Time" },
			// Marshall Islands Time
			{ "MHT", "Kamchatka Standard Time" },
			// Macquarie Island Station Time
			{ "MIST", "Central Pacific Standard Time" },
			// Myanmar Standard Time
			{ "MMT", "Myanmar Standard Time" },
			// Moscow Time
			{ "MSK", Moscow.Id },
			// Malaysia Standard Time
			{ "MST", "Singapore Standard Time" },
			// Mountain Standard Time (North America)
			//{ "MST", "US Mountain Standard Time" },
			// Mauritius Time
			{ "MUT", "Mauritius Standard Time" },
			// Maldives Time
			{ "MVT", "West Asia Standard Time" },
			// Malaysia Time
			{ "MYT", "China Standard Time" },
			// New Caledonia Time
			{ "NCT", "Bougainville Standard Time" },
			// Norfolk Island Time
			{ "NFT", "Bougainville Standard Time" },
			// Nepal Time
			{ "NPT", "Nepal Standard Time" },
			// Niue Time
			{ "NUT", "UTC-11" },
			// New Zealand Daylight Time
			{ "NZDT", "UTC+13" },
			// New Zealand Standard Time
			{ "NZST", "New Zealand Standard Time" },
			// Omsk Time
			{ "OMST", "Omsk Standard Time" },
			// Oral Time
			{ "ORAT", "West Asia Standard Time" },
			// Pacific Daylight Time (North America)
			{ "PDT", "Mountain Standard Time" },
			// Peru Time
			{ "PET", "Cuba Standard Time" },
			// Kamchatka Time
			{ "PETT", "Kamchatka Standard Time" },
			// Papua New Guinea Time
			{ "PGT", "E. Australia Standard Time" },
			// Phoenix Island Time
			{ "PHOT", "Samoa Standard Time" },
			// Philippine Time
			{ "PHT", "China Standard Time" },
			// Pakistan Standard Time
			{ "PKT", "Pakistan Standard Time" },
			// Saint Pierre and Miquelon Daylight Time
			{ "PMDT", "Saint Pierre Standard Time" },
			// Saint Pierre and Miquelon Standard Time
			{ "PMST", "Saint Pierre Standard Time" },
			// Pohnpei Standard Time
			{ "PONT", "Central Pacific Standard Time" },
			// Pacific Standard Time (North America)
			{ "PST", "Pacific Standard Time" },
			// Philippine Standard Time
			//{ "PST", "China Standard Time" },
			// Paraguay Summer Time[7]
			{ "PYST", "Paraguay Standard Time" },
			// Paraguay Time[8]
			{ "PYT", "Paraguay Standard Time" },
			// Réunion Time
			{ "RET", "Caucasus Standard Time" },
			// Rothera Research Station Time
			{ "ROTT", "Argentina Standard Time" },
			// Sakhalin Island Time
			{ "SAKT", "Sakhalin Standard Time" },
			// Samara Time
			{ "SAMT", "Saratov Standard Time" },
			// South African Standard Time
			{ "SAST", "South Africa Standard Time" },
			// Solomon Islands Time
			{ "SBT", "Central Pacific Standard Time" },
			// Seychelles Time
			{ "SCT", "Caucasus Standard Time" },
			// Samoa Daylight Time
			{ "SDT", "Hawaiian Standard Time" },
			// Singapore Time
			{ "SGT", "Singapore Standard Time" },
			// Sri Lanka Standard Time
			{ "SLST", "Sri Lanka Standard Time" },
			// Srednekolymsk Time
			{ "SRET", "Sakhalin Standard Time" },
			// Suriname Time
			{ "SRT", "Tocantins Standard Time" },
			// Samoa Standard Time
			//{ "SST", "UTC-11" },
			// Singapore Standard Time
			{ "SST", "Singapore Standard Time" },
			// Showa Station Time
			{ "SYOT", "Arabic Standard Time" },
			// Tahiti Time
			{ "TAHT", "Hawaiian Standard Time" },
			// Thailand Standard Time
			{ "THA", "SE Asia Standard Time" },
			// Indian/Kerguelen
			{ "TFT", "Pakistan Standard Time" },
			// Tajikistan Time
			{ "TJT", "West Asia Standard Time" },
			// Tokelau Time
			{ "TKT", "Samoa Standard Time" },
			// Timor Leste Time
			{ "TLT", "Transbaikal Standard Time" },
			// Turkmenistan Time
			{ "TMT", "West Asia Standard Time" },
			// Turkey Time
			{ "TRT", "Turkey Standard Time" },
			// Tonga Time
			{ "TOT", "Tonga Standard Time" },
			// Tuvalu Time
			{ "TVT", "New Zealand Standard Time" },
			// Ulaanbaatar Summer Time
			{ "ULAST", "Yakutsk Standard Time" },
			// Ulaanbaatar Standard Time
			{ "ULAT", "Ulaanbaatar Standard Time" },
			// Kaliningrad Time
			{ "USZ1", "Kaliningrad Standard Time" },
			// Coordinated Universal Time
			{ "UTC", "GMT Standard Time" },
			// Uruguay Summer Time
			{ "UYST", "Mid-Atlantic Standard Time" },
			// Uruguay Standard Time
			{ "UYT", "Bahia Standard Time" },
			// Uzbekistan Time
			{ "UZT", "West Asia Standard Time" },
			// Venezuelan Standard Time
			{ "VET", "Venezuela Standard Time" },
			// Vladivostok Time
			{ "VLAT", "Vladivostok Standard Time" },
			// Volgograd Time
			{ "VOLT", "Saratov Standard Time" },
			// Vostok Station Time
			{ "VOST", "Omsk Standard Time" },
			// Vanuatu Time
			{ "VUT", "Bougainville Standard Time" },
			// Wake Island Time
			{ "WAKT", "Kamchatka Standard Time" },
			// West Africa Summer Time
			{ "WAST", "South Africa Standard Time" },
			// West Africa Time
			{ "WAT", "W. Central Africa Standard Time" },
			// Western European Summer Time
			{ "WEST", "W. Europe Standard Time" },
			// Western European Time
			{ "WET", "GMT Standard Time" },
			// Western Indonesian Time
			{ "WIT", "N. Central Asia Standard Time" },
			// Western Standard Time
			{ "WST", "China Standard Time" },
			// Yakutsk Time
			{ "YAKT", "Yakutsk Standard Time" },
			// Yekaterinburg Time
			{ "YEKT", "Ekaterinburg Standard Time" },
		};

		private static readonly Dictionary<string, string> _tzAbbr2 = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
		{
			{ "Alpha Time Zone", "W. Europe Standard Time" },
			{ "Australian Central Daylight Time", "Lord Howe Standard Time" },
			{ "Australian Central Standard Time", "Cen. Australia Standard Time" },
			{ "Acre Time", "SA Pacific Standard Time" },
			{ "Australian Central Time", "Cen. Australia Standard Time" },
			{ "Australian Central Western Standard Time", "Aus Central W. Standard Time" },
			{ "Arabia Daylight Time", "Arabian Standard Time" },
			{ "Atlantic Daylight Time", "Tocantins Standard Time" },
			{ "Australian Eastern Daylight Time", "Bougainville Standard Time" },
			{ "Australian Eastern Standard Time", "E. Australia Standard Time" },
			{ "Australian Eastern Time", "E. Australia Standard Time" },
			{ "Afghanistan Time", "Afghanistan Standard Time" },
			{ "Alaska Daylight Time", "Pacific Standard Time (Mexico)" },
			{ "Alaska Standard Time", "Alaskan Standard Time" },
			{ "Alma-Ata Time", "Central Asia Standard Time" },
			{ "Amazon Summer Time", "Tocantins Standard Time" },
			{ "Armenia Summer Time", "West Asia Standard Time" },
			{ "Amazon Time", "Paraguay Standard Time" },
			{ "Armenia Time", "Arabian Standard Time" },
			{ "Anadyr Summer Time", "Russia Time Zone 11" },
			{ "Anadyr Time", "Russia Time Zone 11" },
			{ "Aqtobe Time", "West Asia Standard Time" },
			{ "Argentina Time", "Tocantins Standard Time" },
			{ "Arabia Standard Time", "Arabic Standard Time" },
			{ "Atlantic Standard Time", "Paraguay Standard Time" },
			{ "Atlantic Time", "Paraguay Standard Time" },
			{ "Australian Western Daylight Time", "Transbaikal Standard Time" },
			{ "Australian Western Standard Time", "China Standard Time" },
			{ "Azores Summer Time", "UTC" },
			{ "Azores Time", "Azores Standard Time" },
			{ "Azerbaijan Summer Time", "West Asia Standard Time" },
			{ "Azerbaijan Time", "Arabian Standard Time" },
			{ "Anywhere on Earth", "Dateline Standard Time" },
			{ "Bravo Time Zone", "Jordan Standard Time" },
			{ "Brunei Darussalam Time", "China Standard Time" },
			{ "Bolivia Time", "Paraguay Standard Time" },
			{ "Bras?lia Summer Time", "UTC-02" },
			{ "Bras?lia Time", "Tocantins Standard Time" },
			{ "Bangladesh Standard Time", "Central Asia Standard Time" },
			{ "Bougainville Standard Time", "Bougainville Standard Time" },
			{ "British Summer Time", "W. Europe Standard Time" },
			{ "Bhutan Time", "Central Asia Standard Time" },
			{ "Charlie Time Zone", "Arabic Standard Time" },
			{ "Casey Time", "China Standard Time" },
			{ "Central Africa Time", "Jordan Standard Time" },
			{ "Cocos Islands Time", "Myanmar Standard Time" },
			{ "Central Daylight Time", "SA Pacific Standard Time" },
			{ "Cuba Daylight Time", "Paraguay Standard Time" },
			{ "Central European Summer Time", "Jordan Standard Time" },
			{ "Central European Time", "W. Europe Standard Time" },
			{ "Chatham Island Daylight Time", "Chatham Islands Standard Time" },
			{ "Chatham Island Standard Time", "Chatham Islands Standard Time" },
			{ "Choibalsan Summer Time", "Transbaikal Standard Time" },
			{ "Choibalsan Time", "China Standard Time" },
			{ "Chuuk Time", "E. Australia Standard Time" },
			{ "Cayman Islands Daylight Saving Time", "Paraguay Standard Time" },
			{ "Cayman Islands Standard Time", "SA Pacific Standard Time" },
			{ "Cook Island Time", "Aleutian Standard Time" },
			{ "Chile Summer Time", "Tocantins Standard Time" },
			{ "Chile Standard Time", "Paraguay Standard Time" },
			{ "Colombia Time", "SA Pacific Standard Time" },
			{ "Central Standard Time", "Central America Standard Time" },
			{ "China Standard Time", "China Standard Time" },
			{ "Cuba Standard Time", "SA Pacific Standard Time" },
			{ "Central Time", "Central America Standard Time" },
			{ "Cape Verde Time", "Azores Standard Time" },
			{ "Christmas Island Time", "SE Asia Standard Time" },
			{ "Chamorro Standard Time", "E. Australia Standard Time" },
			{ "Delta Time Zone", "Arabian Standard Time" },
			{ "Davis Time", "SE Asia Standard Time" },
			{ "Dumont-d'Urville Time", "E. Australia Standard Time" },
			{ "Echo Time Zone", "West Asia Standard Time" },
			{ "Easter Island Summer Time", "SA Pacific Standard Time" },
			{ "Easter Island Standard Time", "Central America Standard Time" },
			{ "Eastern Africa Time", "Arabic Standard Time" },
			{ "Ecuador Time", "SA Pacific Standard Time" },
			{ "Eastern Daylight Time", "Paraguay Standard Time" },
			{ "Eastern European Summer Time", "Arabic Standard Time" },
			{ "Eastern European Time", "Jordan Standard Time" },
			{ "Eastern Greenland Summer Time", "UTC" },
			{ "East Greenland Time", "Azores Standard Time" },
			{ "Eastern Standard Time", "SA Pacific Standard Time" },
			{ "Eastern Time", "SA Pacific Standard Time" },
			{ "Foxtrot Time Zone", "Central Asia Standard Time" },
			{ "Further-Eastern European Time", "Arabic Standard Time" },
			{ "Fiji Summer Time", "UTC+13" },
			{ "Fiji Time", "Russia Time Zone 11" },
			{ "Falkland Islands Summer Time", "Tocantins Standard Time" },
			{ "Falkland Island Time", "Paraguay Standard Time" },
			{ "Fernando de Noronha Time", "UTC-02" },
			{ "Golf Time Zone", "SE Asia Standard Time" },
			{ "Galapagos Time", "Central America Standard Time" },
			{ "Gambier Time", "Alaskan Standard Time" },
			{ "Georgia Standard Time", "Arabian Standard Time" },
			{ "French Guiana Time", "Tocantins Standard Time" },
			{ "Gilbert Island Time", "Russia Time Zone 11" },
			{ "Greenwich Mean Time", "UTC" },
			{ "Gulf Standard Time", "Arabian Standard Time" },
			{ "South Georgia Time", "UTC-02" },
			{ "Guyana Time", "Paraguay Standard Time" },
			{ "Hotel Time Zone", "China Standard Time" },
			{ "Hawaii-Aleutian Daylight Time", "Alaskan Standard Time" },
			{ "Hawaii-Aleutian Standard Time", "Aleutian Standard Time" },
			{ "Hong Kong Time", "China Standard Time" },
			{ "Hovd Summer Time", "China Standard Time" },
			{ "Hovd Time", "SE Asia Standard Time" },
			{ "India Time Zone", "Transbaikal Standard Time" },
			{ "Indochina Time", "SE Asia Standard Time" },
			{ "Israel Daylight Time", "Arabic Standard Time" },
			{ "Indian Chagos Time", "Central Asia Standard Time" },
			{ "Iran Daylight Time", "Afghanistan Standard Time" },
			{ "Irkutsk Summer Time", "Transbaikal Standard Time" },
			{ "Irkutsk Time", "China Standard Time" },
			{ "Iran Standard Time", "Iran Standard Time" },
			{ "India Standard Time", "India Standard Time" },
			{ "Irish Standard Time", "W. Europe Standard Time" },
			{ "Israel Standard Time", "Jordan Standard Time" },
			{ "Japan Standard Time", "Transbaikal Standard Time" },
			{ "Kilo Time Zone", "E. Australia Standard Time" },
			{ "Kyrgyzstan Time", "Central Asia Standard Time" },
			{ "Kosrae Time", "Bougainville Standard Time" },
			{ "Krasnoyarsk Summer Time", "China Standard Time" },
			{ "Krasnoyarsk Time", "SE Asia Standard Time" },
			{ "Korea Standard Time", "Transbaikal Standard Time" },
			{ "Kuybyshev Time", "Arabian Standard Time" },
			{ "Lima Time Zone", "Bougainville Standard Time" },
			{ "Lord Howe Daylight Time", "Bougainville Standard Time" },
			{ "Lord Howe Standard Time", "Lord Howe Standard Time" },
			{ "Line Islands Time", "Line Islands Standard Time" },
			{ "Mike Time Zone", "Russia Time Zone 11" },
			{ "Magadan Summer Time", "Russia Time Zone 11" },
			{ "Magadan Time", "Bougainville Standard Time" },
			{ "Marquesas Time", "Marquesas Standard Time" },
			{ "Mawson Time", "West Asia Standard Time" },
			{ "Mountain Daylight Time", "Central America Standard Time" },
			{ "Marshall Islands Time", "Russia Time Zone 11" },
			{ "Myanmar Time", "Myanmar Standard Time" },
			{ "Moscow Daylight Time", "Arabian Standard Time" },
			{ "Moscow Standard Time", "Arabic Standard Time" },
			{ "Mountain Standard Time", "US Mountain Standard Time" },
			{ "Mountain Time", "US Mountain Standard Time" },
			{ "Mauritius Time", "Arabian Standard Time" },
			{ "Maldives Time", "West Asia Standard Time" },
			{ "Malaysia Time", "China Standard Time" },
			{ "November Time Zone", "Azores Standard Time" },
			{ "New Caledonia Time", "Bougainville Standard Time" },
			{ "Newfoundland Daylight Time", "Newfoundland Standard Time" },
			{ "Norfolk Time", "Bougainville Standard Time" },
			{ "Novosibirsk Summer Time", "SE Asia Standard Time" },
			{ "Novosibirsk Time", "Central Asia Standard Time" },
			{ "Nepal Time", "Nepal Standard Time" },
			{ "Nauru Time", "Russia Time Zone 11" },
			{ "Newfoundland Standard Time", "Newfoundland Standard Time" },
			{ "Niue Time", "UTC-11" },
			{ "New Zealand Daylight Time", "UTC+13" },
			{ "New Zealand Standard Time", "Russia Time Zone 11" },
			{ "Oscar Time Zone", "UTC-02" },
			{ "Omsk Summer Time", "SE Asia Standard Time" },
			{ "Omsk Standard Time", "Central Asia Standard Time" },
			{ "Oral Time", "West Asia Standard Time" },
			{ "Papa Time Zone", "Tocantins Standard Time" },
			{ "Pacific Daylight Time", "US Mountain Standard Time" },
			{ "Peru Time", "SA Pacific Standard Time" },
			{ "Kamchatka Summer Time", "Russia Time Zone 11" },
			{ "Kamchatka Time", "Russia Time Zone 11" },
			{ "Papua New Guinea Time", "E. Australia Standard Time" },
			{ "Phoenix Island Time", "UTC+13" },
			{ "Philippine Time", "China Standard Time" },
			{ "Pakistan Standard Time", "West Asia Standard Time" },
			{ "Pierre & Miquelon Daylight Time", "UTC-02" },
			{ "Pierre & Miquelon Standard Time", "Tocantins Standard Time" },
			{ "Pohnpei Standard Time", "Bougainville Standard Time" },
			{ "Pacific Standard Time", "Pacific Standard Time (Mexico)" },
			{ "Pitcairn Standard Time", "Pacific Standard Time (Mexico)" },
			{ "Pacific Time", "Pacific Standard Time (Mexico)" },
			{ "Palau Time", "Transbaikal Standard Time" },
			{ "Paraguay Summer Time", "Tocantins Standard Time" },
			{ "Paraguay Time", "Paraguay Standard Time" },
			{ "Pyongyang Time", "North Korea Standard Time" },
			{ "Quebec Time Zone", "Paraguay Standard Time" },
			{ "Qyzylorda Time", "Central Asia Standard Time" },
			{ "Romeo Time Zone", "SA Pacific Standard Time" },
			{ "Reunion Time", "Arabian Standard Time" },
			{ "Rothera Time", "Tocantins Standard Time" },
			{ "Sierra Time Zone", "Central America Standard Time" },
			{ "Sakhalin Time", "Bougainville Standard Time" },
			{ "Samara Time", "Arabian Standard Time" },
			{ "South Africa Standard Time", "Jordan Standard Time" },
			{ "Solomon Islands Time", "Bougainville Standard Time" },
			{ "Seychelles Time", "Arabian Standard Time" },
			{ "Singapore Time", "China Standard Time" },
			{ "Srednekolymsk Time", "Bougainville Standard Time" },
			{ "Suriname Time", "Tocantins Standard Time" },
			{ "Samoa Standard Time", "UTC-11" },
			{ "Syowa Time", "Arabic Standard Time" },
			{ "Tango Time Zone", "US Mountain Standard Time" },
			{ "Tahiti Time", "Aleutian Standard Time" },
			{ "French Southern and Antarctic Time", "West Asia Standard Time" },
			{ "Tajikistan Time", "West Asia Standard Time" },
			{ "Tokelau Time", "UTC+13" },
			{ "East Timor Time", "Transbaikal Standard Time" },
			{ "Turkmenistan Time", "West Asia Standard Time" },
			{ "Tonga Summer Time", "Line Islands Standard Time" },
			{ "Tonga Time", "UTC+13" },
			{ "Turkey Time", "Arabic Standard Time" },
			{ "Tuvalu Time", "Russia Time Zone 11" },
			{ "Uniform Time Zone", "Pacific Standard Time (Mexico)" },
			{ "Ulaanbaatar Summer Time", "Transbaikal Standard Time" },
			{ "Ulaanbaatar Time", "China Standard Time" },
			{ "Coordinated Universal Time", "UTC" },
			{ "Uruguay Summer Time", "UTC-02" },
			{ "Uruguay Time", "Tocantins Standard Time" },
			{ "Uzbekistan Time", "West Asia Standard Time" },
			{ "Victor Time Zone", "Alaskan Standard Time" },
			{ "Venezuelan Standard Time", "Paraguay Standard Time" },
			{ "Vladivostok Summer Time", "Bougainville Standard Time" },
			{ "Vladivostok Time", "E. Australia Standard Time" },
			{ "Vostok Time", "Central Asia Standard Time" },
			{ "Vanuatu Time", "Bougainville Standard Time" },
			{ "Whiskey Time Zone", "Aleutian Standard Time" },
			{ "Wake Time", "Russia Time Zone 11" },
			{ "Western Argentine Summer Time", "Tocantins Standard Time" },
			{ "West Africa Summer Time", "Jordan Standard Time" },
			{ "West Africa Time", "W. Europe Standard Time" },
			{ "Western European Summer Time", "W. Europe Standard Time" },
			{ "Western European Time", "UTC" },
			{ "Wallis and Futuna Time", "Russia Time Zone 11" },
			{ "Western Greenland Summer Time", "UTC-02" },
			{ "West Greenland Time", "Tocantins Standard Time" },
			{ "Western Indonesian Time", "SE Asia Standard Time" },
			{ "Eastern Indonesian Time", "Transbaikal Standard Time" },
			{ "Central Indonesian Time", "China Standard Time" },
			{ "West Samoa Time", "UTC+13" },
			{ "Western Sahara Summer Time", "W. Europe Standard Time" },
			{ "Western Sahara Standard Time", "UTC" },
			{ "X-ray Time Zone", "UTC-11" },
			{ "Yankee Time Zone", "Dateline Standard Time" },
			{ "Yakutsk Summer Time", "E. Australia Standard Time" },
			{ "Yakutsk Time", "Transbaikal Standard Time" },
			{ "Yap Time", "E. Australia Standard Time" },
			{ "Yekaterinburg Summer Time", "Central Asia Standard Time" },
			{ "Yekaterinburg Time", "West Asia Standard Time" },
			{ "Zulu Time Zone", "UTC" },
		};

		public static TimeZoneInfo TryGetTimeZoneByAbbr(string tzAbbr)
		{
			return _tzAbbrs.TryGetValue(tzAbbr, out var tz) || _tzAbbr2.TryGetValue(tzAbbr, out tz)
				? tz.To<TimeZoneInfo>() : null;
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

			if (iana.CompareIgnoreCase("Greenwich Mean Time"))
				return Gmt;

			return iana.To<TimeZoneInfo>();
		}

		public static bool IsDateTime(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			return type == typeof(DateTimeOffset) || type == typeof(DateTime);
		}

		public static bool IsDateOrTime(this Type type)
		{
			return type.IsDateTime() || type == typeof(TimeSpan);
		}
	}
}