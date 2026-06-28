namespace Ecng.Tests.Net;

using System;

using Ecng.Net;

[TestClass]
public class SlidingWindowRateLimiterTests : BaseTestClass
{
	[TestMethod]
	public void BelowThreshold_NotLimited()
	{
		var rl = new SlidingWindowRateLimiter<string>(maxAttempts: 3, window: TimeSpan.FromMinutes(1));
		rl.Record("a");
		rl.Record("a");
		rl.IsLimited("a").AssertFalse();
		rl.IsLimited("b").AssertFalse();
	}

	[TestMethod]
	public void AtThreshold_Limited()
	{
		var rl = new SlidingWindowRateLimiter<string>(maxAttempts: 3, window: TimeSpan.FromMinutes(1));
		rl.Record("a");
		rl.Record("a");
		rl.Record("a");
		rl.IsLimited("a").AssertTrue();
	}

	[TestMethod]
	public void EventsOutsideWindow_Pruned_AndTrackerEvicted()
	{
		var now = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var rl = new SlidingWindowRateLimiter<string>(maxAttempts: 2, window: TimeSpan.FromSeconds(10), now: () => now);

		rl.Record("a");
		rl.Record("a");
		rl.IsLimited("a").AssertTrue();

		now = now.AddSeconds(11); // both events fall outside the trailing window

		rl.IsLimited("a").AssertFalse();
		rl.TrackedCount.AssertEqual(0); // empty tracker is evicted on read
	}

	[TestMethod]
	public void Reset_ClearsKey()
	{
		var rl = new SlidingWindowRateLimiter<string>(maxAttempts: 1, window: TimeSpan.FromMinutes(1));
		rl.Record("a");
		rl.IsLimited("a").AssertTrue();

		rl.Reset("a");

		rl.IsLimited("a").AssertFalse();
		rl.TrackedCount.AssertEqual(0);
	}
}
