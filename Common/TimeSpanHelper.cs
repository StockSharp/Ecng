namespace Ecng.Common
{
	using System;

	public static class TimeSpanHelper
	{
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
	}
}