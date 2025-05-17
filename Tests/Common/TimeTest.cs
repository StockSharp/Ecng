namespace Ecng.Tests.Common;

[TestClass]
public class TimeTest
{
	[TestMethod]
	public void Microseconds()
	{
		for (var i = 0; i < 10000000; i++)
		{
			const int microseconds = 456;

			var ts = TimeSpan.Zero;
			ts.AddMicroseconds(microseconds).GetMicroseconds().AssertEqual(microseconds);

			var dt = DateTime.Now;
			var res = microseconds + dt.GetMicroseconds();

			if (res >= 1000)
				res -= 1000;

			dt.AddMicroseconds(microseconds).GetMicroseconds().AssertEqual(res);

			dt = TimeHelper.Now;
			res = microseconds + dt.GetMicroseconds();

			if (res >= 1000)
				res -= 1000;

			dt.AddMicroseconds(microseconds).GetMicroseconds().AssertEqual(res);

			dt = DateTime.MaxValue - TimeSpan.FromDays(1);
			res = microseconds + dt.GetMicroseconds();

			if (res >= 1000)
				res -= 1000;

			dt.AddMicroseconds(microseconds).GetMicroseconds().AssertEqual(res);
		}
	}

	[TestMethod]
	public void Nanoseconds()
	{
		for (var i = 0; i < 10000000; i++)
		{
			var nanoseconds = RandomGen.GetInt(0, 999);
			var roundNs = (nanoseconds / 100) * 100;

			var ts = TimeSpan.Zero;
			ts.AddNanoseconds(nanoseconds).ToNanoseconds().AssertEqual(roundNs);

			var dt = DateTime.Now;
			var ns = dt.GetNanoseconds();
			dt = dt.AddNanoseconds(nanoseconds);
			ns += roundNs;
			if (ns >= 1000)
				ns -= 1000;
			dt.GetNanoseconds().AssertEqual(ns);

			dt = DateTime.MaxValue - TimeSpan.FromDays(1 + RandomGen.GetDouble());
			ns = dt.GetNanoseconds();
			dt = dt.AddNanoseconds(nanoseconds);
			ns += roundNs;
			if (ns >= 1000)
				ns -= 1000;
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
		((dt - res).TotalMilliseconds < 1).AssertTrue();
	}

	[TestMethod]
	public void UnixTimeOutOfRange()
	{
		var dt = DateTime.MinValue;
		dt = dt.UtcKind();
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => dt.ToUnix());
	}
}