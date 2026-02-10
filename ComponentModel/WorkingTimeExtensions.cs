namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

/// <summary>
/// Extension methods for <see cref="WorkingTime"/>.
/// </summary>
public static class WorkingTimeExtensions
{
	/// <summary>
	/// Gets the first period where <see cref="WorkingTimePeriod.Till"/> is greater than or equal to the specified date.
	/// </summary>
	/// <param name="workingTime">Working time settings.</param>
	/// <param name="date">The date to check.</param>
	/// <returns>The matching period, or <c>null</c> if none found.</returns>
	public static WorkingTimePeriod GetPeriod(this WorkingTime workingTime, DateTime date)
	{
		if (workingTime is null)
			throw new ArgumentNullException(nameof(workingTime));

		return workingTime.Periods.FirstOrDefault(p => p.Till >= date);
	}

	/// <summary>
	/// Determines whether the specified date/time falls within working hours.
	/// </summary>
	/// <param name="workingTime">Working time settings.</param>
	/// <param name="dateTime">The date/time to check.</param>
	/// <param name="isWorkingDay">Output: <c>true</c> if the day is a working day, <c>false</c> if holiday, <c>null</c> if not determined.</param>
	/// <param name="period">Output: the matching period.</param>
	/// <returns><c>true</c> if the specified time is within working hours; otherwise, <c>false</c>.</returns>
	public static bool IsWorkingTime(this WorkingTime workingTime, DateTime dateTime, out bool? isWorkingDay, out WorkingTimePeriod period)
	{
		if (workingTime is null)
			throw new ArgumentNullException(nameof(workingTime));

		isWorkingDay = null;
		period = null;

		if (!workingTime.IsEnabled)
			return true;

		period = workingTime.GetPeriod(dateTime);

		if (period is null)
			return false;

		var date = dateTime.Date;
		var timeOfDay = dateTime.TimeOfDay;

		// Check special days on WorkingTime level (specific dates)
		if (workingTime.SpecialDays.TryGetValue(date, out var specialRanges))
		{
			if (specialRanges.Length == 0)
			{
				isWorkingDay = false;
				return false;
			}

			isWorkingDay = true;
			return specialRanges.Any(r => r.Contains(timeOfDay));
		}

		// Check special days on period level (day of week)
		var dayOfWeek = dateTime.DayOfWeek;

		if (period.SpecialDays.TryGetValue(dayOfWeek, out var dowRanges))
		{
			if (dowRanges.Length == 0)
			{
				isWorkingDay = false;
				return false;
			}

			isWorkingDay = true;
			return dowRanges.Any(r => r.Contains(timeOfDay));
		}

		// Check regular times
		if (period.Times.Count == 0)
			return true;

		return period.Times.Any(r => r.Contains(timeOfDay));
	}

	/// <summary>
	/// Determines whether the specified date is a working date.
	/// </summary>
	/// <param name="workingTime">Working time settings.</param>
	/// <param name="date">The date to check.</param>
	/// <param name="checkHolidays">Whether to check for holidays and weekends.</param>
	/// <returns><c>true</c> if the date is a working date; otherwise, <c>false</c>.</returns>
	public static bool IsWorkingDate(this WorkingTime workingTime, DateTime date, bool checkHolidays = true)
	{
		if (workingTime is null)
			throw new ArgumentNullException(nameof(workingTime));

		if (!workingTime.IsEnabled)
			return true;

		date = date.Date;

		// Check special days
		if (workingTime.SpecialDays.TryGetValue(date, out var ranges))
			return ranges.Length > 0;

		if (!checkHolidays)
			return true;

		// Check weekend
		var dow = date.DayOfWeek;
		return dow is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
	}

	/// <summary>
	/// Encodes the working time periods to a compact string representation.
	/// </summary>
	/// <param name="periods">The periods to encode.</param>
	/// <returns>The encoded string.</returns>
	public static string EncodeToString(this IEnumerable<WorkingTimePeriod> periods)
	{
		if (periods is null)
			throw new ArgumentNullException(nameof(periods));

		return periods
			.Select(p =>
			{
				var times = p.Times
					.Select(t => $"{t.Min:c}-{t.Max:c}")
					.Join(",");

				return $"{p.Till:yyyy-MM-dd}={times}";
			})
			.Join(";");
	}

	/// <summary>
	/// Decodes a string representation back to working time periods.
	/// </summary>
	/// <param name="encoded">The encoded string.</param>
	/// <returns>The decoded periods.</returns>
	public static List<WorkingTimePeriod> DecodeToPeriods(this string encoded)
	{
		if (encoded.IsEmpty())
			return [];

		var periods = new List<WorkingTimePeriod>();

		foreach (var part in encoded.Split(';'))
		{
			if (part.IsEmpty())
				continue;

			var eqIdx = part.IndexOf('=');
			if (eqIdx < 0)
				throw new FormatException($"Invalid period format: '{part}'. Expected 'date=times'.");

			var datePart = part.Substring(0, eqIdx);
			var timesPart = part.Substring(eqIdx + 1);

			if (!DateTime.TryParse(datePart, out var till))
				throw new FormatException($"Invalid date format: '{datePart}'.");

			var times = new List<Range<TimeSpan>>();

			if (!timesPart.IsEmpty())
			{
				foreach (var tp in timesPart.Split(','))
				{
					var dashIdx = tp.IndexOf('-');
					if (dashIdx < 0)
						throw new FormatException($"Invalid time range format: '{tp}'. Expected 'min-max'.");

					var minPart = tp.Substring(0, dashIdx);
					if (!TimeSpan.TryParse(minPart, out var min))
						throw new FormatException($"Invalid time format: '{minPart}'.");

					var maxPart = tp.Substring(dashIdx + 1);
					if (!TimeSpan.TryParse(maxPart, out var max))
						throw new FormatException($"Invalid time format: '{maxPart}'.");

					times.Add(new Range<TimeSpan>(min, max));
				}
			}

			periods.Add(new()
			{
				Till = till,
				Times = times,
			});
		}

		return periods;
	}

	/// <summary>
	/// Decodes a string representation to special days dictionary.
	/// </summary>
	/// <param name="encoded">The encoded string.</param>
	/// <returns>The decoded special days dictionary.</returns>
	public static IDictionary<DateTime, Range<TimeSpan>[]> DecodeToSpecialDays(this string encoded)
	{
		if (encoded.IsEmpty())
			return new Dictionary<DateTime, Range<TimeSpan>[]>();

		var result = new Dictionary<DateTime, Range<TimeSpan>[]>();

		foreach (var part in encoded.Split(';'))
		{
			if (part.IsEmpty())
				continue;

			var eqIdx = part.IndexOf('=');
			if (eqIdx < 0)
				throw new FormatException($"Invalid special day format: '{part}'. Expected 'date=times'.");

			var datePart = part.Substring(0, eqIdx);
			var timesPart = part.Substring(eqIdx + 1);

			if (!DateTime.TryParse(datePart, out var date))
				throw new FormatException($"Invalid date format: '{datePart}'.");

			var ranges = new List<Range<TimeSpan>>();

			if (!timesPart.IsEmpty())
			{
				foreach (var tp in timesPart.Split(','))
				{
					var dashIdx = tp.IndexOf('-');
					if (dashIdx < 0)
						throw new FormatException($"Invalid time range format: '{tp}'. Expected 'min-max'.");

					var minPart = tp.Substring(0, dashIdx);
					if (!TimeSpan.TryParse(minPart, out var min))
						throw new FormatException($"Invalid time format: '{minPart}'.");

					var maxPart = tp.Substring(dashIdx + 1);
					if (!TimeSpan.TryParse(maxPart, out var max))
						throw new FormatException($"Invalid time format: '{maxPart}'.");

					ranges.Add(new Range<TimeSpan>(min, max));
				}
			}

			result[date.Date] = [.. ranges];
		}

		return result;
	}
}
