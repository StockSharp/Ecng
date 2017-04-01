namespace Ecng.Test.Common
{
	using System;

	using Ecng.Common;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

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
				const int nanoseconds = 456;

				var ts = TimeSpan.Zero;
				ts.AddNanoseconds(nanoseconds).GetNanoseconds().AssertEqual(400);

				var dt = DateTime.Now;
				dt.AddNanoseconds(nanoseconds).GetNanoseconds().AssertEqual(dt.GetNanoseconds() + 400);

				dt = TimeHelper.Now;
				dt.AddNanoseconds(nanoseconds).GetNanoseconds().AssertEqual(dt.GetNanoseconds() + 400);

				dt = DateTime.MaxValue - TimeSpan.FromDays(1);
				dt.AddNanoseconds(nanoseconds).GetNanoseconds().AssertEqual(dt.GetNanoseconds() + 400);
			}
		}
	}
}