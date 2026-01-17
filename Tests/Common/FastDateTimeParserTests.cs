namespace Ecng.Tests.Common;

[TestClass]
public class FastDateTimeParserTests : BaseTestClass
{
	[TestMethod]
	public void TwoDigitYear()
	{
		Do.Invariant(() =>
		{
			var parser = new FastDateTimeParser("yy-MM-dd");
			var dt = parser.Parse("98-01-01");

			dt.Year.AssertEqual(1998);
			dt.Month.AssertEqual(1);
			dt.Day.AssertEqual(1);
		});
	}

	[TestMethod]
	public void ShortInput()
	{
		var parser = new FastDateTimeParser("yyyy-MM-dd HH:mm:ss.fff");
		var shortInput = "2024-01-02 03:04:0"; // too short for seconds+millis

		ThrowsExactly<FormatException>(() => parser.Parse(shortInput));
	}

	[TestMethod]
	public void RoundTrip_BasicDate()
	{
		Do.Invariant(() =>
		{
			var parser = new FastDateTimeParser("yyyy-MM-dd");
			var dt = new DateTime(2024, 2, 29);
			var str = parser.ToString(dt);

			str.AssertEqual("2024-02-29");
			parser.Parse(str).AssertEqual(dt);
		});
	}

	[TestMethod]
	public void RoundTrip_SingleDigitMonthDay()
	{
		Do.Invariant(() =>
		{
			var parser = new FastDateTimeParser("yyyy-M-d");
			var dt = new DateTime(2024, 2, 3);
			var str = parser.ToString(dt);

			str.AssertEqual("2024-2-3");
			parser.Parse(str).AssertEqual(dt);
		});
	}

	[TestMethod]
	public void Parse_WithTimeAndMillis()
	{
		Do.Invariant(() =>
		{
			var parser = new FastDateTimeParser("yyyy-MM-dd HH:mm:ss.fff");
			var dt = parser.Parse("2024-01-02 03:04:05.006");

			dt.Year.AssertEqual(2024);
			dt.Month.AssertEqual(1);
			dt.Day.AssertEqual(2);
			dt.Hour.AssertEqual(3);
			dt.Minute.AssertEqual(4);
			dt.Second.AssertEqual(5);
			dt.Millisecond.AssertEqual(6);
		});
	}

	[TestMethod]
	public void Parse_Nanoseconds_Uses100nsResolution()
	{
		Do.Invariant(() =>
		{
			var parser = new FastDateTimeParser("yyyy-MM-dd HH:mm:ss.fffffffff");
			var dt = parser.Parse("2024-01-02 03:04:05.123456789");

			dt.Millisecond.AssertEqual(123);
			dt.GetMicroseconds().AssertEqual(456);
			dt.GetNanoseconds().AssertEqual(700); // DateTime resolution is 100ns

			parser.ToString(dt).AssertEqual("2024-01-02 03:04:05.123456700");
		});
	}

	[TestMethod]
	public void ParseDto_WithTimeZone()
	{
		Do.Invariant(() =>
		{
			var parser = new FastDateTimeParser("yyyy-MM-ddTHH:mm:ssz");
			var dto = parser.ParseDto("2020-01-01T00:00:00+03:00");

			dto.Offset.AssertEqual(TimeSpan.FromHours(3));
			dto.UtcDateTime.AssertEqual(new DateTime(2019, 12, 31, 21, 0, 0, DateTimeKind.Utc));
		});
	}

	[TestMethod]
	public void InvalidValue_OutOfRange_ThrowsInvalidCastException()
	{
		Do.Invariant(() =>
		{
			var parser = new FastDateTimeParser("yyyy-MM-dd");

			ThrowsExactly<InvalidCastException>(() => parser.Parse("2024-13-01"));
			ThrowsExactly<InvalidCastException>(() => parser.Parse("2024-00-01"));
			ThrowsExactly<InvalidCastException>(() => parser.Parse("2024-02-30"));
		});
	}

	[TestMethod]
	public void UppercaseTokens_AreSupported()
	{
		Do.Invariant(() =>
		{
			var parser = new FastDateTimeParser("YYYY-MM-DD HH:mm:ss.FFF");
			var dt = parser.Parse("2024-01-02 03:04:05.006");

			dt.AssertEqual(new DateTime(2024, 1, 2, 3, 4, 5, 6));
			parser.ToString(dt).AssertEqual("2024-01-02 03:04:05.006");
		});
	}

	/// <summary>
	/// BUG: FastDateTimeParser:280 - Sign only applied to hours, not minutes.
	/// For "-05:30" timezone, minutes should also be negative.
	/// TimeSpan(-5, 30, 0) = -4:30, but should be -5:30.
	/// </summary>
	[TestMethod]
	public void NegativeTimezone_ShouldApplySignToMinutes()
	{
		Do.Invariant(() =>
		{
			// Format with timezone offset that has non-zero minutes
			var parser = new FastDateTimeParser("yyyy-MM-dd HH:mm:sszzz");

			// India Standard Time is +05:30
			var istTime = parser.ParseDto("2024-01-15 10:30:00+05:30");
			istTime.Offset.AssertEqual(TimeSpan.FromHours(5.5), "IST +05:30 should be 5.5 hours offset");

			// Nepal Time is +05:45
			var nptTime = parser.ParseDto("2024-01-15 10:30:00+05:45");
			nptTime.Offset.AssertEqual(TimeSpan.FromMinutes(345), "NPT +05:45 should be 345 minutes offset");

			// Newfoundland Standard Time is -03:30
			var nstTime = parser.ParseDto("2024-01-15 10:30:00-03:30");
			nstTime.Offset.AssertEqual(TimeSpan.FromHours(-3.5), "NST -03:30 should be -3.5 hours offset");

			// Another negative offset with minutes
			var customTime = parser.ParseDto("2024-01-15 10:30:00-05:30");
			customTime.Offset.AssertEqual(TimeSpan.FromHours(-5.5), "-05:30 should be -5.5 hours offset, not -4.5!");
		});
	}
}
