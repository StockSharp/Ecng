namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

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
}
