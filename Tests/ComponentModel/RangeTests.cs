namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;
using Ecng.Serialization;

[TestClass]
public class RangeTests
{
	[TestMethod]
	public void Parse()
	{
		Parse(new Range<int>());
		Parse(new Range<int>(0, 10));

		Parse(new Range<DateTime>());
		Parse(new Range<DateTime>(DateTime.MinValue, DateTime.MaxValue.Truncate(TimeSpan.FromSeconds(1))));

		Parse(new Range<DateTimeOffset>());
		Parse(new Range<DateTimeOffset>(DateTimeOffset.MinValue, DateTimeOffset.MaxValue.Truncate(TimeSpan.FromSeconds(1))));
		Parse(new Range<DateTimeOffset>(DateTimeOffset.Now.Truncate(TimeSpan.FromSeconds(1)), DateTimeOffset.Now.AddDays(10).Truncate(TimeSpan.FromSeconds(1))));

		Parse(new Range<string>());
		Parse(new Range<string>("1", "2"));
	}

	private static void Parse<T>(Range<T> range)
		where T : IComparable<T>
	{
		Range<T>.Parse(range.ToString()).AssertEqual(range);
		Range<T>.Parse(range.To<string>()).AssertEqual(range);
		((Range<T>)range.To<string>()).AssertEqual(range);
		range.To<string>().To<Range<T>>().AssertEqual(range);
	}

	[TestMethod]
	public void ToStorage()
	{
		var r = new Range<int>(1, 10);
		r.ToStorage().ToRange<int>().AssertEqual(r);
	}

	[TestMethod]
	public void Empty()
	{
		var r = new Range<int>();
		r.GetHashCode().AssertEqual(0);
		r.Equals(new()).AssertTrue();

		r.Min = 10;
		(r.GetHashCode() > 0).AssertTrue();
		r.Equals(new() { Min = 10 }).AssertTrue();

		r.Max = 11;
		(r.GetHashCode() > 0).AssertTrue();
		r.Equals(new() { Max = 11 }).AssertFalse();
		r.Equals(new() { Min = 10, Max = 11 }).AssertTrue();
	}

	#region RangeHelper Tests

	[TestMethod]
	public void IsEmpty_EmptyRange_ReturnsTrue()
	{
		var range = new Range<int>();
		range.IsEmpty().AssertTrue();
	}

	[TestMethod]
	public void IsEmpty_RangeWithMin_ReturnsFalse()
	{
		var range = new Range<int> { Min = 1 };
		range.IsEmpty().AssertFalse();
	}

	[TestMethod]
	public void IsEmpty_RangeWithMax_ReturnsFalse()
	{
		var range = new Range<int> { Max = 10 };
		range.IsEmpty().AssertFalse();
	}

	[TestMethod]
	public void IsEmpty_RangeWithBoth_ReturnsFalse()
	{
		var range = new Range<int>(1, 10);
		range.IsEmpty().AssertFalse();
	}

	[TestMethod]
	public void IsEmpty_NullRange_Throws()
	{
		Range<int> range = null;
		Assert.ThrowsExactly<ArgumentNullException>(() => range.IsEmpty());
	}

	[TestMethod]
	public void JoinRanges_NoOverlap_RemainsUnchanged()
	{
		var ranges = new[]
		{
			new Range<int>(1, 5),
			new Range<int>(10, 15),
			new Range<int>(20, 25)
		};

		var result = ranges.JoinRanges().ToArray();

		result.Length.AssertEqual(3);
		result[0].AssertEqual(new Range<int>(1, 5));
		result[1].AssertEqual(new Range<int>(10, 15));
		result[2].AssertEqual(new Range<int>(20, 25));
	}

	[TestMethod]
	public void JoinRanges_PartialOverlap_Joins()
	{
		var ranges = new[]
		{
			new Range<int>(1, 10),
			new Range<int>(5, 15),
			new Range<int>(20, 30)
		};

		var result = ranges.JoinRanges().ToArray();

		result.Length.AssertEqual(2);
		result[0].AssertEqual(new Range<int>(1, 15));
		result[1].AssertEqual(new Range<int>(20, 30));
	}

	[TestMethod]
	public void JoinRanges_FullyContained_RemovesDuplicate()
	{
		var ranges = new[]
		{
			new Range<int>(1, 20),
			new Range<int>(5, 10),
			new Range<int>(30, 40)
		};

		var result = ranges.JoinRanges().ToArray();

		result.Length.AssertEqual(2);
		result[0].AssertEqual(new Range<int>(1, 20));
		result[1].AssertEqual(new Range<int>(30, 40));
	}

	[TestMethod]
	public void JoinRanges_ContainedInSmaller_RemovesSmaller()
	{
		var ranges = new[]
		{
			new Range<int>(5, 10),
			new Range<int>(1, 20)
		};

		var result = ranges.JoinRanges().ToArray();

		result.Length.AssertEqual(1);
		result[0].AssertEqual(new Range<int>(1, 20));
	}

	[TestMethod]
	public void JoinRanges_MultipleOverlaps_JoinsAll()
	{
		var ranges = new[]
		{
			new Range<int>(1, 5),
			new Range<int>(4, 10),
			new Range<int>(9, 15),
			new Range<int>(14, 20)
		};

		var result = ranges.JoinRanges().ToArray();

		result.Length.AssertEqual(1);
		result[0].AssertEqual(new Range<int>(1, 20));
	}

	[TestMethod]
	public void Exclude_NoIntersection_ReturnsOriginal()
	{
		var from = new Range<long>(1, 10);
		var excluding = new Range<long>(20, 30);

		var result = from.Exclude(excluding).ToArray();

		result.Length.AssertEqual(1);
		result[0].AssertEqual(from);
	}

	[TestMethod]
	public void Exclude_FullIntersection_ReturnsEmpty()
	{
		var from = new Range<long>(1, 10);
		var excluding = new Range<long>(1, 10);

		var result = from.Exclude(excluding).ToArray();

		result.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Exclude_PartialIntersectionLeft_ReturnsTail()
	{
		var from = new Range<long>(1, 10);
		var excluding = new Range<long>(1, 5);

		var result = from.Exclude(excluding).ToArray();

		result.Length.AssertEqual(1);
		result[0].AssertEqual(new Range<long>(6, 10));
	}

	[TestMethod]
	public void Exclude_PartialIntersectionRight_ReturnsHead()
	{
		var from = new Range<long>(1, 10);
		var excluding = new Range<long>(5, 10);

		var result = from.Exclude(excluding).ToArray();

		result.Length.AssertEqual(1);
		result[0].AssertEqual(new Range<long>(1, 4));
	}

	[TestMethod]
	public void Exclude_MiddleIntersection_ReturnsTwoParts()
	{
		var from = new Range<long>(1, 20);
		var excluding = new Range<long>(8, 12);

		var result = from.Exclude(excluding).ToArray();

		result.Length.AssertEqual(2);
		result[0].AssertEqual(new Range<long>(1, 7));
		result[1].AssertEqual(new Range<long>(13, 20));
	}

	[TestMethod]
	public void Exclude_ExcludingContainsFrom_ReturnsEmpty()
	{
		var from = new Range<long>(5, 10);
		var excluding = new Range<long>(1, 20);

		var result = from.Exclude(excluding).ToArray();

		result.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Exclude_DateTime_NoIntersection()
	{
		var from = new Range<DateTime>(new DateTime(2025, 1, 1), new DateTime(2025, 1, 10));
		var excluding = new Range<DateTime>(new DateTime(2025, 2, 1), new DateTime(2025, 2, 10));

		var result = from.Exclude(excluding).ToArray();

		result.Length.AssertEqual(1);
		result[0].AssertEqual(from);
	}

	[TestMethod]
	public void Exclude_DateTime_MiddleIntersection()
	{
		var from = new Range<DateTime>(new DateTime(2025, 1, 1), new DateTime(2025, 1, 20));
		var excluding = new Range<DateTime>(new DateTime(2025, 1, 8), new DateTime(2025, 1, 12));

		var result = from.Exclude(excluding).ToArray();

		result.Length.AssertEqual(2);
		result[0].Min.AssertEqual(new DateTime(2025, 1, 1));
		result[0].Max.AssertEqual(new DateTime(2025, 1, 8).AddTicks(-1));
		result[1].Min.AssertEqual(new DateTime(2025, 1, 12).AddTicks(1));
		result[1].Max.AssertEqual(new DateTime(2025, 1, 20));
	}

	[TestMethod]
	public void Exclude_DateTimeOffset_NoIntersection()
	{
		var from = new Range<DateTimeOffset>(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero));
		var excluding = new Range<DateTimeOffset>(new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 2, 10, 0, 0, 0, TimeSpan.Zero));

		var result = from.Exclude(excluding).ToArray();

		result.Length.AssertEqual(1);
		result[0].AssertEqual(from);
	}

	[TestMethod]
	public void GetRanges_EmptySequence_ReturnsEmpty()
	{
		var dates = Array.Empty<long>();
		var result = dates.GetRanges(0, 100).ToArray();

		result.Length.AssertEqual(0);
	}

	[TestMethod]
	public void GetRanges_ConsecutiveDays_ReturnsSingleRange()
	{
		var day1 = new DateTime(2025, 1, 1).Ticks;
		var day2 = new DateTime(2025, 1, 2).Ticks;
		var day3 = new DateTime(2025, 1, 3).Ticks;

		var dates = new[] { day1, day2, day3 };
		var result = dates.GetRanges(day1, day3).ToArray();

		// GetRanges uses Skip(1) and starts from 'from' parameter, iterates through remaining dates
		result.Length.AssertEqual(1);
		result[0].Min.AssertEqual(day1);
		result[0].Max.AssertEqual(day3 + TimeSpan.TicksPerDay - 1);
	}

	[TestMethod]
	public void GetRanges_NonConsecutiveDays_ReturnsMultipleRanges()
	{
		var day1 = new DateTime(2025, 1, 1).Ticks;
		var day2 = new DateTime(2025, 1, 2).Ticks;
		var day5 = new DateTime(2025, 1, 5).Ticks;
		var day6 = new DateTime(2025, 1, 6).Ticks;

		var dates = new[] { day1, day2, day5, day6 };
		var result = dates.GetRanges(day1, day6).ToArray();

		result.Length.AssertEqual(2);
	}

	[TestMethod]
	public void GetRanges_DateTime_ConsecutiveDays()
	{
		var dates = new[]
		{
			new DateTime(2025, 1, 1),
			new DateTime(2025, 1, 2),
			new DateTime(2025, 1, 3)
		};

		var result = dates.GetRanges(dates[0], dates[2]).ToArray();

		result.Length.AssertEqual(1);
	}

	[TestMethod]
	public void GetRanges_DateTimeOffset_ConsecutiveDays()
	{
		var dates = new[]
		{
			new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2025, 1, 3, 0, 0, 0, TimeSpan.Zero)
		};

		var result = dates.GetRanges(dates[0], dates[2]).ToArray();

		result.Length.AssertEqual(1);
	}

	[TestMethod]
	public void ToStorage_EmptyRange_Roundtrips()
	{
		var range = new Range<int>();
		var storage = range.ToStorage();
		var restored = storage.ToRange<int>();

		restored.AssertEqual(range);
		restored.IsEmpty().AssertTrue();
	}

	[TestMethod]
	public void ToStorage_RangeWithMinOnly_Roundtrips()
	{
		var range = new Range<int> { Min = 10 };
		var storage = range.ToStorage();
		var restored = storage.ToRange<int>();

		restored.AssertEqual(range);
		restored.HasMinValue.AssertTrue();
		restored.HasMaxValue.AssertFalse();
	}

	[TestMethod]
	public void ToStorage_RangeWithMaxOnly_Roundtrips()
	{
		var range = new Range<int> { Max = 100 };
		var storage = range.ToStorage();
		var restored = storage.ToRange<int>();

		restored.AssertEqual(range);
		restored.HasMinValue.AssertFalse();
		restored.HasMaxValue.AssertTrue();
	}

	[TestMethod]
	public void ToStorage_FullRange_Roundtrips()
	{
		var range = new Range<int>(10, 100);
		var storage = range.ToStorage();
		var restored = storage.ToRange<int>();

		restored.AssertEqual(range);
		restored.Min.AssertEqual(10);
		restored.Max.AssertEqual(100);
	}

	[TestMethod]
	public void ToStorage_NullRange_Throws()
	{
		Range<int> range = null;
		Assert.ThrowsExactly<ArgumentNullException>(() => range.ToStorage());
	}

	[TestMethod]
	public void ToRange_NullStorage_Throws()
	{
		SettingsStorage storage = null;
		Assert.ThrowsExactly<ArgumentNullException>(() => storage.ToRange<int>());
	}

	#endregion
}
