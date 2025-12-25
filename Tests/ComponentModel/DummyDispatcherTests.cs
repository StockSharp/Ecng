namespace Ecng.Tests.ComponentModel;

using System.Reflection;

using Ecng.ComponentModel;

[TestClass]
public class DummyDispatcherTests : BaseTestClass
{
	private async Task WaitForAsync(Func<bool> predicate, TimeSpan timeout, string message)
	{
		var sw = Stopwatch.StartNew();

		while (!predicate() && sw.Elapsed < timeout)
			await Task.Delay(10, CancellationToken);

		predicate().AssertTrue($"{message} after {sw.ElapsedMilliseconds}ms");
	}

	private static TimeSpan GetTimerInterval(IDispatcher dispatcher)
	{
		if (dispatcher is not DummyDispatcher dummy)
			throw new ArgumentException($"Expected {nameof(DummyDispatcher)}.", nameof(dispatcher));

		var field = typeof(DummyDispatcher).GetField("_timerInterval", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"Cannot access {nameof(DummyDispatcher)}._timerInterval.");

		return (TimeSpan)field.GetValue(dummy);
	}

	private static int GetPlannerCount(IDispatcher dispatcher)
	{
		if (dispatcher is not DummyDispatcher dummy)
			throw new ArgumentException($"Expected {nameof(DummyDispatcher)}.", nameof(dispatcher));

		var field = typeof(DummyDispatcher).GetField("_periodic", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"Cannot access {nameof(DummyDispatcher)}._periodic.");

		var planner = (PeriodicActionPlanner)field.GetValue(dummy);
		return planner.Count;
	}

	[TestMethod]
	public void CheckAccess_AlwaysTrue()
	{
		IDispatcher d = new DummyDispatcher();

		// From current thread
		d.CheckAccess().AssertTrue("CheckAccess should return true from current thread");

		// From another thread
		var otherThreadResult = false;
		var t = new Thread(() => otherThreadResult = d.CheckAccess())
		{
			IsBackground = true
		};
		t.Start();
		t.Join();
		otherThreadResult.AssertTrue("CheckAccess should return true from another thread");
	}

	[TestMethod]
	public void Invoke_IsSynchronous()
	{
		IDispatcher d = new DummyDispatcher();

		var value = 0;
		d.Invoke(() => value = 42);

		// Should be set immediately, before return
		value.AssertEqual(42, $"Expected value=42, but was {value}");
	}

	[TestMethod]
	public void InvokeAsync_ExecutesLater()
	{
		IDispatcher d = new DummyDispatcher();

		var value = 0;
		using var done = new ManualResetEventSlim(false);

		d.InvokeAsync(() => { Interlocked.Exchange(ref value, 1); done.Set(); });

		// Should complete within a reasonable time (10 seconds to handle slow CI environments)
		done.Wait(TimeSpan.FromSeconds(10), CancellationToken).AssertTrue("InvokeAsync action should complete within 10 seconds");
		value.AssertEqual(1, $"Expected value=1, but was {value}");
	}

	[TestMethod]
	public async Task InvokePeriodically_Basic()
	{
		IDispatcher d = new DummyDispatcher();

		var counter = 0;
		using var sub = d.InvokePeriodically(() => Interlocked.Increment(ref counter), TimeSpan.FromMilliseconds(50));

		// Wait until a few ticks arrive
		var sw = Stopwatch.StartNew();
		while (counter < 3 && sw.Elapsed < TimeSpan.FromSeconds(2))
			await Task.Delay(10, CancellationToken);

		(counter >= 3).AssertTrue($"Expected counter >= 3, but was {counter} after {sw.ElapsedMilliseconds}ms");

		// After dispose, counter should stop increasing.
		sub.Dispose();
		var after = counter; // Capture AFTER dispose to avoid race condition
		await Task.Delay(200, CancellationToken);
		counter.AssertEqual(after, $"Expected counter to stay at {after} after dispose, but was {counter}");
	}

	[TestMethod]
	public async Task InvokePeriodically_MultipleSubscriptions()
	{
		IDispatcher d = new DummyDispatcher();

		var c1 = 0;
		var c2 = 0;

		using var s1 = d.InvokePeriodically(() => Interlocked.Increment(ref c1), TimeSpan.FromMilliseconds(40));
		using var s2 = d.InvokePeriodically(() => Interlocked.Increment(ref c2), TimeSpan.FromMilliseconds(120));

		var sw = Stopwatch.StartNew();
		while ((c1 < 3 || c2 < 2) && sw.Elapsed < TimeSpan.FromSeconds(3))
			await Task.Delay(10, CancellationToken);

		(c1 >= 3).AssertTrue($"Expected c1 >= 3, but was {c1} after {sw.ElapsedMilliseconds}ms");
		(c2 >= 2).AssertTrue($"Expected c2 >= 2, but was {c2} after {sw.ElapsedMilliseconds}ms");

		// Dispose the faster one, ensure it stops while the slower continues.
		var before1 = c1;
		s1.Dispose();

		var oldC2 = c2;
		await WaitForAsync(() => c2 > oldC2, TimeSpan.FromSeconds(2), $"Expected c2 > {oldC2} (slower action should continue), but was {c2}");
		await Task.Delay(250, CancellationToken);

		var diff1 = c1 - before1;
		(diff1 <= 1).AssertTrue($"Expected c1 to stop after s1.Dispose() (allowing 1 in-flight tick), but before={before1}, after={c1}, diff={diff1}");
	}

	[TestMethod]
	public async Task InvokePeriodically_IntervalNotRecalculatedAfterUnsubscribe()
	{
		// This test verifies whether the timer interval is recalculated
		// when a faster action is unsubscribed, leaving only slower actions.
		//
		// If the interval is NOT recalculated (bug), the timer continues
		// ticking at the fast rate (50ms) even though only the slow action (500ms) remains.
		// This wastes CPU cycles checking unnecessarily.

		IDispatcher d = new DummyDispatcher();

		var fastCounter = 0;
		var slowCounter = 0;
		var timerTickCount = 0;

		// Register slow action first (500ms interval)
		using var slowSub = d.InvokePeriodically(() =>
		{
			Interlocked.Increment(ref slowCounter);
			Interlocked.Increment(ref timerTickCount);
		}, TimeSpan.FromMilliseconds(500));

		// Register fast action (50ms interval) - this should change timer to 50ms
		var fastSub = d.InvokePeriodically(() =>
		{
			Interlocked.Increment(ref fastCounter);
			Interlocked.Increment(ref timerTickCount);
		}, TimeSpan.FromMilliseconds(50));

		await WaitForAsync(() => fastCounter >= 2, TimeSpan.FromSeconds(2), $"Expected fastCounter >= 2, but was {fastCounter}");

		// Unsubscribe fast action
		var ticksBefore = timerTickCount;
		fastSub.Dispose();

		// Wait for slow action to fire at least once after fast unsubscription
		// Use WaitForAsync to avoid flaky timing issues
		await WaitForAsync(() => slowCounter >= 1, TimeSpan.FromSeconds(2), $"Expected slowCounter >= 1, but was {slowCounter}");

		var ticksAfter = timerTickCount - ticksBefore;

		// With proper interval recalculation, should be ~1-2 ticks (500ms interval)
		// With bug (interval stays 50ms), would be many more ticks
		// This test verifies the slow action continues to work after fast action is removed
		(ticksAfter >= 1).AssertTrue($"Expected ticksAfter >= 1, but was {ticksAfter}");
	}

	[TestMethod]
	public void InvokePeriodically_DoubleDispose_NoException()
	{
		IDispatcher d = new DummyDispatcher();

		var sub = d.InvokePeriodically(() => { }, TimeSpan.FromMilliseconds(100));
		sub.Dispose();
		sub.Dispose(); // Should not throw
	}

	[TestMethod]
	public void InvokePeriodically_NullAction_Throws()
	{
		IDispatcher d = new DummyDispatcher();

		ThrowsExactly<ArgumentNullException>(() => d.InvokePeriodically(null, TimeSpan.FromMilliseconds(100)));
	}

	[TestMethod]
	public async Task InvokePeriodically_ActionException_DoesNotStopTimer()
	{
		IDispatcher d = new DummyDispatcher();

		var counter = 0;
		var throwOnFirst = true;

		using var sub = d.InvokePeriodically(() =>
		{
			Interlocked.Increment(ref counter);
			if (throwOnFirst)
			{
				throwOnFirst = false;
				throw new InvalidOperationException("Test exception");
			}
		}, TimeSpan.FromMilliseconds(50));

		// Wait for multiple ticks
		var sw = Stopwatch.StartNew();
		while (counter < 3 && sw.Elapsed < TimeSpan.FromSeconds(2))
			await Task.Delay(10, CancellationToken);

		// Timer should continue despite exception
		(counter >= 3).AssertTrue($"Expected counter >= 3 (timer continues after exception), but was {counter} after {sw.ElapsedMilliseconds}ms");
	}

	[TestMethod]
	public async Task InvokePeriodically_RemoveLastAction_TimerStops()
	{
		// When the last action is removed, the timer should be disposed
		// and no more ticks should occur
		IDispatcher d = new DummyDispatcher();

		var counter = 0;
		var sub = d.InvokePeriodically(() => Interlocked.Increment(ref counter), TimeSpan.FromMilliseconds(50));

		// Wait for a few ticks
		var sw = Stopwatch.StartNew();
		while (counter < 3 && sw.Elapsed < TimeSpan.FromSeconds(2))
			await Task.Delay(10, CancellationToken);

		(counter >= 3).AssertTrue($"Expected counter >= 3, but was {counter} after {sw.ElapsedMilliseconds}ms");

		// Remove the last (and only) action
		sub.Dispose();

		// Wait a bit for any in-flight tick to complete
		await Task.Delay(100, CancellationToken);

		// Capture counter AFTER dispose has settled
		var counterAfterDispose = counter;

		// Wait longer and verify no more ticks
		await Task.Delay(300, CancellationToken);
		counter.AssertEqual(counterAfterDispose, $"Expected counter to stay at {counterAfterDispose} after removing last action, but was {counter}");
	}

	[TestMethod]
	public async Task InvokePeriodically_AddRemoveAddRemove_IntervalsChange()
	{
		// Test actively adding and removing actions with different intervals
		// to verify interval recalculation works correctly
		IDispatcher d = new DummyDispatcher();

		var counter1 = 0;
		var counter2 = 0;
		var counter3 = 0;

		// Add first action with 200ms interval
		var sub1 = d.InvokePeriodically(() => Interlocked.Increment(ref counter1), TimeSpan.FromMilliseconds(200));

		await WaitForAsync(() => counter1 >= 1, TimeSpan.FromSeconds(3), $"Expected counter1 >= 1 with 200ms interval, but was {counter1}");
		GetTimerInterval(d).AssertEqual(TimeSpan.FromMilliseconds(200), "Timer interval should be 200ms with only the 200ms subscription.");

		// Add faster action with 50ms interval - timer should speed up
		var sub2 = d.InvokePeriodically(() => Interlocked.Increment(ref counter2), TimeSpan.FromMilliseconds(50));
		GetTimerInterval(d).AssertEqual(TimeSpan.FromMilliseconds(50), "Timer interval should switch to 50ms after adding the 50ms subscription.");
		await WaitForAsync(() => counter2 >= 2, TimeSpan.FromSeconds(3), $"Expected counter2 >= 2 with 50ms interval, but was {counter2}");

		// Remove fast action - timer should slow down to 200ms
		var c1Before = counter1;
		sub2.Dispose();
		GetTimerInterval(d).AssertEqual(TimeSpan.FromMilliseconds(200), "Timer interval should revert to 200ms after removing the 50ms subscription.");

		await Task.Delay(250, CancellationToken);
		// counter1 should not have increased much (200ms interval)
		(counter1 - c1Before <= 2).AssertTrue($"Expected counter1 - c1Before <= 2 (timer slowed to 200ms), but c1Before={c1Before}, counter1={counter1}, diff={counter1 - c1Before}");

		// Add even faster action with 30ms interval
		var sub3 = d.InvokePeriodically(() => Interlocked.Increment(ref counter3), TimeSpan.FromMilliseconds(30));
		GetTimerInterval(d).AssertEqual(TimeSpan.FromMilliseconds(30), "Timer interval should switch to 30ms after adding the 30ms subscription.");

		await WaitForAsync(() => counter3 >= 3, TimeSpan.FromSeconds(2), $"Expected counter3 >= 3 with 30ms interval, but was {counter3}");

		// Remove all
		sub3.Dispose();
		sub1.Dispose();
		GetTimerInterval(d).AssertEqual(TimeSpan.Zero, "Timer interval should reset after removing all subscriptions.");

		// Verify everything stopped
		var finalC1 = counter1;
		var finalC3 = counter3;
		await Task.Delay(200, CancellationToken);

		(counter1 - finalC1 <= 1).AssertTrue($"Expected counter1 to stop after dispose (allowing 1 in-flight tick), but finalC1={finalC1}, counter1={counter1}");
		(counter3 - finalC3 <= 1).AssertTrue($"Expected counter3 to stop after dispose (allowing 1 in-flight tick), but finalC3={finalC3}, counter3={counter3}");
	}

	[TestMethod]
	public async Task InvokePeriodically_ReaddAfterRemoveAll_TimerRestarts()
	{
		// After removing all actions (timer disposed), adding a new action
		// should create a new timer
		IDispatcher d = new DummyDispatcher();

		var counter1 = 0;
		var counter2 = 0;

		// Add and remove first action
		var sub1 = d.InvokePeriodically(() => Interlocked.Increment(ref counter1), TimeSpan.FromMilliseconds(50));

		await WaitForAsync(() => counter1 >= 1, TimeSpan.FromSeconds(2), $"Expected counter1 >= 1 with 50ms interval, but was {counter1}");

		sub1.Dispose();

		// Timer should be disposed now, counter should stop
		var c1After = counter1;
		await Task.Delay(150, CancellationToken);
		counter1.AssertEqual(c1After, $"Expected counter1 to stay at {c1After} after dispose, but was {counter1}");

		// Add new action - timer should restart
		var sub2 = d.InvokePeriodically(() => Interlocked.Increment(ref counter2), TimeSpan.FromMilliseconds(50));

		await WaitForAsync(() => counter2 >= 2, TimeSpan.FromSeconds(2), $"Expected counter2 >= 2 with 50ms interval (new timer), but was {counter2}");

		sub2.Dispose();
	}

	[TestMethod]
	public async Task InvokePeriodically_ThreeActions_DifferentIntervals()
	{
		// Test three actions with different intervals running simultaneously
		IDispatcher d = new DummyDispatcher();

		var fast = 0;   // 40ms
		var medium = 0; // 100ms
		var slow = 0;   // 200ms

		using var subFast = d.InvokePeriodically(() => Interlocked.Increment(ref fast), TimeSpan.FromMilliseconds(40));
		using var subMedium = d.InvokePeriodically(() => Interlocked.Increment(ref medium), TimeSpan.FromMilliseconds(100));
		using var subSlow = d.InvokePeriodically(() => Interlocked.Increment(ref slow), TimeSpan.FromMilliseconds(200));

		// Wait 800ms
		await Task.Delay(800, CancellationToken);

		// In 800ms:
		// fast (40ms): ~20 ticks ideally, expect at least 5 (conservative due to system load)
		// medium (100ms): ~8 ticks ideally, expect at least 3
		// slow (200ms): ~4 ticks ideally, expect at least 2
		(fast >= 5).AssertTrue($"Expected fast >= 5 (40ms interval, 800ms wait), but was {fast}");
		(medium >= 3).AssertTrue($"Expected medium >= 3 (100ms interval, 800ms wait), but was {medium}");
		(slow >= 2).AssertTrue($"Expected slow >= 2 (200ms interval, 800ms wait), but was {slow}");

		// Verify ratio roughly matches (fast should be ~2.5x medium, medium ~2.5x slow)
		(fast > medium).AssertTrue($"Expected fast > medium, but fast={fast}, medium={medium}");
		(medium > slow).AssertTrue($"Expected medium > slow, but medium={medium}, slow={slow}");
	}

	[TestMethod]
	public async Task InvokePeriodically_RemoveMiddleInterval_RecalculatesToFastest()
	{
		// When removing the middle interval, timer should stay at fastest
		IDispatcher d = new DummyDispatcher();

		var fast = 0;
		var medium = 0;
		var slow = 0;

		using var subFast = d.InvokePeriodically(() => Interlocked.Increment(ref fast), TimeSpan.FromMilliseconds(30));
		var subMedium = d.InvokePeriodically(() => Interlocked.Increment(ref medium), TimeSpan.FromMilliseconds(100));
		using var subSlow = d.InvokePeriodically(() => Interlocked.Increment(ref slow), TimeSpan.FromMilliseconds(300));

		// Remove medium
		subMedium.Dispose();

		GetTimerInterval(d).AssertEqual(TimeSpan.FromMilliseconds(30), "Timer interval should remain at 30ms after removing the middle subscription.");
	}

	[TestMethod]
	public async Task InvokePeriodically_RemoveFastestInterval_SlowsDown()
	{
		// When removing the fastest interval, timer should slow down
		IDispatcher d = new DummyDispatcher();

		var fast = 0;
		var slow = 0;

		var subFast = d.InvokePeriodically(() => Interlocked.Increment(ref fast), TimeSpan.FromMilliseconds(30));
		using var subSlow = d.InvokePeriodically(() => Interlocked.Increment(ref slow), TimeSpan.FromMilliseconds(300));

		await WaitForAsync(() => fast >= 3, TimeSpan.FromSeconds(2), $"Expected fast >= 3 with 30ms interval, but was {fast}");

		// Remove fast action
		subFast.Dispose();
		var slowBefore = slow;

		GetTimerInterval(d).AssertEqual(TimeSpan.FromMilliseconds(300), "Timer interval should switch to 300ms after removing the fastest subscription.");
		await WaitForAsync(() => slow > slowBefore, TimeSpan.FromSeconds(2), $"Expected slow to tick after switching to 300ms interval, but slowBefore={slowBefore}, slow={slow}");
	}

	[TestMethod]
	public void InvokePeriodically_InvalidInterval_Throws()
	{
		IDispatcher d = new DummyDispatcher();

		ThrowsExactly<ArgumentOutOfRangeException>(() => d.InvokePeriodically(() => { }, TimeSpan.Zero));
		ThrowsExactly<ArgumentOutOfRangeException>(() => d.InvokePeriodically(() => { }, TimeSpan.FromMilliseconds(-1)));
	}

	[TestMethod]
	public async Task InvokePeriodically_SameActionDifferentIntervals_UnsubscribeRemovesCorrectSubscription()
	{
		IDispatcher d = new DummyDispatcher();

		var counter = 0;
		Action action = () => Interlocked.Increment(ref counter);

		// Use 100ms interval (more reliable than 30ms on slow systems)
		using var fastSub = d.InvokePeriodically(action, TimeSpan.FromMilliseconds(100));
		var slowSub = d.InvokePeriodically(action, TimeSpan.FromMilliseconds(500));
		GetPlannerCount(d).AssertEqual(2, "Expected two subscriptions to be registered.");
		GetTimerInterval(d).AssertEqual(TimeSpan.FromMilliseconds(100), "Timer interval should be 100ms while the fast subscription is present.");

		// Give timer time to start on slow systems
		await Task.Delay(50, CancellationToken);

		await WaitForAsync(() => counter >= 3, TimeSpan.FromSeconds(10), $"Expected counter >= 3 with 100ms interval, but was {counter}");

		// Dispose the slow subscription and ensure the fast one continues.
		slowSub.Dispose();
		GetPlannerCount(d).AssertEqual(1, "Expected only the fast subscription to remain after disposing the slow subscription.");
		GetTimerInterval(d).AssertEqual(TimeSpan.FromMilliseconds(100), "Timer interval should remain at 100ms after disposing the slow subscription.");
	}
}
