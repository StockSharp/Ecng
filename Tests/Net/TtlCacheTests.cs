namespace Ecng.Tests.Net;

using Ecng.Net;

[TestClass]
public class TtlCacheTests : BaseTestClass
{
	// A clock the test moves itself, so an answer can be aged past its lifetime without waiting it out.
	private sealed class TestClock : TimeProvider
	{
		private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

		public override DateTimeOffset GetUtcNow() => _now;

		public void Advance(TimeSpan by) => _now += by;
	}

	private static readonly TimeSpan _ttl = TimeSpan.FromSeconds(10);

	private static ValueTask<string> Answer(string key, CancellationToken ct) => new("answer:" + key);

	private static TtlCache<string, string> Create(TestClock time)
		=> new(_ttl, time, StringComparer.OrdinalIgnoreCase);

	[TestMethod]
	public void TtlHasToBePositiveAndAClockIsRequired()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TtlCache<string, string>(TimeSpan.Zero, new TestClock()));
		Assert.ThrowsExactly<ArgumentNullException>(() => new TtlCache<string, string>(_ttl, null));
	}

	[TestMethod]
	public async Task ResolverIsRequired()
	{
		var cache = Create(new TestClock());

		await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => cache.GetAsync("A", null, default).AsTask());
	}

	[TestMethod]
	public async Task WhatIsAskedOnceIsServedUntilItGoesStale()
	{
		var time = new TestClock();
		var cache = Create(time);
		var asked = 0;

		ValueTask<string> Count(string key, CancellationToken ct)
		{
			asked++;
			return new("answer:" + key);
		}

		(await cache.GetAsync("A", Count, default)).AssertEqual("answer:A");
		(await cache.GetAsync("A", Count, default)).AssertEqual("answer:A");

		asked.AssertEqual(1);

		time.Advance(_ttl + TimeSpan.FromSeconds(1));

		await cache.GetAsync("A", Count, default);
		asked.AssertEqual(2);
	}

	[TestMethod]
	public async Task KeysAreComparedTheWayTheCallerSaid()
	{
		var cache = Create(new TestClock());
		var asked = 0;

		ValueTask<string> Count(string key, CancellationToken ct)
		{
			asked++;
			return new("answer");
		}

		await cache.GetAsync("acct", Count, default);
		await cache.GetAsync("ACCT", Count, default);

		asked.AssertEqual(1);
	}

	[TestMethod]
	public async Task WhatHasGoneStaleIsDroppedRatherThanKept()
	{
		var time = new TestClock();
		var cache = Create(time);

		foreach (var key in new[] { "A", "B", "C" })
			await cache.GetAsync(key, Answer, default);

		cache.Count.AssertEqual(3);
		cache.HeldCount.AssertEqual(3);

		time.Advance(_ttl + TimeSpan.FromSeconds(1));

		cache.Count.AssertEqual(0);
		cache.HeldCount.AssertEqual(3, "nothing has asked yet, so nothing has been swept");

		await cache.GetAsync("D", Answer, default);

		cache.HeldCount.AssertEqual(1, "the ask that reached the source swept what had gone stale");
	}

	[TestMethod]
	public async Task AnAnswerJustRefreshedSurvivesTheSweepItTriggers()
	{
		var time = new TestClock();
		var cache = Create(time);

		await cache.GetAsync("old", Answer, default);

		time.Advance(_ttl + TimeSpan.FromSeconds(1));

		await cache.GetAsync("old", Answer, default);

		cache.HeldCount.AssertEqual(1);
		cache.Count.AssertEqual(1);
	}

	[TestMethod]
	public async Task SweepingHappensAtMostOncePerTtl()
	{
		var time = new TestClock();
		var cache = Create(time);

		await cache.GetAsync("A", Answer, default);

		time.Advance(_ttl + TimeSpan.FromSeconds(1));

		await cache.GetAsync("B", Answer, default);
		cache.HeldCount.AssertEqual(1);

		await cache.GetAsync("C", Answer, default);
		cache.HeldCount.AssertEqual(2, "a walk of the whole cache per ask is what the interval is there to avoid");
	}

	[TestMethod]
	public void WhatIsPutInIsServedUntilItGoesStale()
	{
		var time = new TestClock();
		var cache = Create(time);

		cache.TryGet("A", out _).AssertFalse();

		cache.Set("A", "answer");

		cache.TryGet("A", out var held).AssertTrue();
		held.AssertEqual("answer");

		time.Advance(_ttl + TimeSpan.FromSeconds(1));

		cache.TryGet("A", out _).AssertFalse("what has gone stale is not served");
		cache.HeldCount.AssertEqual(0, "and being asked for is what takes it away, without waiting for a sweep");
	}

	[TestMethod]
	public void EveryKeyTheCallerNamesIsDropped()
	{
		var cache = Create(new TestClock());

		cache.Set("keep", "a");
		cache.Set("drop-1", "b");
		cache.Set("drop-2", "c");

		cache.RemoveWhere(key => key.StartsWith("drop")).AssertEqual(2);

		cache.TryGet("keep", out _).AssertTrue();
		cache.HeldCount.AssertEqual(1);
	}

	[TestMethod]
	public void WhatToDropHasToBeNamed()
	{
		var cache = Create(new TestClock());

		Assert.ThrowsExactly<ArgumentNullException>(() => cache.RemoveWhere(null));
	}

	[TestMethod]
	public async Task WhatIsKnownToHaveChangedCanBeDropped()
	{
		var cache = Create(new TestClock());

		await cache.GetAsync("A", Answer, default);

		cache.Remove("A").AssertTrue();
		cache.HeldCount.AssertEqual(0);
		cache.Remove("A").AssertFalse();
	}

	[TestMethod]
	public async Task ClearingForgetsEverything()
	{
		var cache = Create(new TestClock());

		await cache.GetAsync("A", Answer, default);
		await cache.GetAsync("B", Answer, default);

		cache.Clear();

		cache.HeldCount.AssertEqual(0);
		cache.Count.AssertEqual(0);
	}
}
