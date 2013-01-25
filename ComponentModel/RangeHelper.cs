namespace Ecng.ComponentModel
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	public static class RangeHelper
	{
		public static bool IsEmpty<T>(this Range<T> range)
			where T : IComparable<T>
		{
			if (range == null)
				throw new ArgumentNullException("range");

			return !range.HasMaxValue && !range.HasMinValue;
		}

		public static IEnumerable<Range<T>> JoinRanges<T>(this IEnumerable<Range<T>> ranges)
			where T : IComparable<T>
		{
			var orderedRanges = ranges.OrderBy(r => r.Min).ToList();

			for (var i = 0; i < orderedRanges.Count - 1; )
			{
				if (orderedRanges[i].Intersect(orderedRanges[i + 1]) != null)
				{
					orderedRanges[i] = new Range<T>(orderedRanges[i].Min, orderedRanges[i + 1].Max);
					orderedRanges.RemoveAt(i + 1);
				}
				else
					i++;
			}

			return orderedRanges;
		}

		public static IEnumerable<Range<DateTime>> Exclude(this Range<DateTime> from, Range<DateTime> excludingRange)
		{
			var intersectedRange = from.Intersect(excludingRange);

			if (intersectedRange == null)
				yield return from;
			else
			{
				if (from == intersectedRange)
					yield break;

				if (from.Contains(intersectedRange))
				{
					if (from.Min != intersectedRange.Min)
						yield return new Range<DateTime>(from.Min, intersectedRange.Min - TimeSpan.FromTicks(1));

					if (from.Max != intersectedRange.Max)
						yield return new Range<DateTime>(intersectedRange.Max + TimeSpan.FromTicks(1), from.Max);
				}
				else
				{
					if (from.Min < intersectedRange.Min)
						yield return new Range<DateTime>(from.Min, intersectedRange.Min);
					else
						yield return new Range<DateTime>(intersectedRange.Max, from.Max);
				}
			}
		}

		public static IEnumerable<Range<DateTime>> GetRanges(this IEnumerable<DateTime> dates, DateTime from, DateTime to)
		{
			if (dates.IsEmpty())
				yield break;

			var step = TimeSpan.FromDays(1);

			var beginDate = from;
			var nextDate = beginDate + step;

			foreach (var date in dates.Skip(1))
			{
				if (date != nextDate)
				{
					yield return new Range<DateTime>(beginDate, nextDate - TimeSpan.FromTicks(1));
					beginDate = date;
				}

				nextDate = date + step;
			}

			yield return new Range<DateTime>(beginDate, nextDate - TimeSpan.FromTicks(1));
		}
	}
}