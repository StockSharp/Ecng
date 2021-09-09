namespace Ecng.ComponentModel
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	public static class RangeHelper
	{
		public static bool IsEmpty<T>(this Range<T> range)
			where T : IComparable<T>
		{
			if (range is null)
				throw new ArgumentNullException(nameof(range));

			return !range.HasMaxValue && !range.HasMinValue;
		}

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

		public static IEnumerable<Range<DateTimeOffset>> Exclude(this Range<DateTimeOffset> from, Range<DateTimeOffset> excludingRange)
		{
			return new Range<long>(from.Min.UtcTicks, from.Max.UtcTicks)
				.Exclude(new Range<long>(excludingRange.Min.UtcTicks, excludingRange.Max.UtcTicks))
				.Select(r => new Range<DateTimeOffset>(r.Min.To<DateTimeOffset>(), r.Max.To<DateTimeOffset>()));
		}

		public static IEnumerable<Range<DateTime>> Exclude(this Range<DateTime> from, Range<DateTime> excludingRange)
		{
			return new Range<long>(from.Min.Ticks, from.Max.Ticks)
				.Exclude(new Range<long>(excludingRange.Min.Ticks, excludingRange.Max.Ticks))
				.Select(r => new Range<DateTime>(r.Min.To<DateTime>(), r.Max.To<DateTime>()));
		}

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

		public static IEnumerable<Range<DateTimeOffset>> GetRanges(this IEnumerable<DateTimeOffset> dates, DateTimeOffset from, DateTimeOffset to)
		{
			return dates.Select(d => d.To<long>())
				.GetRanges(from.UtcTicks, to.UtcTicks)
				.Select(r => new Range<DateTimeOffset>(r.Min.To<DateTimeOffset>(), r.Max.To<DateTimeOffset>()));
		}

		public static IEnumerable<Range<DateTime>> GetRanges(this IEnumerable<DateTime> dates, DateTime from, DateTime to)
		{
			return dates.Select(d => d.To<long>())
				.GetRanges(from.Ticks, to.Ticks)
				.Select(r => new Range<DateTime>(r.Min.To<DateTime>(), r.Max.To<DateTime>()));
		}

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

		public static SettingsStorage ToStorage<T>(this Range<T> range)
			where T : IComparable<T>
		{
			if (range is null)
				throw new ArgumentNullException(nameof(range));

			return new SettingsStorage()
				.Set(nameof(range.Min), range.HasMinValue ? (object)range.Min : null)
				.Set(nameof(range.Max), range.HasMaxValue ? (object)range.Max : null);
		}

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
}