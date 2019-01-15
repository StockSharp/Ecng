namespace Ecng.Test.ComponentModel
{
	using System;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

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
	}
}
