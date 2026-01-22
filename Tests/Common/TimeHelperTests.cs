namespace Ecng.Tests.Common;

[TestClass]
public class TimeHelperTests : BaseTestClass
{
	private static readonly Lock _nowOffsetSync = new();

	[TestMethod]
	public void Microseconds()
	{
		for (var i = 0; i < 100000; i++)
		{
			var microseconds = RandomGen.GetInt(0, 1000000);

			var ts = TimeSpan.Zero;
			ts.AddMicroseconds(microseconds).GetMicroseconds().AssertEqual(microseconds % 1000);

			var dt = DateTime.Now;
			var res = (microseconds + dt.GetMicroseconds()) % 1000;

			dt.AddMicroseconds(microseconds).GetMicroseconds().AssertEqual(res);

			dt = TimeHelper.Now;
			res = (microseconds + dt.GetMicroseconds()) % 1000;

			dt.AddMicroseconds(microseconds).GetMicroseconds().AssertEqual(res);

			dt = DateTime.MaxValue - TimeSpan.FromDays(1);
			res = (microseconds + dt.GetMicroseconds()) % 1000;

			dt.AddMicroseconds(microseconds).GetMicroseconds().AssertEqual(res);
		}
	}

	[TestMethod]
	public void Nanoseconds()
	{
		for (var i = 0; i < 100000; i++)
		{
			var nanoseconds = RandomGen.GetInt(0, 999);
			var roundNs = (nanoseconds / 100) * 100;

			var ts = TimeSpan.Zero;
			ts.AddNanoseconds(nanoseconds).ToNanoseconds().AssertEqual(roundNs);

			var dt = DateTime.Now;
			var ns = dt.GetNanoseconds();
			dt = dt.AddNanoseconds(nanoseconds);
			ns = (ns + roundNs) % 1000;
			dt.GetNanoseconds().AssertEqual(ns);

			dt = DateTime.MaxValue - TimeSpan.FromDays(1 + RandomGen.GetDouble());
			ns = dt.GetNanoseconds();
			dt = dt.AddNanoseconds(nanoseconds);
			ns = (ns + roundNs) % 1000;
			dt.GetNanoseconds().AssertEqual(ns);
		}
	}

	[TestMethod]
	public void WeekOfYear()
	{
		new DateTime(2015, 1, 1).UtcKind().GetIso8601WeekOfYear().AssertEqual(1);
		new DateTime(2015, 12, 31).UtcKind().GetIso8601WeekOfYear().AssertEqual(53);

		new DateTime(2016, 1, 1).UtcKind().GetIso8601WeekOfYear().AssertEqual(53);
		new DateTime(2016, 12, 31).UtcKind().GetIso8601WeekOfYear().AssertEqual(52);
	}

	[TestMethod]
	public void UnixTime()
	{
		var dt = DateTime.Now.ToUniversalTime();
		var res = dt.ToUnix().FromUnix();
		res.Kind.AssertEqual(DateTimeKind.Utc);

		var diffMls = (dt - res).TotalMilliseconds;
		(diffMls < 1).AssertTrue($"(dt - res).TotalMilliseconds={diffMls} should be <1");
	}

	[TestMethod]
	public void UnixTimeOutOfRange()
	{
		var dt = DateTime.MinValue;
		dt = dt.UtcKind();
		ThrowsExactly<ArgumentOutOfRangeException>(() => dt.ToUnix());
	}


	[TestMethod]
	public void DateTime_Add_PreservesKind()
	{
		var utc = DateTime.UtcNow;
		(utc + TimeSpan.FromSeconds(1)).Kind.AssertEqual(DateTimeKind.Utc);

		var local = DateTime.Now;
		(local + TimeSpan.FromSeconds(1)).Kind.AssertEqual(DateTimeKind.Local);

		var unspecified = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
		(unspecified + TimeSpan.FromSeconds(1)).Kind.AssertEqual(DateTimeKind.Unspecified);
	}

	[TestMethod]
	public void UtcKind_DoesNotConvertLocalToUtc()
	{
		var local = DateTime.Now;
		var fakeUtc = local.UtcKind();
		var realUtc = local.ToUniversalTime();

		fakeUtc.Kind.AssertEqual(DateTimeKind.Utc);
		realUtc.Kind.AssertEqual(DateTimeKind.Utc);

		var offset = TimeZoneInfo.Local.GetUtcOffset(local);
		var diff = fakeUtc - realUtc;

		// UtcKind changes only DateTime.Kind, it does not convert the clock.
		// If applied to local time, the resulting value differs from the true UTC time by approximately the local offset.
		(Math.Abs((diff - offset).TotalSeconds) < 2).AssertTrue(
			$"fakeUtc-realUtc={diff} should be approximately offset={offset} (local={local}, fakeUtc={fakeUtc}, realUtc={realUtc})");
	}

	[TestMethod]
	public void Now_IsUtcAndCloseToUtcNow()
	{
		using (_nowOffsetSync.EnterScope())
		{
			var prevOffset = TimeHelper.NowOffset;

			try
			{
				TimeHelper.NowOffset = TimeSpan.Zero;

				var before = DateTime.UtcNow;
				var now = TimeHelper.Now;
				var after = DateTime.UtcNow;

				now.Kind.AssertEqual(DateTimeKind.Utc, $"Expected TimeHelper.Now.Kind to be Utc, but was {now.Kind}");

				var afterPlusOne = after.AddSeconds(1);
				(now <= afterPlusOne).AssertTrue($"now={now} should be <={afterPlusOne}");
				(now >= before.AddSeconds(-1)).AssertTrue($"now={now} should be >={before.AddSeconds(-1)}");
			}
			finally
			{
				TimeHelper.NowOffset = prevOffset;
			}
		}
	}

	[TestMethod]
	public void NowAndNowOffset()
	{
		using (_nowOffsetSync.EnterScope())
		{
			var prevOffset = TimeHelper.NowOffset;

			try
			{
				TimeHelper.NowOffset = TimeSpan.Zero;

				// Test that Now returns current time
				var before = DateTime.UtcNow;
				var now = TimeHelper.Now;
				var after = DateTime.UtcNow;

				//(now >= before).AssertTrue();
				var afterPlusOne = after.AddSeconds(1);
				(now <= afterPlusOne).AssertTrue($"now={now} should be <={afterPlusOne}");

				// Test NowOffset setter
				var offset = TimeSpan.FromHours(3);
				TimeHelper.NowOffset = offset;
				var nowWithOffset = TimeHelper.Now;
				var expectedMin = DateTime.UtcNow + offset;
				var expectedMax = DateTime.UtcNow + offset + TimeSpan.FromSeconds(1);

				var minMinusOne = expectedMin.AddSeconds(-1);
				(nowWithOffset >= minMinusOne).AssertTrue($"nowWithOffset={nowWithOffset} should be >={minMinusOne}");
				(nowWithOffset <= expectedMax).AssertTrue($"nowWithOffset={nowWithOffset} should be <={expectedMax}");
			}
			finally
			{
				TimeHelper.NowOffset = prevOffset;
			}
		}
	}

	[TestMethod]
	public void NowWithOffsetTest()
	{
		var nowWithOffset = TimeHelper.NowWithOffset;
		nowWithOffset.Offset.AssertEqual(TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));
	}

	[TestMethod]
	public void TimeZoneOffsetTest()
	{
		using (_nowOffsetSync.EnterScope())
		{
			var original = TimeHelper.TimeZoneOffset;

			try
			{
				TimeHelper.TimeZoneOffset = TimeSpan.FromHours(5);
				TimeHelper.TimeZoneOffset.AssertEqual(TimeSpan.FromHours(5));
			}
			finally
			{
				TimeHelper.TimeZoneOffset = original;
			}
		}
	}

	[TestMethod]
	public void TotalWeeksTest()
	{
		TimeSpan.FromDays(7).TotalWeeks().AssertEqual(1.0);
		TimeSpan.FromDays(14).TotalWeeks().AssertEqual(2.0);
		TimeSpan.FromDays(3.5).TotalWeeks().AssertEqual(0.5);
	}

	[TestMethod]
	public void TotalMonthsTest()
	{
		TimeSpan.FromDays(30).TotalMonths().AssertEqual(1.0);
		TimeSpan.FromDays(60).TotalMonths().AssertEqual(2.0);
		TimeSpan.FromDays(15).TotalMonths().AssertEqual(0.5);
	}

	[TestMethod]
	public void TotalYearsTest()
	{
		TimeSpan.FromDays(365).TotalYears().AssertEqual(1.0);
		TimeSpan.FromDays(730).TotalYears().AssertEqual(2.0);
		TimeSpan.FromDays(182.5).TotalYears().AssertEqual(0.5);
	}

	[TestMethod]
	public void TotalCenturiesTest()
	{
		TimeSpan.FromDays(36500).TotalCenturies().AssertEqual(1.0);
		TimeSpan.FromDays(18250).TotalCenturies().AssertEqual(0.5);
	}

	[TestMethod]
	public void TotalMilleniumsTest()
	{
		TimeSpan.FromDays(365000).TotalMilleniums().AssertEqual(1.0);
		TimeSpan.FromDays(182500).TotalMilleniums().AssertEqual(0.5);
	}

	[TestMethod]
	public void TicksConstantsTest()
	{
		TimeHelper.NanosecondsPerTick.AssertEqual(100L);
		TimeHelper.TicksPerNanosecond.AssertEqual(1.0 / 100.0);
		TimeHelper.TicksPerMicrosecond.AssertEqual(10L);
		TimeHelper.TicksPerWeek.AssertEqual(TimeSpan.TicksPerDay * 7);
		TimeHelper.TicksPerMonth.AssertEqual(TimeSpan.TicksPerDay * 30);
		TimeHelper.TicksPerYear.AssertEqual(TimeSpan.TicksPerDay * 365);
		TimeHelper.TicksPerCentury.AssertEqual(TimeHelper.TicksPerYear * 100);
		TimeHelper.TicksPerMillenium.AssertEqual(TimeHelper.TicksPerCentury * 10);
	}

	[TestMethod]
	public void TimeSpanConstantsTest()
	{
		TimeHelper.Minute1.AssertEqual(TimeSpan.FromMinutes(1));
		TimeHelper.Minute5.AssertEqual(TimeSpan.FromMinutes(5));
		TimeHelper.Minute10.AssertEqual(TimeSpan.FromMinutes(10));
		TimeHelper.Minute15.AssertEqual(TimeSpan.FromMinutes(15));
		TimeHelper.Hour.AssertEqual(TimeSpan.FromHours(1));
		TimeHelper.Day.AssertEqual(TimeSpan.FromDays(1));
		TimeHelper.Week.AssertEqual(TimeSpan.FromTicks(TimeHelper.TicksPerWeek));
		TimeHelper.Month.AssertEqual(TimeSpan.FromTicks(TimeHelper.TicksPerMonth));
		TimeHelper.Year.AssertEqual(TimeSpan.FromTicks(TimeHelper.TicksPerYear));
		TimeHelper.LessOneDay.AssertEqual(TimeSpan.FromTicks(TimeSpan.TicksPerDay - 1));
	}

	[TestMethod]
	public void NanosecondsConversionTest()
	{
		var ticks = 12345L;
		var nanoseconds = ticks.TicksToNanoseconds();
		nanoseconds.AssertEqual(ticks * TimeHelper.NanosecondsPerTick);

		var convertedBack = nanoseconds.NanosecondsToTicks();
		convertedBack.AssertEqual(ticks);
	}

	[TestMethod]
	public void MicrosecondsConversionTest()
	{
		var ticks = 1234L;
		var microseconds = ticks.TicksToMicroseconds();
		microseconds.AssertEqual(ticks / TimeHelper.TicksPerMicrosecond);

		var convertedBack = microseconds.MicrosecondsToTicks();
		convertedBack.AssertEqual(ticks - (ticks % TimeHelper.TicksPerMicrosecond));
	}

	[TestMethod]
	public void AddMicrosecondsToDateTimeOffsetTest()
	{
		var dto = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
		var result = dto.AddMicroseconds(5000);
		(result - dto).Ticks.AssertEqual(5000 * TimeHelper.TicksPerMicrosecond);
	}

	[TestMethod]
	public void AddNanosecondsToDateTimeOffsetTest()
	{
		var dto = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
		var result = dto.AddNanoseconds(5000);
		(result - dto).Ticks.AssertEqual(5000 / TimeHelper.NanosecondsPerTick);
	}

	[TestMethod]
	public void TruncateDateTimeTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 35, 47, 123);

		// Truncate to seconds
		var truncated = dt.Truncate(TimeSpan.FromSeconds(1));
		truncated.AssertEqual(new DateTime(2024, 1, 15, 14, 35, 47, 0));

		// Truncate to minutes
		truncated = dt.Truncate(TimeSpan.FromMinutes(1));
		truncated.AssertEqual(new DateTime(2024, 1, 15, 14, 35, 0, 0));

		// Truncate to hours
		truncated = dt.Truncate(TimeSpan.FromHours(1));
		truncated.AssertEqual(new DateTime(2024, 1, 15, 14, 0, 0, 0));
	}

	[TestMethod]
	public void TruncateTimeSpanTest()
	{
		var ts = TimeSpan.FromHours(2.5);

		// Truncate to hours
		var truncated = ts.Truncate(TimeSpan.FromHours(1));
		truncated.AssertEqual(TimeSpan.FromHours(2));

		// Truncate to minutes
		ts = TimeSpan.FromMinutes(123.456);
		truncated = ts.Truncate(TimeSpan.FromMinutes(1));
		truncated.AssertEqual(TimeSpan.FromMinutes(123));
	}

	[TestMethod]
	public void RangeTest()
	{
		var start = new DateTime(2024, 1, 1);
		var end = new DateTime(2024, 1, 5);
		var interval = TimeSpan.FromDays(1);

		var dates = start.Range(end, interval).ToArray();
		dates.Length.AssertEqual(5);
		dates[0].AssertEqual(new DateTime(2024, 1, 1));
		dates[1].AssertEqual(new DateTime(2024, 1, 2));
		dates[4].AssertEqual(new DateTime(2024, 1, 5));
	}

	[TestMethod]
	public void RangeInvalidIntervalTest()
	{
		var start = new DateTime(2024, 1, 1);
		var end = new DateTime(2024, 1, 5);

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			start.Range(end, TimeSpan.Zero).ToList());
	}

	[TestMethod]
	public void DaysInMonthTest()
	{
		new DateTime(2024, 1, 15).DaysInMonth().AssertEqual(31);
		new DateTime(2024, 2, 15).DaysInMonth().AssertEqual(29); // Leap year
		new DateTime(2023, 2, 15).DaysInMonth().AssertEqual(28);
		new DateTime(2024, 4, 15).DaysInMonth().AssertEqual(30);
	}

	[TestMethod]
	public void ChangeKindTest()
	{
		var dt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

		var utc = dt.ChangeKind(DateTimeKind.Utc);
		utc.Kind.AssertEqual(DateTimeKind.Utc);
		utc.Ticks.AssertEqual(dt.Ticks);

		var local = dt.ChangeKind(DateTimeKind.Local);
		local.Kind.AssertEqual(DateTimeKind.Local);
		local.Ticks.AssertEqual(dt.Ticks);
	}

	[TestMethod]
	public void UtcKindTest()
	{
		var dt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
		var utc = dt.UtcKind();
		utc.Kind.AssertEqual(DateTimeKind.Utc);
		utc.Ticks.AssertEqual(dt.Ticks);
	}

	[TestMethod]
	public void StartOfWeekTest()
	{
		// Test with Monday as start of week
		var dt = new DateTime(2024, 1, 15); // Monday
		dt.StartOfWeek(DayOfWeek.Monday).AssertEqual(new DateTime(2024, 1, 15).Date);

		dt = new DateTime(2024, 1, 17); // Wednesday
		dt.StartOfWeek(DayOfWeek.Monday).AssertEqual(new DateTime(2024, 1, 15).Date);

		// Test with Sunday as start of week
		dt = new DateTime(2024, 1, 15); // Monday
		dt.StartOfWeek(DayOfWeek.Sunday).AssertEqual(new DateTime(2024, 1, 14).Date);
	}

	[TestMethod]
	public void EndOfDayDateTimeTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 30, 0);
		var endOfDay = dt.EndOfDay();
		endOfDay.AssertEqual(new DateTime(2024, 1, 15, 23, 59, 59) + TimeSpan.FromTicks(TimeSpan.TicksPerSecond - 1));
	}

	[TestMethod]
	public void EndOfDayDateTimeOffsetTest()
	{
		var dto = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(3));
		var endOfDay = dto.EndOfDay();
		endOfDay.Date.AssertEqual(new DateTime(2024, 1, 15));
		endOfDay.Offset.AssertEqual(TimeSpan.FromHours(3));
	}

	[TestMethod]
	public void GregorianStartTest()
	{
		TimeHelper.GregorianStart.AssertEqual(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
	}

	[TestMethod]
	public void ToDateTimeTest()
	{
		var str = "2024-01-15";
		var format = "yyyy-MM-dd";
		var dt = str.ToDateTime(format);
		dt.AssertEqual(new DateTime(2024, 1, 15));
	}

	[TestMethod]
	public void ToDateTimeInvalidTest()
	{
		ThrowsExactly<InvalidCastException>(() =>
			"invalid".ToDateTime("yyyy-MM-dd"));
	}

	[TestMethod]
	public void TryToDateTimeTest()
	{
		var str = "2024-01-15";
		var format = "yyyy-MM-dd";
		var dt = str.TryToDateTime(format);
		dt.HasValue.AssertTrue();
		dt.Value.AssertEqual(new DateTime(2024, 1, 15));

		// Test with null/empty string
		((string)null).TryToDateTime(format).AssertNull();
		"".TryToDateTime(format).AssertNull();
	}

	[TestMethod]
	public void FromDateTimeTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 30, 0);
		var str = dt.FromDateTime("yyyy-MM-dd HH:mm:ss");
		str.AssertEqual("2024-01-15 14:30:00");
	}

	[TestMethod]
	public void ToTimeSpanTest()
	{
		var str = "14:30:00";
		var format = "hh\\:mm\\:ss";
		var ts = str.ToTimeSpan(format);
		ts.AssertEqual(new TimeSpan(14, 30, 0));
	}

	[TestMethod]
	public void TryToTimeSpanTest()
	{
		var str = "14:30:00";
		var format = "hh\\:mm\\:ss";
		var ts = str.TryToTimeSpan(format);
		ts.HasValue.AssertTrue();
		ts.Value.AssertEqual(new TimeSpan(14, 30, 0));

		((string)null).TryToTimeSpan(format).AssertNull();
	}

	[TestMethod]
	public void FromTimeSpanTest()
	{
		var ts = new TimeSpan(14, 30, 45);
		var str = ts.FromTimeSpan("hh\\:mm\\:ss");
		str.AssertEqual("14:30:45");
	}

	[TestMethod]
	public void ToDateTimeOffsetTest()
	{
		var str = "2024-01-15T14:30:00+03:00";
		var format = "yyyy-MM-dd'T'HH:mm:sszzz";
		var dto = str.ToDateTimeOffset(format);
		dto.DateTime.AssertEqual(new DateTime(2024, 1, 15, 14, 30, 0));
		dto.Offset.AssertEqual(TimeSpan.FromHours(3));
	}

	[TestMethod]
	public void TryToDateTimeOffsetTest()
	{
		var str = "2024-01-15T14:30:00+03:00";
		var format = "yyyy-MM-dd'T'HH:mm:sszzz";
		var dto = str.TryToDateTimeOffset(format);
		dto.HasValue.AssertTrue();

		((string)null).TryToDateTimeOffset(format).AssertNull();
	}

	[TestMethod]
	public void FromDateTimeOffsetTest()
	{
		var dto = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(3));
		var str = dto.FromDateTimeOffset("yyyy-MM-dd'T'HH:mm:sszzz");
		str.AssertEqual("2024-01-15T14:30:00+03:00");
	}

	[TestMethod]
	public void DateTimeToDateTimeOffsetTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 30, 0);
		var offset = TimeSpan.FromHours(3);
		var dto = dt.ToDateTimeOffset(offset);
		dto.Offset.AssertEqual(offset);
	}

	[TestMethod]
	public void ApplyLocalTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 30, 0);
		var dto = dt.ApplyLocal();
		dto.Offset.AssertEqual(TimeZoneInfo.Local.GetUtcOffset(dt));
	}

	[TestMethod]
	public void ApplyUtcTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 30, 0);
		var dto = dt.ApplyUtc();
		dto.Offset.AssertEqual(TimeSpan.Zero);
	}

	[TestMethod]
	public void ApplyTimeZoneTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 30, 0);
		var dto = dt.ApplyTimeZone(TimeSpan.FromHours(5));
		dto.Offset.AssertEqual(TimeSpan.FromHours(5));
	}

	[TestMethod]
	public void ApplyTimeZoneNullTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 30, 0);
		ThrowsExactly<ArgumentNullException>(() => dt.ApplyTimeZone((TimeZoneInfo)null));
	}

	[TestMethod]
	public void ConvertToUtcTest()
	{
		var dto = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(3));
		var utc = dto.ConvertToUtc();
		utc.Offset.AssertEqual(TimeSpan.Zero);
	}

	[TestMethod]
	public void TruncateDateTimeOffsetTest()
	{
		var dto = new DateTimeOffset(2024, 1, 15, 14, 35, 47, 123, TimeSpan.FromHours(3));
		var truncated = dto.Truncate(TimeSpan.FromMinutes(1));
		truncated.Minute.AssertEqual(35);
		truncated.Second.AssertEqual(0);
		truncated.Millisecond.AssertEqual(0);
	}

	[TestMethod]
	public void FromIso8601Test()
	{
		var str = "2024-01-15T14:30:00.000Z";
		var dt = str.FromIso8601();
		dt.Year.AssertEqual(2024);
		dt.Month.AssertEqual(1);
		dt.Day.AssertEqual(15);
		dt.Hour.AssertEqual(14);
		dt.Minute.AssertEqual(30);
		dt.Kind.AssertEqual(DateTimeKind.Utc);
	}

	[TestMethod]
	public void ToIso8601Test()
	{
		var dt = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc);
		var str = dt.ToIso8601();
		str.Contains("2024-01-15").AssertTrue();
		str.Contains("14:30:00").AssertTrue();
	}

	[TestMethod]
	public void UnixTimeSecondsTest()
	{
		var dt = new DateTime(1970, 1, 1, 0, 0, 10, DateTimeKind.Utc);
		dt.ToUnix(true).AssertEqual(10.0);

		var converted = 10.0.FromUnix(true);
		converted.AssertEqual(dt);
	}

	[TestMethod]
	public void UnixTimeMillisecondsTest()
	{
		var dt = new DateTime(1970, 1, 1, 0, 0, 0, 100, DateTimeKind.Utc);
		var unix = dt.ToUnix(false);
		(unix >= 100.0 && unix < 101.0).AssertTrue($"unix={unix} should be >=100.0 and <101.0");
	}

	[TestMethod]
	public void TryFromUnixTest()
	{
		0L.TryFromUnix(true).AssertNull();
		10L.TryFromUnix(true).HasValue.AssertTrue();

		0.0.TryFromUnix(true).AssertNull();
		10.0.TryFromUnix(true).HasValue.AssertTrue();
	}

	[TestMethod]
	public void FromUnixMcsTest()
	{
		var mcs = 1000000L; // 1 second in microseconds
		var dt = mcs.FromUnixMcs();
		dt.AssertEqual(TimeHelper.GregorianStart.AddMicroseconds(mcs));
	}

	[TestMethod]
	public void ToUnixMcsTest()
	{
		var dt = TimeHelper.GregorianStart.AddSeconds(1);
		var mcs = dt.ToUnixMcs();
		mcs.AssertEqual(1000000L);
	}

	[TestMethod]
	public void UnixNowPropertiesTest()
	{
		var nowS = TimeHelper.UnixNowS;
		var nowMls = TimeHelper.UnixNowMls;

		(nowS > 0).AssertTrue($"UnixNowS={nowS} should be >0");
		(nowMls > 0).AssertTrue($"UnixNowMls={nowMls} should be >0");
		(nowMls > nowS).AssertTrue($"UnixNowMls={nowMls} should be >UnixNowS={nowS}"); // Milliseconds should be larger number
	}

	[TestMethod]
	public void IsDateTimeTest()
	{
		typeof(DateTime).IsDateTime().AssertTrue();
		typeof(DateTimeOffset).IsDateTime().AssertTrue();
		typeof(TimeSpan).IsDateTime().AssertFalse();
		typeof(int).IsDateTime().AssertFalse();
	}

	[TestMethod]
	public void IsDateTimeNullTest()
	{
		ThrowsExactly<ArgumentNullException>(() => ((Type)null).IsDateTime());
	}

	[TestMethod]
	public void IsDateOrTimeTest()
	{
		typeof(DateTime).IsDateOrTime().AssertTrue();
		typeof(DateTimeOffset).IsDateOrTime().AssertTrue();
		typeof(TimeSpan).IsDateOrTime().AssertTrue();
		typeof(int).IsDateOrTime().AssertFalse();
	}

	[TestMethod]
	public void IsWeekdayDateTimeOffsetTest()
	{
		new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero).IsWeekday().AssertTrue(); // Monday
		new DateTimeOffset(2024, 1, 16, 12, 0, 0, TimeSpan.Zero).IsWeekday().AssertTrue(); // Tuesday
		new DateTimeOffset(2024, 1, 20, 12, 0, 0, TimeSpan.Zero).IsWeekend().AssertTrue(); // Saturday
		new DateTimeOffset(2024, 1, 21, 12, 0, 0, TimeSpan.Zero).IsWeekend().AssertTrue(); // Sunday
	}

	[TestMethod]
	public void IsWeekdayDateTimeTest()
	{
		new DateTime(2024, 1, 15).IsWeekday().AssertTrue(); // Monday
		new DateTime(2024, 1, 19).IsWeekday().AssertTrue(); // Friday
		new DateTime(2024, 1, 20).IsWeekend().AssertTrue(); // Saturday
		new DateTime(2024, 1, 21).IsWeekend().AssertTrue(); // Sunday
	}

	[TestMethod]
	public void IsWeekdayDayOfWeekTest()
	{
		DayOfWeek.Monday.IsWeekday().AssertTrue();
		DayOfWeek.Tuesday.IsWeekday().AssertTrue();
		DayOfWeek.Wednesday.IsWeekday().AssertTrue();
		DayOfWeek.Thursday.IsWeekday().AssertTrue();
		DayOfWeek.Friday.IsWeekday().AssertTrue();
		DayOfWeek.Saturday.IsWeekend().AssertTrue();
		DayOfWeek.Sunday.IsWeekend().AssertTrue();
	}

	[TestMethod]
	public void GetLunarPhaseTest()
	{
		// Test with known lunar dates (from NASA lunar calendar 2024)
		// Note: Algorithm uses simplified 29.53-day cycle, so we allow for ±1 phase tolerance
		// due to the inherent approximation in the calculation method

		// New Moon: January 11, 2024 06:57 UTC
		var newMoonDate = new DateTime(2024, 1, 11, 6, 57, 0, DateTimeKind.Utc);
		var newMoonPhase = newMoonDate.GetLunarPhase();
		// Should be NewMoon (0) or adjacent phase (7 or 1) due to calculation tolerance
		((int)newMoonPhase <= 1 || (int)newMoonPhase == 7).AssertTrue(
			$"New Moon date should return NewMoon (0) or adjacent phase, got: {newMoonPhase} ({(int)newMoonPhase})");

		// Full Moon: January 25, 2024 17:54 UTC
		var fullMoonDate = new DateTime(2024, 1, 25, 17, 54, 0, DateTimeKind.Utc);
		var fullMoonPhase = fullMoonDate.GetLunarPhase();
		// Should be FullMoon (4) or adjacent phase (3 or 5)
		((int)fullMoonPhase >= 3 && (int)fullMoonPhase <= 5).AssertTrue(
			$"Full Moon date should return FullMoon (4) or adjacent phase, got: {fullMoonPhase} ({(int)fullMoonPhase})");

		// Verify range for any date
		for (int month = 1; month <= 12; month++)
		{
			var testDate = new DateTime(2024, month, 15);
			var phase = testDate.GetLunarPhase();
			var phaseInt = (int)phase;
			(phaseInt >= 0 && phaseInt <= 7).AssertTrue($"phase={phase} ({phaseInt}) should be >=0 and <=7 for {testDate}");
		}
	}

	[TestMethod]
	public void ToJulianDateTest()
	{
		// Test with a known date
		var dt = new DateTime(2000, 1, 1, 12, 0, 0);
		var julian = dt.ToJulianDate();

		// Julian date for Jan 1, 2000 at noon is approximately 2451545
		(julian > 2451544 && julian < 2451546).AssertTrue($"julian={julian} should be >2451544 and <2451546");
	}

	[TestMethod]
	public void ToTimeZoneConversionTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc);
		var converted = dt.To(TimeZoneInfo.Utc, TimeZoneInfo.Local);

		// Should convert to local time
		converted.Kind.AssertEqual(DateTimeKind.Local);
	}

	[TestMethod]
	public void ToLocalTimeTest()
	{
		var dto = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.Zero);
		var localDt = dto.ToLocalTime(TimeZoneInfo.Local);

		// Verify it's a DateTime (not DateTimeOffset)
		localDt.GetType().AssertEqual(typeof(DateTime));
	}

	[TestMethod]
	public void GetNanosecondsFromTicksTest()
	{
		var ticks = 12345L;
		var ns = ticks.GetNanoseconds();
		ns.AssertEqual((int)((ticks % 10) * TimeHelper.NanosecondsPerTick));
	}

	[TestMethod]
	public void DateTimeToDateTimeOffsetWithTimeZoneTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 30, 0);
		var dto = dt.ToDateTimeOffset(TimeZoneInfo.Utc);
		dto.Offset.AssertEqual(TimeSpan.Zero);
	}

	[TestMethod]
	public void ToDateTimeOffsetNullZoneTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 30, 0);
		ThrowsExactly<ArgumentNullException>(() => dt.ToDateTimeOffset((TimeZoneInfo)null));
	}

	[TestMethod]
	public void ConvertDateTimeOffsetTest()
	{
		var dto = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(3));
		var converted = dto.Convert(TimeZoneInfo.Utc);
		converted.Offset.AssertEqual(TimeSpan.Zero);
	}

	[TestMethod]
	public void ToTimeZoneDefaultSourceTest()
	{
		// Test with UTC kind
		var dt = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc);
		var result = dt.To(destination: TimeZoneInfo.Local);
		result.Kind.AssertEqual(DateTimeKind.Local);

		// Test with local kind
		dt = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Local);
		result = dt.To(destination: TimeZoneInfo.Utc);
		result.Kind.AssertEqual(DateTimeKind.Utc);
	}

	[TestMethod]
	public void UnixTimeDateTimeOffsetTest()
	{
		var dto = new DateTimeOffset(1970, 1, 1, 0, 0, 10, TimeSpan.Zero);
		dto.ToUnix(true).AssertEqual(10.0);
		dto.ToUnix(false).AssertEqual(10000.0);
	}

	[TestMethod]
	public void TimeZoneInfoPropertiesTest()
	{
		// Just verify the timezone properties are accessible and not null
		TimeHelper.Est.AssertNotNull();
		TimeHelper.Cst.AssertNotNull();
		TimeHelper.Moscow.AssertNotNull();
		TimeHelper.Gmt.AssertNotNull();
		TimeHelper.Fle.AssertNotNull();
		TimeHelper.China.AssertNotNull();
		TimeHelper.Korea.AssertNotNull();
		TimeHelper.Tokyo.AssertNotNull();
		TimeHelper.Tunisia.AssertNotNull();
	}

	[TestMethod]
	public void ApplyChinaTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 30, 0);
		var dto = dt.ApplyChina();
		dto.Offset.AssertEqual(TimeHelper.China.GetUtcOffset(dt));
	}

	[TestMethod]
	public void ApplyEstTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 30, 0);
		var dto = dt.ApplyEst();
		dto.Offset.AssertEqual(TimeHelper.Est.GetUtcOffset(dt));
	}

	[TestMethod]
	public void ApplyMoscowTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 30, 0);
		var dto = dt.ApplyMoscow();
		dto.Offset.AssertEqual(TimeHelper.Moscow.GetUtcOffset(dt));
	}

	[TestMethod]
	public void ConvertToChinaTest()
	{
		var dto = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.Zero);
		var converted = dto.ConvertToChina();
		converted.Offset.AssertEqual(TimeHelper.China.GetUtcOffset(dto.DateTime));
	}

	[TestMethod]
	public void ConvertToEstTest()
	{
		var dto = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.Zero);
		var converted = dto.ConvertToEst();
		converted.Offset.AssertEqual(TimeHelper.Est.GetUtcOffset(dto.DateTime));
	}

	[TestMethod]
	public void ConvertToMoscowTest()
	{
		var dto = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.Zero);
		var converted = dto.ConvertToMoscow();
		converted.Offset.AssertEqual(TimeHelper.Moscow.GetUtcOffset(dto.DateTime));
	}

	[TestMethod]
	public void GetIso8601WeekOfYearWithCultureTest()
	{
		var dt = new DateTime(2024, 1, 15);
		var week = dt.GetIso8601WeekOfYear(System.Globalization.CultureInfo.InvariantCulture);
		(week >= 1 && week <= 53).AssertTrue($"week={week} should be >=1 and <=53");
	}

	[TestMethod]
	public void FromUnixMcsDoubleTest()
	{
		var mcs = 1000000.0;
		var dt = mcs.FromUnixMcs();
		dt.AssertEqual(TimeHelper.GregorianStart.AddMicroseconds((long)mcs));
	}

	[TestMethod]
	public void GetMicrosecondsFromTimeSpanTest()
	{
		var ts = TimeSpan.FromMilliseconds(1.234);
		var microseconds = ts.GetMicroseconds();
		(microseconds >= 0 && microseconds < 1000).AssertTrue($"microseconds={microseconds} should be >=0 and <1000");
	}

	[TestMethod]
	public void GetMicrosecondsFromDateTimeTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 30, 0).AddTicks(12345);
		var microseconds = dt.GetMicroseconds();
		(microseconds >= 0 && microseconds < 1000).AssertTrue($"microseconds={microseconds} should be >=0 and <1000");
	}

	[TestMethod]
	public void ToNanosecondsTimeSpanTest()
	{
		var ts = TimeSpan.FromTicks(12345);
		var ns = ts.ToNanoseconds();
		ns.AssertEqual(12345 * TimeHelper.NanosecondsPerTick);
	}

	[TestMethod]
	public void GetUnixDiffTest()
	{
		var dt = TimeHelper.GregorianStart.AddSeconds(100);
		var diff = dt.GetUnixDiff();
		diff.AssertEqual(TimeSpan.FromSeconds(100));
	}

	[TestMethod]
	public void TruncateDateTimeByTicksTest()
	{
		var dt = new DateTime(2024, 1, 15, 14, 35, 47, 123);
		var truncated = dt.Truncate(TimeSpan.TicksPerSecond);
		truncated.Millisecond.AssertEqual(0);
	}

	[TestMethod]
	public void TruncateTimeSpanByTicksTest()
	{
		var ts = TimeSpan.FromMilliseconds(1234.567);
		var truncated = ts.Truncate(TimeSpan.TicksPerMillisecond);
		var totalMls = truncated.TotalMilliseconds;
		(totalMls >= 1234.0 && totalMls < 1235.0).AssertTrue($"truncated.TotalMilliseconds={totalMls} should be >=1234.0 and <1235.0");
	}

	[TestMethod]
	public void TruncateDateTimeOffsetByTicksTest()
	{
		var dto = new DateTimeOffset(2024, 1, 15, 14, 35, 47, 123, TimeSpan.FromHours(3));
		var truncated = dto.Truncate(TimeSpan.TicksPerSecond);
		truncated.Millisecond.AssertEqual(0);
		truncated.Offset.AssertEqual(TimeSpan.FromHours(3));
	}

	#region Now and Offset Properties

	[TestMethod]
	public void Now_ShouldReturnCurrentTime()
	{
		using (_nowOffsetSync.EnterScope())
		{
			var prevOffset = TimeHelper.NowOffset;

			try
			{
				TimeHelper.NowOffset = TimeSpan.Zero;

				var before = DateTime.UtcNow;
				var now = TimeHelper.Now;
				var after = DateTime.UtcNow;

				// Now should be between before and after (with some tolerance)
				var beforeMinusOne = before.AddSeconds(-1);
				(now >= beforeMinusOne).AssertTrue($"now={now} should be >={beforeMinusOne}");
				var afterPlusOne = after.AddSeconds(1);
				(now <= afterPlusOne).AssertTrue($"now={now} should be <={afterPlusOne}");
			}
			finally
			{
				TimeHelper.NowOffset = prevOffset;
			}
		}
	}

	[TestMethod]
	public void NowOffset_WhenSet_ShouldOffsetNowProperty()
	{
		using (_nowOffsetSync.EnterScope())
		{
			var originalOffset = TimeHelper.NowOffset;

			try
			{
				// Set 5 hour offset
				TimeHelper.NowOffset = TimeSpan.FromHours(5);

				var utcNow = DateTime.UtcNow;
				var now = TimeHelper.Now;

				// Now should be approximately UTC + 5 hours
				var expected = utcNow.AddHours(5);
				var diff = Math.Abs((now - expected).TotalSeconds);
				(diff < 2).AssertTrue(); // Within 2 seconds tolerance
			}
			finally
			{
				TimeHelper.NowOffset = originalOffset;
			}
		}
	}

	[TestMethod]
	public void NowWithOffset_ShouldReturnDateTimeOffsetWithLocalOffset()
	{
		var nowWithOffset = TimeHelper.NowWithOffset;
		var localOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);

		nowWithOffset.Offset.AssertEqual(localOffset);
	}

	[TestMethod]
	public void TimeZoneOffset_CanGetAndSet()
	{
		using (_nowOffsetSync.EnterScope())
		{
			var original = TimeHelper.TimeZoneOffset;

			try
			{
				var newOffset = TimeSpan.FromHours(8);
				TimeHelper.TimeZoneOffset = newOffset;
				TimeHelper.TimeZoneOffset.AssertEqual(newOffset);
			}
			finally
			{
				TimeHelper.TimeZoneOffset = original;
			}
		}
	}

	#endregion

	#region TimeSpan Extensions - Total*

	[TestMethod]
	public void TotalWeeks_ShouldReturnCorrectValue()
	{
		TimeSpan.FromDays(7).TotalWeeks().AssertEqual(1.0);
		TimeSpan.FromDays(14).TotalWeeks().AssertEqual(2.0);
		TimeSpan.FromDays(3.5).TotalWeeks().AssertEqual(0.5);
		TimeSpan.FromDays(0).TotalWeeks().AssertEqual(0.0);
	}

	[TestMethod]
	public void TotalMonths_ShouldReturnCorrectValue()
	{
		// Assuming 30 days per month
		TimeSpan.FromDays(30).TotalMonths().AssertEqual(1.0);
		TimeSpan.FromDays(60).TotalMonths().AssertEqual(2.0);
		TimeSpan.FromDays(15).TotalMonths().AssertEqual(0.5);
		TimeSpan.FromDays(90).TotalMonths().AssertEqual(3.0);
	}

	[TestMethod]
	public void TotalYears_ShouldReturnCorrectValue()
	{
		// Assuming 365 days per year
		TimeSpan.FromDays(365).TotalYears().AssertEqual(1.0);
		TimeSpan.FromDays(730).TotalYears().AssertEqual(2.0);
		TimeSpan.FromDays(182.5).TotalYears().AssertEqual(0.5);
	}

	[TestMethod]
	public void TotalCenturies_ShouldReturnCorrectValue()
	{
		// 100 years = 36500 days
		TimeSpan.FromDays(36500).TotalCenturies().AssertEqual(1.0);
		TimeSpan.FromDays(73000).TotalCenturies().AssertEqual(2.0);
	}

	[TestMethod]
	public void TotalMilleniums_ShouldReturnCorrectValue()
	{
		// 1000 years = 365000 days
		TimeSpan.FromDays(365000).TotalMilleniums().AssertEqual(1.0);
		TimeSpan.FromDays(730000).TotalMilleniums().AssertEqual(2.0);
	}

	#endregion

	#region Constants

	[TestMethod]
	public void TickConstants_ShouldHaveCorrectValues()
	{
		// 1 tick = 100 nanoseconds
		TimeHelper.NanosecondsPerTick.AssertEqual(100L);
		TimeHelper.TicksPerNanosecond.AssertEqual(0.01);

		// 1 microsecond = 10 ticks
		TimeHelper.TicksPerMicrosecond.AssertEqual(10L);

		// 1 week = 7 days
		TimeHelper.TicksPerWeek.AssertEqual(TimeSpan.TicksPerDay * 7);

		// 1 month = 30 days
		TimeHelper.TicksPerMonth.AssertEqual(TimeSpan.TicksPerDay * 30);

		// 1 year = 365 days
		TimeHelper.TicksPerYear.AssertEqual(TimeSpan.TicksPerDay * 365);

		// 1 century = 100 years
		TimeHelper.TicksPerCentury.AssertEqual(TimeHelper.TicksPerYear * 100);

		// 1 millennium = 10 centuries
		TimeHelper.TicksPerMillenium.AssertEqual(TimeHelper.TicksPerCentury * 10);
	}

	[TestMethod]
	public void TimeSpanConstants_ShouldHaveCorrectValues()
	{
		TimeHelper.Minute1.AssertEqual(TimeSpan.FromMinutes(1));
		TimeHelper.Minute5.AssertEqual(TimeSpan.FromMinutes(5));
		TimeHelper.Minute10.AssertEqual(TimeSpan.FromMinutes(10));
		TimeHelper.Minute15.AssertEqual(TimeSpan.FromMinutes(15));
		TimeHelper.Hour.AssertEqual(TimeSpan.FromHours(1));
		TimeHelper.Day.AssertEqual(TimeSpan.FromDays(1));
		TimeHelper.Week.AssertEqual(TimeSpan.FromDays(7));
		TimeHelper.Month.AssertEqual(TimeSpan.FromDays(30));
		TimeHelper.Year.AssertEqual(TimeSpan.FromDays(365));

		// LessOneDay should be 1 day minus 1 tick
		TimeHelper.LessOneDay.AssertEqual(TimeSpan.FromDays(1) - TimeSpan.FromTicks(1));
	}

	#endregion

	#region Microseconds

	[TestMethod]
	public void MicrosecondsToTicks_ShouldConvertCorrectly()
	{
		// 1 microsecond = 10 ticks
		1000L.MicrosecondsToTicks().AssertEqual(10000L);
		500L.MicrosecondsToTicks().AssertEqual(5000L);
		0L.MicrosecondsToTicks().AssertEqual(0L);
	}

	[TestMethod]
	public void TicksToMicroseconds_ShouldConvertCorrectly()
	{
		// 10 ticks = 1 microsecond
		10000L.TicksToMicroseconds().AssertEqual(1000L);
		5000L.TicksToMicroseconds().AssertEqual(500L);
		0L.TicksToMicroseconds().AssertEqual(0L);
	}

	[TestMethod]
	public void GetMicroseconds_FromTimeSpan_ShouldReturnComponentOnly()
	{
		// Should return only the microseconds component (0-999)
		var ts = TimeSpan.FromTicks(12345); // 1234.5 microseconds
		var result = ts.GetMicroseconds();
		(result >= 0 && result < 1000).AssertTrue($"result={result} should be >=0 and <1000");
		// 12345 ticks = 1234 microseconds, component = 234
		result.AssertEqual(234);
	}

	[TestMethod]
	public void GetMicroseconds_FromDateTime_ShouldReturnComponentOnly()
	{
		var dt = new DateTime(2024, 1, 1, 0, 0, 0);
		dt = dt.AddTicks(12345);
		var result = dt.GetMicroseconds();
		(result >= 0 && result < 1000).AssertTrue($"result={result} should be >=0 and <1000");
		result.AssertEqual(234);
	}

	[TestMethod]
	public void AddMicroseconds_ToTimeSpan_ShouldAddCorrectly()
	{
		var ts = TimeSpan.FromSeconds(1);
		var result = ts.AddMicroseconds(500);

		// Should add 500 microseconds = 5000 ticks
		result.Ticks.AssertEqual(ts.Ticks + 5000);
	}

	[TestMethod]
	public void AddMicroseconds_ToDateTime_ShouldAddCorrectly()
	{
		var dt = new DateTime(2024, 1, 1, 12, 0, 0);
		var result = dt.AddMicroseconds(1000);

		// Should add 1000 microseconds = 10000 ticks
		result.Ticks.AssertEqual(dt.Ticks + 10000);
	}

	[TestMethod]
	public void AddMicroseconds_ToDateTimeOffset_ShouldAddCorrectly()
	{
		var dto = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(3));
		var result = dto.AddMicroseconds(1000);

		result.Ticks.AssertEqual(dto.Ticks + 10000);
		result.Offset.AssertEqual(dto.Offset); // Offset should remain
	}

	#endregion

	#region Nanoseconds

	[TestMethod]
	public void NanosecondsToTicks_ShouldConvertCorrectly()
	{
		// 100 nanoseconds = 1 tick
		100L.NanosecondsToTicks().AssertEqual(1L);
		1000L.NanosecondsToTicks().AssertEqual(10L);
		0L.NanosecondsToTicks().AssertEqual(0L);
	}

	[TestMethod]
	public void TicksToNanoseconds_ShouldConvertCorrectly()
	{
		// 1 tick = 100 nanoseconds
		1L.TicksToNanoseconds().AssertEqual(100L);
		10L.TicksToNanoseconds().AssertEqual(1000L);
		0L.TicksToNanoseconds().AssertEqual(0L);
	}

	[TestMethod]
	public void GetNanoseconds_FromTicks_ShouldReturnComponentOnly()
	{
		// Should return only nanoseconds component (0-900 in steps of 100)
		// 5 ticks = 500 nanoseconds, component = 500
		var result = 5L.GetNanoseconds();
		result.AssertEqual(500);

		// 12 ticks: 12 % 10 = 2, 2 * 100 = 200
		result = 12L.GetNanoseconds();
		result.AssertEqual(200);
	}

	[TestMethod]
	public void GetNanoseconds_FromTimeSpan_ShouldReturnComponentOnly()
	{
		var ts = TimeSpan.FromTicks(15);
		var result = ts.GetNanoseconds();
		// 15 % 10 = 5, 5 * 100 = 500
		result.AssertEqual(500);
	}

	[TestMethod]
	public void GetNanoseconds_FromDateTime_ShouldReturnComponentOnly()
	{
		var dt = new DateTime(2024, 1, 1).AddTicks(17);
		var result = dt.GetNanoseconds();
		// 17 % 10 = 7, 7 * 100 = 700
		result.AssertEqual(700);
	}

	[TestMethod]
	public void ToNanoseconds_FromTimeSpan_ShouldConvertCorrectly()
	{
		var ts = TimeSpan.FromTicks(10);
		ts.ToNanoseconds().AssertEqual(1000L);
	}

	[TestMethod]
	public void AddNanoseconds_ToTimeSpan_ShouldAddCorrectly()
	{
		var ts = TimeSpan.FromSeconds(1);
		var result = ts.AddNanoseconds(500);

		// 500 nanoseconds = 5 ticks
		result.Ticks.AssertEqual(ts.Ticks + 5);
	}

	[TestMethod]
	public void AddNanoseconds_ToDateTime_ShouldAddCorrectly()
	{
		var dt = new DateTime(2024, 1, 1, 12, 0, 0);
		var result = dt.AddNanoseconds(1000);

		// 1000 nanoseconds = 10 ticks
		result.Ticks.AssertEqual(dt.Ticks + 10);
	}

	[TestMethod]
	public void AddNanoseconds_ToDateTimeOffset_ShouldAddCorrectly()
	{
		var dto = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));
		var result = dto.AddNanoseconds(1000);

		result.Ticks.AssertEqual(dto.Ticks + 10);
		result.Offset.AssertEqual(dto.Offset);
	}

	#endregion

	#region Truncate

	[TestMethod]
	public void Truncate_DateTime_ByTimeSpan_ShouldTruncateCorrectly()
	{
		var dt = new DateTime(2024, 6, 15, 14, 35, 47, 123);

		// Truncate to seconds - should remove milliseconds
		var truncated = dt.Truncate(TimeSpan.FromSeconds(1));
		truncated.AssertEqual(new DateTime(2024, 6, 15, 14, 35, 47, 0));

		// Truncate to minutes - should remove seconds and below
		truncated = dt.Truncate(TimeSpan.FromMinutes(1));
		truncated.AssertEqual(new DateTime(2024, 6, 15, 14, 35, 0, 0));

		// Truncate to hours
		truncated = dt.Truncate(TimeSpan.FromHours(1));
		truncated.AssertEqual(new DateTime(2024, 6, 15, 14, 0, 0, 0));

		// Truncate to days
		truncated = dt.Truncate(TimeSpan.FromDays(1));
		truncated.AssertEqual(new DateTime(2024, 6, 15, 0, 0, 0, 0));
	}

	[TestMethod]
	public void Truncate_DateTime_ByTicks_ShouldTruncateCorrectly()
	{
		var dt = new DateTime(2024, 6, 15, 14, 35, 47, 123);
		var truncated = dt.Truncate(TimeSpan.TicksPerSecond);

		truncated.Second.AssertEqual(47);
		truncated.Millisecond.AssertEqual(0);
	}

	[TestMethod]
	public void Truncate_TimeSpan_ShouldTruncateCorrectly()
	{
		var ts = TimeSpan.FromHours(2.5);
		var truncated = ts.Truncate(TimeSpan.FromHours(1));
		truncated.AssertEqual(TimeSpan.FromHours(2));

		ts = new TimeSpan(1, 23, 45, 30, 500);
		truncated = ts.Truncate(TimeSpan.FromMinutes(1));
		truncated.AssertEqual(new TimeSpan(1, 23, 45, 0, 0));
	}

	[TestMethod]
	public void Truncate_DateTimeOffset_ShouldTruncateAndPreserveOffset()
	{
		var dto = new DateTimeOffset(2024, 6, 15, 14, 35, 47, 123, TimeSpan.FromHours(5));
		var truncated = dto.Truncate(TimeSpan.FromMinutes(1));

		truncated.Year.AssertEqual(2024);
		truncated.Minute.AssertEqual(35);
		truncated.Second.AssertEqual(0);
		truncated.Millisecond.AssertEqual(0);
		truncated.Offset.AssertEqual(TimeSpan.FromHours(5));
	}

	#endregion

	#region Range

	[TestMethod]
	public void Range_ShouldGenerateCorrectSequence()
	{
		var start = new DateTime(2024, 1, 1);
		var end = new DateTime(2024, 1, 5);
		var interval = TimeSpan.FromDays(1);

		var dates = start.Range(end, interval).ToArray();

		dates.Length.AssertEqual(5);
		dates[0].AssertEqual(new DateTime(2024, 1, 1));
		dates[1].AssertEqual(new DateTime(2024, 1, 2));
		dates[2].AssertEqual(new DateTime(2024, 1, 3));
		dates[3].AssertEqual(new DateTime(2024, 1, 4));
		dates[4].AssertEqual(new DateTime(2024, 1, 5));
	}

	[TestMethod]
	public void Range_WithHourInterval_ShouldWork()
	{
		var start = new DateTime(2024, 1, 1, 10, 0, 0);
		var end = new DateTime(2024, 1, 1, 13, 0, 0);
		var interval = TimeSpan.FromHours(1);

		var dates = start.Range(end, interval).ToArray();

		dates.Length.AssertEqual(4);
		dates[0].Hour.AssertEqual(10);
		dates[3].Hour.AssertEqual(13);
	}

	[TestMethod]
	public void Range_WithZeroInterval_ShouldThrow()
	{
		var start = new DateTime(2024, 1, 1);
		var end = new DateTime(2024, 1, 5);

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			start.Range(end, TimeSpan.Zero).ToList());
	}

	[TestMethod]
	public void Range_WithNegativeInterval_ShouldThrow()
	{
		var start = new DateTime(2024, 1, 1);
		var end = new DateTime(2024, 1, 5);

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			start.Range(end, TimeSpan.FromDays(-1)).ToList());
	}

	#endregion

	#region DateTime Helpers

	[TestMethod]
	public void DaysInMonth_ShouldReturnCorrectValues()
	{
		new DateTime(2024, 1, 15).DaysInMonth().AssertEqual(31);
		new DateTime(2024, 2, 15).DaysInMonth().AssertEqual(29); // Leap year
		new DateTime(2023, 2, 15).DaysInMonth().AssertEqual(28); // Not leap year
		new DateTime(2024, 4, 15).DaysInMonth().AssertEqual(30);
		new DateTime(2024, 12, 1).DaysInMonth().AssertEqual(31);
	}

	[TestMethod]
	public void ChangeKind_ShouldChangeKindWithoutChangingTicks()
	{
		var dt = new DateTime(2024, 6, 15, 12, 30, 0, DateTimeKind.Unspecified);
		var originalTicks = dt.Ticks;

		var utc = dt.ChangeKind(DateTimeKind.Utc);
		utc.Kind.AssertEqual(DateTimeKind.Utc);
		utc.Ticks.AssertEqual(originalTicks);

		var local = dt.ChangeKind(DateTimeKind.Local);
		local.Kind.AssertEqual(DateTimeKind.Local);
		local.Ticks.AssertEqual(originalTicks);

		var unspec = utc.ChangeKind(DateTimeKind.Unspecified);
		unspec.Kind.AssertEqual(DateTimeKind.Unspecified);
		unspec.Ticks.AssertEqual(originalTicks);
	}

	[TestMethod]
	public void UtcKind_ShouldSetKindToUtc()
	{
		var dt = new DateTime(2024, 6, 15, 12, 30, 0, DateTimeKind.Unspecified);
		var utc = dt.UtcKind();

		utc.Kind.AssertEqual(DateTimeKind.Utc);
		utc.Ticks.AssertEqual(dt.Ticks);
	}

	[TestMethod]
	public void StartOfWeek_WithMonday_ShouldReturnCorrectDate()
	{
		// Jan 15, 2024 is Monday
		var monday = new DateTime(2024, 1, 15);
		monday.StartOfWeek(DayOfWeek.Monday).AssertEqual(new DateTime(2024, 1, 15));

		// Jan 17, 2024 is Wednesday
		var wednesday = new DateTime(2024, 1, 17);
		wednesday.StartOfWeek(DayOfWeek.Monday).AssertEqual(new DateTime(2024, 1, 15));

		// Jan 21, 2024 is Sunday
		var sunday = new DateTime(2024, 1, 21);
		sunday.StartOfWeek(DayOfWeek.Monday).AssertEqual(new DateTime(2024, 1, 15));
	}

	[TestMethod]
	public void StartOfWeek_WithSunday_ShouldReturnCorrectDate()
	{
		// Jan 21, 2024 is Sunday
		var sunday = new DateTime(2024, 1, 21);
		sunday.StartOfWeek(DayOfWeek.Sunday).AssertEqual(new DateTime(2024, 1, 21));

		// Jan 15, 2024 is Monday
		var monday = new DateTime(2024, 1, 15);
		monday.StartOfWeek(DayOfWeek.Sunday).AssertEqual(new DateTime(2024, 1, 14));
	}

	[TestMethod]
	public void EndOfDay_DateTime_ShouldReturnLastTickOfDay()
	{
		var dt = new DateTime(2024, 6, 15, 14, 30, 0);
		var endOfDay = dt.EndOfDay();

		endOfDay.Year.AssertEqual(2024);
		endOfDay.Month.AssertEqual(6);
		endOfDay.Day.AssertEqual(15);
		endOfDay.Hour.AssertEqual(23);
		endOfDay.Minute.AssertEqual(59);
		endOfDay.Second.AssertEqual(59);

		// Should be one tick before midnight
		(endOfDay.Ticks + 1).AssertEqual(new DateTime(2024, 6, 16).Ticks);
	}

	[TestMethod]
	public void EndOfDay_DateTimeOffset_ShouldReturnLastTickAndPreserveOffset()
	{
		var dto = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.FromHours(3));
		var endOfDay = dto.EndOfDay();

		endOfDay.Day.AssertEqual(15);
		endOfDay.Hour.AssertEqual(23);
		endOfDay.Minute.AssertEqual(59);
		endOfDay.Second.AssertEqual(59);
		endOfDay.Offset.AssertEqual(TimeSpan.FromHours(3));
	}

	#endregion

	#region String Parsing and Formatting

	[TestMethod]
	public void ToDateTime_WithValidString_ShouldParse()
	{
		var result = "2024-06-15".ToDateTime("yyyy-MM-dd");
		result.AssertEqual(new DateTime(2024, 6, 15));

		result = "15/06/2024 14:30".ToDateTime("dd/MM/yyyy HH:mm");
		result.AssertEqual(new DateTime(2024, 6, 15, 14, 30, 0));
	}

	[TestMethod]
	public void ToDateTime_WithInvalidString_ShouldThrowInvalidCastException()
	{
		ThrowsExactly<InvalidCastException>(() =>
			"not-a-date".ToDateTime("yyyy-MM-dd"));

		ThrowsExactly<InvalidCastException>(() =>
			"2024-13-01".ToDateTime("yyyy-MM-dd")); // Invalid month
	}

	[TestMethod]
	public void TryToDateTime_WithValidString_ShouldReturnValue()
	{
		var result = "2024-06-15".TryToDateTime("yyyy-MM-dd");
		result.HasValue.AssertTrue();
		result.Value.AssertEqual(new DateTime(2024, 6, 15));
	}

	[TestMethod]
	public void TryToDateTime_WithEmptyString_ShouldReturnNull()
	{
		"".TryToDateTime("yyyy-MM-dd").AssertNull();
		((string)null).TryToDateTime("yyyy-MM-dd").AssertNull();
	}

	[TestMethod]
	public void FromDateTime_ShouldFormatCorrectly()
	{
		var dt = new DateTime(2024, 6, 15, 14, 30, 45);

		dt.FromDateTime("yyyy-MM-dd").AssertEqual("2024-06-15");
		dt.FromDateTime("dd/MM/yyyy HH:mm:ss").AssertEqual("15/06/2024 14:30:45");
	}

	[TestMethod]
	public void ToTimeSpan_WithValidString_ShouldParse()
	{
		var result = "14:30:00".ToTimeSpan("hh\\:mm\\:ss");
		result.AssertEqual(new TimeSpan(14, 30, 0));
	}

	[TestMethod]
	public void TryToTimeSpan_WithEmptyString_ShouldReturnNull()
	{
		"".TryToTimeSpan("hh\\:mm\\:ss").AssertNull();
		((string)null).TryToTimeSpan("hh\\:mm\\:ss").AssertNull();
	}

	[TestMethod]
	public void FromTimeSpan_ShouldFormatCorrectly()
	{
		var ts = new TimeSpan(2, 14, 30, 45);
		ts.FromTimeSpan("d\\.hh\\:mm\\:ss").AssertEqual("2.14:30:45");
	}

	#endregion

	#region DateTimeOffset Conversion

	[TestMethod]
	public void ToDateTimeOffset_WithTimeSpan_ShouldApplyOffset()
	{
		var dt = new DateTime(2024, 6, 15, 12, 0, 0);
		var offset = TimeSpan.FromHours(5);

		var dto = dt.ApplyTimeZone(offset);

		dto.Offset.AssertEqual(offset);
		dto.Year.AssertEqual(2024);
		dto.Hour.AssertEqual(12);
	}

	[TestMethod]
	public void ToDateTimeOffset_WithTimeZone_ShouldApplyZoneOffset()
	{
		var dt = new DateTime(2024, 6, 15, 12, 0, 0);
		var dto = dt.ToDateTimeOffset(TimeZoneInfo.Utc);

		dto.Offset.AssertEqual(TimeSpan.Zero);
	}

	[TestMethod]
	public void ToDateTimeOffset_WithNullZone_ShouldThrow()
	{
		var dt = new DateTime(2024, 6, 15, 12, 0, 0);
		ThrowsExactly<ArgumentNullException>(() =>
			dt.ToDateTimeOffset((TimeZoneInfo)null));
	}

	[TestMethod]
	public void ApplyUtc_ShouldCreateOffsetWithZero()
	{
		var dt = new DateTime(2024, 6, 15, 12, 0, 0);
		var dto = dt.ApplyUtc();

		dto.Offset.AssertEqual(TimeSpan.Zero);
	}

	[TestMethod]
	public void ApplyLocal_ShouldUseLocalOffset()
	{
		var dt = new DateTime(2024, 6, 15, 12, 0, 0);
		var dto = dt.ApplyLocal();

		var expectedOffset = TimeZoneInfo.Local.GetUtcOffset(dt);
		dto.Offset.AssertEqual(expectedOffset);
	}

	[TestMethod]
	public void ApplyTimeZone_WithTimeSpan_ShouldApplyOffset()
	{
		var dt = new DateTime(2024, 6, 15, 12, 0, 0);
		var dto = dt.ApplyTimeZone(TimeSpan.FromHours(7));

		dto.Offset.AssertEqual(TimeSpan.FromHours(7));
	}

	[TestMethod]
	public void ApplyTimeZone_WithNullZone_ShouldThrow()
	{
		var dt = new DateTime(2024, 6, 15, 12, 0, 0);
		ThrowsExactly<ArgumentNullException>(() =>
			dt.ApplyTimeZone((TimeZoneInfo)null));
	}

	#endregion

	#region DateTimeOffset TimeZone Conversion

	[TestMethod]
	public void ConvertToUtc_ShouldConvertToUTC()
	{
		var dto = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.FromHours(5));
		var utc = dto.ConvertToUtc();

		utc.Offset.AssertEqual(TimeSpan.Zero);
		// Time should be adjusted: 12:00 +5 = 07:00 UTC
		utc.Hour.AssertEqual(7);
	}

	[TestMethod]
	public void Convert_ToSpecificZone_ShouldConvert()
	{
		var dto = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
		var local = dto.Convert(TimeZoneInfo.Local);

		local.Offset.AssertEqual(TimeZoneInfo.Local.GetUtcOffset(dto.DateTime));
	}

	[TestMethod]
	public void ToLocalTime_ShouldReturnDateTime()
	{
		var dto = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
		var dt = dto.ToLocalTime(TimeZoneInfo.Local);

		dt.GetType().AssertEqual(typeof(DateTime));
	}

	#endregion

	#region ISO8601

	[TestMethod]
	public void ToIso8601_ShouldFormatCorrectly()
	{
		var dt = new DateTime(2024, 6, 15, 14, 30, 45, 123, DateTimeKind.Utc);
		var str = dt.ToIso8601();

		str.Contains("2024-06-15").AssertTrue();
		str.Contains("14:30:45").AssertTrue();
		str.Contains(".123").AssertTrue();
	}

	[TestMethod]
	public void FromIso8601_ShouldParseCorrectly()
	{
		var dt = "2024-06-15T14:30:45.123Z".FromIso8601();

		dt.Year.AssertEqual(2024);
		dt.Month.AssertEqual(6);
		dt.Day.AssertEqual(15);
		dt.Hour.AssertEqual(14);
		dt.Minute.AssertEqual(30);
		dt.Second.AssertEqual(45);
		dt.Kind.AssertEqual(DateTimeKind.Utc);
	}

	[TestMethod]
	public void GetIso8601WeekOfYear_ShouldCalculateCorrectly()
	{
		// Jan 1, 2024 is Monday - should be week 1
		new DateTime(2024, 1, 1).GetIso8601WeekOfYear().AssertEqual(1);

		// Dec 31, 2024 is Tuesday - should be week 1 of next year or last week
		var week = new DateTime(2024, 12, 31).GetIso8601WeekOfYear();
		(week >= 1 && week <= 53).AssertTrue($"week={week} should be >=1 and <=53");
	}

	#endregion

	#region Unix Time

	[TestMethod]
	public void GregorianStart_ShouldBe1970Jan1()
	{
		TimeHelper.GregorianStart.Year.AssertEqual(1970);
		TimeHelper.GregorianStart.Month.AssertEqual(1);
		TimeHelper.GregorianStart.Day.AssertEqual(1);
		TimeHelper.GregorianStart.Hour.AssertEqual(0);
		TimeHelper.GregorianStart.Kind.AssertEqual(DateTimeKind.Utc);
	}

	[TestMethod]
	public void ToUnix_InSeconds_ShouldConvert()
	{
		var dt = new DateTime(1970, 1, 1, 0, 1, 0, DateTimeKind.Utc);
		var unix = dt.ToUnix(isSeconds: true);

		unix.AssertEqual(60.0); // 1 minute = 60 seconds
	}

	[TestMethod]
	public void ToUnix_InMilliseconds_ShouldConvert()
	{
		var dt = new DateTime(1970, 1, 1, 0, 1, 0, DateTimeKind.Utc);
		var unix = dt.ToUnix(isSeconds: false);

		unix.AssertEqual(60000.0); // 1 minute = 60000 milliseconds
	}

	[TestMethod]
	public void ToUnix_WithDateTimeOffset_ShouldConvert()
	{
		var dto = new DateTimeOffset(1970, 1, 1, 0, 1, 0, TimeSpan.Zero);
		dto.ToUnix(true).AssertEqual(60.0);
	}

	[TestMethod]
	public void ToUnix_BeforeGregorianStart_ShouldThrow()
	{
		var dt = new DateTime(1969, 12, 31, 23, 59, 59, DateTimeKind.Utc);
		ThrowsExactly<ArgumentOutOfRangeException>(() => dt.ToUnix());
	}

	[TestMethod]
	public void FromUnix_LongSeconds_ShouldConvert()
	{
		var dt = 60L.FromUnix(isSeconds: true);

		dt.AssertEqual(new DateTime(1970, 1, 1, 0, 1, 0, DateTimeKind.Utc));
		dt.Kind.AssertEqual(DateTimeKind.Utc);
	}

	[TestMethod]
	public void FromUnix_LongMilliseconds_ShouldConvert()
	{
		var dt = 60000L.FromUnix(isSeconds: false);

		dt.AssertEqual(new DateTime(1970, 1, 1, 0, 1, 0, DateTimeKind.Utc));
	}

	[TestMethod]
	public void FromUnix_DoubleSeconds_ShouldConvert()
	{
		var dt = 90.5.FromUnix(isSeconds: true);

		// 90.5 seconds from epoch
		dt.Year.AssertEqual(1970);
		dt.Minute.AssertEqual(1);
		dt.Second.AssertEqual(30);
	}

	[TestMethod]
	public void TryFromUnix_WithZero_ShouldReturnNull()
	{
		0L.TryFromUnix(true).AssertNull();
		0.0.TryFromUnix(true).AssertNull();
	}

	[TestMethod]
	public void TryFromUnix_WithNonZero_ShouldReturnValue()
	{
		var result = 60L.TryFromUnix(true);
		result.HasValue.AssertTrue();
		result.Value.Year.AssertEqual(1970);
	}

	[TestMethod]
	public void ToUnixMcs_ShouldConvertToMicroseconds()
	{
		var dt = TimeHelper.GregorianStart.AddSeconds(1);
		var mcs = dt.ToUnixMcs();

		mcs.AssertEqual(1000000L); // 1 second = 1,000,000 microseconds
	}

	[TestMethod]
	public void FromUnixMcs_Long_ShouldConvertFromMicroseconds()
	{
		var dt = 1000000L.FromUnixMcs();

		dt.AssertEqual(TimeHelper.GregorianStart.AddSeconds(1));
	}

	[TestMethod]
	public void FromUnixMcs_Double_ShouldConvertFromMicroseconds()
	{
		var dt = 500000.0.FromUnixMcs();

		dt.AssertEqual(TimeHelper.GregorianStart.AddMilliseconds(500));
	}

	[TestMethod]
	public void FromUnixAuto_Seconds_ShouldDetectAndConvert()
	{
		// 10 digits - seconds (e.g., 1700000000 = Nov 14, 2023)
		var ts = 1700000000L;
		var dt = ts.FromUnixAuto();

		dt.AssertEqual(ts.FromUnix(true));
		dt.Year.AssertEqual(2023);
	}

	[TestMethod]
	public void FromUnixAuto_Milliseconds_ShouldDetectAndConvert()
	{
		// 13 digits - milliseconds (e.g., 1700000000000)
		var ts = 1700000000000L;
		var dt = ts.FromUnixAuto();

		dt.AssertEqual(ts.FromUnix(false));
		dt.Year.AssertEqual(2023);
	}

	[TestMethod]
	public void FromUnixAuto_Microseconds_ShouldDetectAndConvert()
	{
		// 16 digits - microseconds (e.g., 1700000000000000)
		var ts = 1700000000000000L;
		var dt = ts.FromUnixAuto();

		dt.AssertEqual(ts.FromUnixMcs());
		dt.Year.AssertEqual(2023);
	}

	[TestMethod]
	public void FromUnixAuto_TenThousandths_ShouldDetectAndConvert()
	{
		// 14-15 digits: 1/10000 seconds (e.g., 17000000000000)
		var ts = 17000000000000L; // 14 digits
		var dt = ts.FromUnixAuto();

		// Should convert to microseconds by multiplying by 100
		dt.AssertEqual((ts * 100).FromUnixMcs());
		dt.Year.AssertEqual(2023);
	}

	[TestMethod]
	public void FromUnixAuto_BoundaryValues_ShouldDetectCorrectly()
	{
		// Just above seconds threshold (13 digits) - should be milliseconds
		var ms = 1_000_000_000_001L;
		var dtMs = ms.FromUnixAuto();
		dtMs.AssertEqual(ms.FromUnix(false));

		// 14 digits - should be 1/10000 seconds
		var tenK = 10_000_000_000_001L;
		var dtTenK = tenK.FromUnixAuto();
		dtTenK.AssertEqual((tenK * 100).FromUnixMcs());

		// Just above 1/10000 threshold (16 digits) - should be microseconds
		var mcs = 1_000_000_000_000_001L;
		var dtMcs = mcs.FromUnixAuto();
		dtMcs.AssertEqual(mcs.FromUnixMcs());
	}

	[TestMethod]
	public void UnixNow_Properties_ShouldReturnCurrentUnixTime()
	{
		var nowS = TimeHelper.UnixNowS;
		var nowMls = TimeHelper.UnixNowMls;

		// Should be positive and reasonable (after year 2020)
		(nowS > 1577836800).AssertTrue($"UnixNowS={nowS} should be >1577836800"); // Jan 1, 2020 in seconds
		(nowMls > 1577836800000).AssertTrue($"UnixNowMls={nowMls} should be >1577836800000"); // Jan 1, 2020 in milliseconds

		// Milliseconds should be ~1000x seconds
		var ratio = nowMls / nowS;
		(ratio > 900 && ratio < 1100).AssertTrue($"ratio={ratio} (nowMls/nowS) should be >900 and <1100");
	}

	[TestMethod]
	public void GetUnixDiff_ShouldReturnDifferenceFromEpoch()
	{
		var dt = new DateTime(1970, 1, 1, 1, 0, 0, DateTimeKind.Utc);
		var diff = dt.GetUnixDiff();

		diff.AssertEqual(TimeSpan.FromHours(1));
	}

	#endregion

	#region Type Checking

	[TestMethod]
	public void IsDateTime_WithDateTimeTypes_ShouldReturnTrue()
	{
		typeof(DateTime).IsDateTime().AssertTrue();
		typeof(DateTimeOffset).IsDateTime().AssertTrue();
	}

	[TestMethod]
	public void IsDateTime_WithOtherTypes_ShouldReturnFalse()
	{
		typeof(TimeSpan).IsDateTime().AssertFalse();
		typeof(int).IsDateTime().AssertFalse();
		typeof(string).IsDateTime().AssertFalse();
	}

	[TestMethod]
	public void IsDateTime_WithNull_ShouldThrow()
	{
		ThrowsExactly<ArgumentNullException>(() =>
			((Type)null).IsDateTime());
	}

	[TestMethod]
	public void IsDateOrTime_WithDateTimeTypes_ShouldReturnTrue()
	{
		typeof(DateTime).IsDateOrTime().AssertTrue();
		typeof(DateTimeOffset).IsDateOrTime().AssertTrue();
		typeof(TimeSpan).IsDateOrTime().AssertTrue();
	}

	[TestMethod]
	public void IsDateOrTime_WithOtherTypes_ShouldReturnFalse()
	{
		typeof(int).IsDateOrTime().AssertFalse();
		typeof(string).IsDateOrTime().AssertFalse();
	}

	#endregion

	#region Weekday/Weekend

	[TestMethod]
	public void IsWeekday_DateTime_ShouldIdentifyWeekdays()
	{
		new DateTime(2024, 1, 15).IsWeekday().AssertTrue(); // Monday
		new DateTime(2024, 1, 16).IsWeekday().AssertTrue(); // Tuesday
		new DateTime(2024, 1, 17).IsWeekday().AssertTrue(); // Wednesday
		new DateTime(2024, 1, 18).IsWeekday().AssertTrue(); // Thursday
		new DateTime(2024, 1, 19).IsWeekday().AssertTrue(); // Friday
		new DateTime(2024, 1, 20).IsWeekday().AssertFalse(); // Saturday
		new DateTime(2024, 1, 21).IsWeekday().AssertFalse(); // Sunday
	}

	[TestMethod]
	public void IsWeekend_DateTime_ShouldIdentifyWeekends()
	{
		new DateTime(2024, 1, 20).IsWeekend().AssertTrue(); // Saturday
		new DateTime(2024, 1, 21).IsWeekend().AssertTrue(); // Sunday
		new DateTime(2024, 1, 15).IsWeekend().AssertFalse(); // Monday
	}

	[TestMethod]
	public void IsWeekday_DateTimeOffset_ShouldIdentifyWeekdays()
	{
		new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero).IsWeekday().AssertTrue(); // Monday
		new DateTimeOffset(2024, 1, 20, 12, 0, 0, TimeSpan.Zero).IsWeekday().AssertFalse(); // Saturday
	}

	[TestMethod]
	public void IsWeekday_DayOfWeek_ShouldIdentifyWeekdays()
	{
		DayOfWeek.Monday.IsWeekday().AssertTrue();
		DayOfWeek.Tuesday.IsWeekday().AssertTrue();
		DayOfWeek.Wednesday.IsWeekday().AssertTrue();
		DayOfWeek.Thursday.IsWeekday().AssertTrue();
		DayOfWeek.Friday.IsWeekday().AssertTrue();
		DayOfWeek.Saturday.IsWeekday().AssertFalse();
		DayOfWeek.Sunday.IsWeekday().AssertFalse();
	}

	[TestMethod]
	public void IsWeekend_DayOfWeek_ShouldIdentifyWeekends()
	{
		DayOfWeek.Saturday.IsWeekend().AssertTrue();
		DayOfWeek.Sunday.IsWeekend().AssertTrue();
		DayOfWeek.Monday.IsWeekend().AssertFalse();
	}

	#endregion

	#region Lunar and Julian

	[TestMethod]
	public void ToJulianDate_ShouldConvertCorrectly()
	{
		// Jan 1, 2000 at noon should be approximately JD 2451545
		var dt = new DateTime(2000, 1, 1, 12, 0, 0);
		var jd = dt.ToJulianDate();

		// Should be close to 2451545
		(jd > 2451544 && jd < 2451546).AssertTrue($"jd={jd} should be >2451544 and <2451546");
	}

	#endregion

	#region DateTime To() TimeZone Conversion

	[TestMethod]
	public void To_WithSourceAndDestination_ShouldConvert()
	{
		var dt = new DateTime(2024, 6, 15, 12, 0, 0);
		var result = dt.To(TimeZoneInfo.Utc, TimeZoneInfo.Local);

		// Should convert from UTC to Local
		result.Kind.AssertEqual(DateTimeKind.Local);
	}

	[TestMethod]
	public void To_WithUtcKind_ShouldUseUtcAsSource()
	{
		var dt = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
		var result = dt.To(destination: TimeZoneInfo.Local);

		result.Kind.AssertEqual(DateTimeKind.Local);
	}

	[TestMethod]
	public void To_WithLocalKind_ShouldUseLocalAsSource()
	{
		var dt = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Local);
		var result = dt.To(destination: TimeZoneInfo.Utc);

		result.Kind.AssertEqual(DateTimeKind.Utc);
	}

	#endregion

	#region TimeZone Properties

	[TestMethod]
	public void TimeZoneProperties_ShouldNotBeNull()
	{
		TimeHelper.Est.AssertNotNull();
		TimeHelper.Cst.AssertNotNull();
		TimeHelper.Moscow.AssertNotNull();
		TimeHelper.Gmt.AssertNotNull();
		TimeHelper.Fle.AssertNotNull();
		TimeHelper.China.AssertNotNull();
		TimeHelper.Korea.AssertNotNull();
		TimeHelper.Tokyo.AssertNotNull();
		TimeHelper.Tunisia.AssertNotNull();
	}

	[TestMethod]
	public void ApplyChina_ShouldApplyChinaTimeZone()
	{
		var dt = new DateTime(2024, 6, 15, 12, 0, 0);
		var dto = dt.ApplyChina();

		// China is UTC+8
		dto.Offset.Hours.AssertEqual(8);
	}

	[TestMethod]
	public void ApplyMoscow_ShouldApplyMoscowTimeZone()
	{
		var dt = new DateTime(2024, 6, 15, 12, 0, 0);
		var dto = dt.ApplyMoscow();

		// Moscow is typically UTC+3
		var expectedOffset = TimeHelper.Moscow.GetUtcOffset(dt);
		dto.Offset.AssertEqual(expectedOffset);
	}

	[TestMethod]
	public void ConvertToChina_ShouldConvertToChinaTimeZone()
	{
		var dto = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
		var result = dto.ConvertToChina();

		// Should convert time AND offset
		result.Offset.Hours.AssertEqual(8);
		result.Hour.AssertEqual(20); // 12 + 8 = 20
	}

	#endregion
}

