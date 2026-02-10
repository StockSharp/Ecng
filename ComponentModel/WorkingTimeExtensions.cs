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

	private const string _dateFormat = "yyyyMMdd";
	private const string _timeFormat = "hh\\:mm";

	/// <summary>
	/// Encode <see cref="WorkingTime.Periods"/> to string.
	/// </summary>
	/// <param name="periods">Schedule validity periods.</param>
	/// <returns>Encoded string.</returns>
	public static string EncodeToString(this IEnumerable<WorkingTimePeriod> periods)
	{
		return periods
			.Select(p => $"{p.Till:yyyyMMdd}=" + p.Times.Select(r => $"{r.Min:hh\\:mm}-{r.Max:hh\\:mm}").Join("--") + "=" + p.SpecialDays.Select(p2 => $"{p2.Key}:" + p2.Value.Select(r => $"{r.Min:hh\\:mm}-{r.Max:hh\\:mm}").Join("--")).Join("//"))
			.Join(",");
	}

	/// <summary>
	/// Decode from string to <see cref="WorkingTime.Periods"/>.
	/// </summary>
	/// <param name="input">Encoded string.</param>
	/// <returns>Schedule validity periods.</returns>
	public static IEnumerable<WorkingTimePeriod> DecodeToPeriods(this string input)
	{
		var periods = new List<WorkingTimePeriod>();

		if (input.IsEmpty())
			return periods;

		try
		{
			foreach (var str in input.SplitByComma())
			{
				var parts = str.Split('=');
				periods.Add(new WorkingTimePeriod
				{
					Till = parts[0].ToDateTime(_dateFormat).UtcKind(),
					Times = [.. parts[1].SplitBySep("--").Select(s =>
					{
						var parts2 = s.Split('-');
						return new Range<TimeSpan>(parts2[0].ToTimeSpan(_timeFormat), parts2[1].ToTimeSpan(_timeFormat));
					})],
					SpecialDays = parts[2].SplitBySep("//").Select(s =>
					{
						var idx = s.IndexOf(':');
						return new KeyValuePair<DayOfWeek, Range<TimeSpan>[]>(s.Substring(0, idx).To<DayOfWeek>(), [.. s.Substring(idx + 1).SplitBySep("--").Select(s2 =>
						{
							var parts3 = s2.Split('-');
							return new Range<TimeSpan>(parts3[0].ToTimeSpan(_timeFormat), parts3[1].ToTimeSpan(_timeFormat));
						})]);
					}).ToDictionary()
				});
			}
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException(LocalizedStrings.ErrorParsing.Put(input), ex);
		}

		return periods;
	}

	/// <summary>
	/// Encode <see cref="WorkingTime.SpecialDays"/> to string.
	/// </summary>
	/// <param name="specialDays">Special working days and holidays.</param>
	/// <returns>Encoded string.</returns>
	public static string EncodeToString(this IDictionary<DateTime, Range<TimeSpan>[]> specialDays)
	{
		return specialDays.Select(p => $"{p.Key:yyyyMMdd}=" + p.Value.Select(r => $"{r.Min:hh\\:mm}-{r.Max:hh\\:mm}").Join("--")).JoinComma();
	}

	/// <summary>
	/// Decode from string to <see cref="WorkingTime.SpecialDays"/>.
	/// </summary>
	/// <param name="input">Encoded string.</param>
	/// <returns>Special working days and holidays.</returns>
	public static IDictionary<DateTime, Range<TimeSpan>[]> DecodeToSpecialDays(this string input)
	{
		var specialDays = new Dictionary<DateTime, Range<TimeSpan>[]>();

		if (input.IsEmpty())
			return specialDays;

		try
		{
			foreach (var str in input.SplitByComma())
			{
				var parts = str.Split('=');
				specialDays[parts[0].ToDateTime(_dateFormat)] = [.. parts[1].SplitBySep("--").Select(s =>
				{
					var parts2 = s.Split('-');
					return new Range<TimeSpan>(parts2[0].ToTimeSpan(_timeFormat), parts2[1].ToTimeSpan(_timeFormat));
				})];
			}
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException(LocalizedStrings.ErrorParsing.Put(input), ex);
		}

		return specialDays;
	}
}
