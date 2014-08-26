namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;

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
		public static DateTime Now
		{
			get { return _startWithOffset + _timer.Elapsed; }
		}

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

		private static TimeSpan _timeZoneOffset = TimeZoneInfo.Local.BaseUtcOffset;

		/// <summary>
		/// Смещение временной зоны.
		/// </summary>
		public static TimeSpan TimeZoneOffset
		{
			get { return _timeZoneOffset; }
			set { _timeZoneOffset = value; }
		}

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
	}
}