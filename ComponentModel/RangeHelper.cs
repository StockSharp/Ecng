namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Serialization;

/// <summary>
/// Provides helper methods for manipulating Range objects.
/// </summary>
public static class RangeHelper
{
	/// <summary>
	/// Determines whether the specified range is empty, meaning it has no defined minimum or maximum value.
	/// </summary>
	/// <typeparam name="T">The type of the range values. Must implement IComparable&lt;T&gt;.</typeparam>
	/// <param name="range">The range to check.</param>
	/// <returns>true if the range has no minimum and maximum value; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the range is null.</exception>
	public static bool IsEmpty<T>(this Range<T> range)
		where T : IComparable<T>
	{
		if (range is null)
			throw new ArgumentNullException(nameof(range));

		return !range.HasMaxValue && !range.HasMinValue;
	}

	/// <summary>
	/// Joins overlapping or contiguous ranges in the collection.
	/// </summary>
	/// <typeparam name="T">The type of the range values. Must implement IComparable&lt;T&gt;.</typeparam>
	/// <param name="ranges">The collection of ranges to join.</param>
	/// <returns>A collection of combined ranges.</returns>
	public static IEnumerable<Range<T>> JoinRanges<T>(this IEnumerable<Range<T>> ranges)
		where T : IComparable<T>
	{
		var orderedRanges = ranges.OrderBy(r => r.Min).ToList();

		for (var i = 0; i < orderedRanges.Count - 1; )
		{
			if (orderedRanges[i].Contains(orderedRanges[i + 1]))
			{
				orderedRanges.RemoveAt(i + 1);
			}
			else if (orderedRanges[i + 1].Contains(orderedRanges[i]))
			{
				orderedRanges.RemoveAt(i);
			}
			else if (orderedRanges[i].Intersect(orderedRanges[i + 1]) != null)
			{
				orderedRanges[i] = new Range<T>(orderedRanges[i].Min, orderedRanges[i + 1].Max);
				orderedRanges.RemoveAt(i + 1);
			}
			else
				i++;
		}

		return orderedRanges;
	}

	/// <summary>
	/// Excludes the specified DateTimeOffset range from the current range.
	/// </summary>
	/// <param name="from">The original DateTimeOffset range.</param>
	/// <param name="excludingRange">The DateTimeOffset range to exclude.</param>
	/// <returns>A collection of remaining DateTimeOffset ranges after exclusion.</returns>
	public static IEnumerable<Range<DateTimeOffset>> Exclude(this Range<DateTimeOffset> from, Range<DateTimeOffset> excludingRange)
	{
		return new Range<long>(from.Min.UtcTicks, from.Max.UtcTicks)
			.Exclude(new Range<long>(excludingRange.Min.UtcTicks, excludingRange.Max.UtcTicks))
			.Select(r => new Range<DateTimeOffset>(r.Min.To<DateTimeOffset>(), r.Max.To<DateTimeOffset>()));
	}

	/// <summary>
	/// Excludes the specified DateTime range from the current range.
	/// </summary>
	/// <param name="from">The original DateTime range.</param>
	/// <param name="excludingRange">The DateTime range to exclude.</param>
	/// <returns>A collection of remaining DateTime ranges after exclusion.</returns>
	public static IEnumerable<Range<DateTime>> Exclude(this Range<DateTime> from, Range<DateTime> excludingRange)
	{
		return new Range<long>(from.Min.Ticks, from.Max.Ticks)
			.Exclude(new Range<long>(excludingRange.Min.Ticks, excludingRange.Max.Ticks))
			.Select(r => new Range<DateTime>(r.Min.To<DateTime>(), r.Max.To<DateTime>()));
	}

	/// <summary>
	/// Excludes the specified long range from the current long range.
	/// </summary>
	/// <param name="from">The original long range.</param>
	/// <param name="excludingRange">The long range to exclude.</param>
	/// <returns>A collection of remaining long ranges after exclusion.</returns>
	public static IEnumerable<Range<long>> Exclude(this Range<long> from, Range<long> excludingRange)
	{
		var intersectedRange = from.Intersect(excludingRange);

		if (intersectedRange is null)
			yield return from;
		else
		{
			if (from == intersectedRange)
				yield break;

			if (from.Contains(intersectedRange))
			{
				if (from.Min != intersectedRange.Min)
					yield return new Range<long>(from.Min, intersectedRange.Min - 1);

				if (from.Max != intersectedRange.Max)
					yield return new Range<long>(intersectedRange.Max + 1, from.Max);
			}
			else
			{
				if (from.Min < intersectedRange.Min)
					yield return new Range<long>(from.Min, intersectedRange.Min);
				else
					yield return new Range<long>(intersectedRange.Max, from.Max);
			}
		}
	}

	/// <summary>
	/// Generates ranges from a sequence of DateTimeOffset values within the specified boundaries.
	/// </summary>
	/// <param name="dates">The sequence of DateTimeOffset values.</param>
	/// <param name="from">The start boundary.</param>
	/// <param name="to">The end boundary.</param>
	/// <returns>A collection of DateTimeOffset ranges.</returns>
	public static IEnumerable<Range<DateTimeOffset>> GetRanges(this IEnumerable<DateTimeOffset> dates, DateTimeOffset from, DateTimeOffset to)
	{
		return dates.Select(d => d.To<long>())
			.GetRanges(from.UtcTicks, to.UtcTicks)
			.Select(r => new Range<DateTimeOffset>(r.Min.To<DateTimeOffset>(), r.Max.To<DateTimeOffset>()));
	}

	/// <summary>
	/// Generates ranges from a sequence of DateTime values within the specified boundaries.
	/// </summary>
	/// <param name="dates">The sequence of DateTime values.</param>
	/// <param name="from">The start boundary.</param>
	/// <param name="to">The end boundary.</param>
	/// <returns>A collection of DateTime ranges.</returns>
	public static IEnumerable<Range<DateTime>> GetRanges(this IEnumerable<DateTime> dates, DateTime from, DateTime to)
	{
		return dates.Select(d => d.To<long>())
			.GetRanges(from.Ticks, to.Ticks)
			.Select(r => new Range<DateTime>(r.Min.To<DateTime>(), r.Max.To<DateTime>()));
	}

	/// <summary>
	/// Generates ranges based on a sequence of long values (ticks) given the specified start and end boundaries.
	/// </summary>
	/// <param name="dates">The sequence of long values representing tick counts.</param>
	/// <param name="from">The starting tick value.</param>
	/// <param name="to">The ending tick value.</param>
	/// <returns>A collection of ranges represented by long values.</returns>
	public static IEnumerable<Range<long>> GetRanges(this IEnumerable<long> dates, long from, long to)
	{
		if (dates.IsEmpty())
			yield break;

		const long step = TimeSpan.TicksPerDay;

		var beginDate = from;
		var nextDate = beginDate + step;

		foreach (var date in dates.Skip(1))
		{
			if (date != nextDate)
			{
				yield return new Range<long>(beginDate, nextDate - 1);
				beginDate = date;
			}

			nextDate = date + step;
		}

		yield return new Range<long>(beginDate, nextDate - 1);
	}

	/// <summary>
	/// Converts the range to a SettingsStorage object for serialization.
	/// </summary>
	/// <typeparam name="T">The type of the range values. Must implement IComparable&lt;T&gt;.</typeparam>
	/// <param name="range">The range to convert.</param>
	/// <returns>A SettingsStorage object containing the range data.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the range is null.</exception>
	public static SettingsStorage ToStorage<T>(this Range<T> range)
		where T : IComparable<T>
	{
		if (range is null)
			throw new ArgumentNullException(nameof(range));

		return new SettingsStorage()
			.Set(nameof(range.Min), range.HasMinValue ? (object)range.Min : null)
			.Set(nameof(range.Max), range.HasMaxValue ? (object)range.Max : null);
	}

	/// <summary>
	/// Constructs a Range&lt;T&gt; object from the provided SettingsStorage.
	/// </summary>
	/// <typeparam name="T">The type of the range values. Must implement IComparable&lt;T&gt;.</typeparam>
	/// <param name="storage">The SettingsStorage containing the range data.</param>
	/// <returns>A new Range&lt;T&gt; initialized with the storage values.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the storage is null.</exception>
	public static Range<T> ToRange<T>(this SettingsStorage storage)
		where T : IComparable<T>
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		var range = new Range<T>();

		var min = storage.GetValue<object>(nameof(range.Min));
		var max = storage.GetValue<object>(nameof(range.Max));

		if (min is not null)
			range.Min = min.To<T>();

		if (max is not null)
			range.Max = max.To<T>();

		return range;
	}
}