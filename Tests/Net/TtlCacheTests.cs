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

	// The oldest an answer may be, however often it is asked for.
	private static readonly TimeSpan _maxAge = TimeSpan.FromSeconds(60);

	private static TtlCache<string, string> Create(TestClock time)
		=> new(_ttl, _maxAge, time, StringComparer.OrdinalIgnoreCase);

	/// <summary>Asked for again, an answer is kept longer, so a busy key stops reaching the source.</summary>
	[TestMethod]
	public async Task AnAnswerAskedForAgainIsHeldLonger()
	{
		var time = new TestClock();
		var cache = Create(time);
		var asked = 0;

		ValueTask<string> Count(string key, CancellationToken ct)
		{
			asked++;
			return new("answer:" + key);
		}

		await cache.GetAsync("A", Count, CancellationToken);

		// Asked for every eight seconds, so it never sits unasked for the ten it is kept.
		for (var i = 0; i < 5; i++)
		{
			time.Advance(TimeSpan.FromSeconds(8));
			await cache.GetAsync("A", Count, CancellationToken);
		}

		AreEqual(1, asked);
	}

	/// <summary>However often it is asked for, it is fetched again once it is old enough.</summary>
	/// <remarks>
	/// Without this a busy key would never see a change at the source: each ask would push its lifetime out,
	/// and the more popular the answer, the longer it would go on being wrong.
	/// </remarks>
	[TestMethod]
	public async Task AnAnswerIsFetchedAgainOnceItIsOldEnough()
	{
		var time = new TestClock();
		var cache = Create(time);
		var asked = 0;

		ValueTask<string> Count(string key, CancellationToken ct)
		{
			asked++;
			return new("answer:" + key);
		}

		await cache.GetAsync("A", Count, CancellationToken);

		for (var i = 0; i < 10; i++)
		{
			time.Advance(TimeSpan.FromSeconds(8));
			await cache.GetAsync("A", Count, CancellationToken);
		}

		// Eighty seconds of being asked for, against a sixty-second oldest it may be.
		AreEqual(2, asked);
	}

	/// <summary>An answer nobody asks for still goes when its time is up.</summary>
	[TestMethod]
	public async Task AnAnswerNobodyAsksForGoesOnTime()
	{
		var time = new TestClock();
		var cache = Create(time);

		await cache.GetAsync("A", Answer, CancellationToken);

		time.Advance(_ttl + TimeSpan.FromSeconds(1));

		IsFalse(cache.TryGet("A", out _));
	}

	[TestMethod]
	public void TtlHasToBePositiveAndAClockIsRequired()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TtlCache<string, string>(TimeSpan.Zero, _maxAge, new TestClock()));
		Assert.ThrowsExactly<ArgumentNullException>(() => new TtlCache<string, string>(_ttl, _maxAge, null));
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

	// --- a lifetime the caller decides per answer ---

	[TestMethod]
	public void AnAnswerCanBeHeldForALifetimeOfItsOwn()
	{
		var time = new TestClock();
		var cache = Create(time);

		// A site whose cache lifetime is a setting reads it as it stores, so a change to the setting applies
		// to what is stored next rather than at the next restart.
		cache.Set("A", "one", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
		cache.Set("B", "two");

		time.Advance(TimeSpan.FromSeconds(20));

		IsTrue(cache.TryGet("A", out var a));
		AreEqual("one", a);

		// The one stored for the cache's own lifetime went stale on schedule.
		IsFalse(cache.TryGet("B", out _));

		time.Advance(TimeSpan.FromSeconds(11));

		IsFalse(cache.TryGet("A", out _));
	}

	[TestMethod]
	public async Task AResolvedAnswerCanBeHeldForALifetimeOfItsOwn()
	{
		var time = new TestClock();
		var cache = Create(time);
		var asked = 0;

		ValueTask<string> Count(string key, CancellationToken ct)
		{
			asked++;
			return Answer(key, ct);
		}

		AreEqual("answer:A", await cache.GetAsync("A", Count, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), CancellationToken));

		time.Advance(TimeSpan.FromSeconds(20));

		AreEqual("answer:A", await cache.GetAsync("A", Count, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), CancellationToken));
		AreEqual(1, asked);

		time.Advance(TimeSpan.FromSeconds(11));

		AreEqual("answer:A", await cache.GetAsync("A", Count, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), CancellationToken));
		AreEqual(2, asked);
	}

	[TestMethod]
	public void ALifetimeOfItsOwnHasToBePositive()
	{
		var cache = Create(new TestClock());

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => cache.Set("A", "one", TimeSpan.Zero, _maxAge));
	}

	// --- dropping and reading by what is held, not only by key ---

	[TestMethod]
	public void EveryAnswerTheCallerRecognisesIsDropped()
	{
		var time = new TestClock();
		var cache = Create(time);

		cache.Set("A", "keep");
		cache.Set("B", "drop me");
		cache.Set("C", "drop me too");

		// What has to go is often known by what was cached, not by the key it was cached under: a page cache
		// drops whatever holds the entity that just changed.
		AreEqual(2, cache.RemoveWhere((key, value) => value.StartsWith("drop")));

		IsTrue(cache.TryGet("A", out _));
		IsFalse(cache.TryGet("B", out _));
		IsFalse(cache.TryGet("C", out _));
	}

	[TestMethod]
	public void WhatIsHeldCanBeRead()
	{
		var time = new TestClock();
		var cache = Create(time);

		cache.Set("A", "one");
		cache.Set("B", "two", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

		CollectionAssert.AreEquivalent(new[] { "one", "two" }, cache.Values.ToArray());

		time.Advance(TimeSpan.FromSeconds(11));

		// What has gone stale is not held any more, whether or not the sweep has come round to it.
		CollectionAssert.AreEquivalent(new[] { "two" }, cache.Values.ToArray());
	}

	[TestMethod]
	public void WhatToDropByValueHasToBeNamed()
	{
		var cache = Create(new TestClock());

		Assert.ThrowsExactly<ArgumentNullException>(() => cache.RemoveWhere((Func<string, string, bool>)null));
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

		Assert.ThrowsExactly<ArgumentNullException>(() => cache.RemoveWhere((Func<string, bool>)null));
	}

	/// <summary>
	/// A burst on a key nobody has asked about yet reaches the source once: the first caller asks, the rest
	/// wait for that answer. (Was: each of them asked, so a cache going cold under load cost as many
	/// resolutions as there were callers at that moment.)
	/// </summary>
	[TestMethod]
	public async Task ABurstOnAColdKeyReachesTheSourceOnce()
	{
		var cache = Create(new TestClock());
		var asked = 0;
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		async ValueTask<string> Slow(string key, CancellationToken ct)
		{
			Interlocked.Increment(ref asked);
			await gate.Task;
			return "answer:" + key;
		}

		var callers = Enumerable.Range(0, 10).Select(_ => cache.GetAsync("A", Slow, default).AsTask()).ToArray();

		gate.SetResult();

		foreach (var answer in await Task.WhenAll(callers))
			answer.AssertEqual("answer:A");

		asked.AssertEqual(1);
	}

	/// <summary>
	/// A failure is not held: those waiting hear it, and the next ask starts afresh rather than being served
	/// the failure for the rest of the lifetime.
	/// </summary>
	[TestMethod]
	public async Task AFailedAskIsNotHeld()
	{
		var cache = Create(new TestClock());
		var asked = 0;

		ValueTask<string> Failing(string key, CancellationToken ct)
		{
			Interlocked.Increment(ref asked);
			throw new InvalidOperationException("source is down");
		}

		await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await cache.GetAsync("A", Failing, default));
		await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await cache.GetAsync("A", Failing, default));

		asked.AssertEqual(2);
		cache.HeldCount.AssertEqual(0);
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
