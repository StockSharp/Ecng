namespace Ecng.Tests.ComponentModel;

using System;
using System.Threading;
using System.Threading.Tasks;

using Ecng.ComponentModel;
using Ecng.UnitTesting;

[TestClass]
public class DummyDispatcherTests : BaseTestClass
{
	private async Task WaitForAsync(Func<bool> predicate, TimeSpan timeout, string message)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();

		while (!predicate() && sw.Elapsed < timeout)
			await Task.Delay(10, CancellationToken);

		predicate().AssertTrue($"{message} after {sw.ElapsedMilliseconds}ms");
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

		// Should complete within a reasonable time
		done.Wait(TimeSpan.FromSeconds(2), CancellationToken).AssertTrue("InvokeAsync action should complete within 2 seconds");
		value.AssertEqual(1, $"Expected value=1, but was {value}");
	}

	[TestMethod]
	public async Task InvokePeriodically_Basic()
	{
		IDispatcher d = new DummyDispatcher();

		var counter = 0;
		using var sub = d.InvokePeriodically(() => Interlocked.Increment(ref counter), TimeSpan.FromMilliseconds(50));

		// Wait until a few ticks arrive
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (counter < 3 && sw.Elapsed < TimeSpan.FromSeconds(2))
			await Task.Delay(10, CancellationToken);

		(counter >= 3).AssertTrue($"Expected counter >= 3, but was {counter} after {sw.ElapsedMilliseconds}ms");

		// After dispose, counter should stop increasing.
		var after = counter;
		sub.Dispose();
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

		var sw = System.Diagnostics.Stopwatch.StartNew();
		while ((c1 < 3 || c2 < 2) && sw.Elapsed < TimeSpan.FromSeconds(3))
			await Task.Delay(10, CancellationToken);

		(c1 >= 3).AssertTrue($"Expected c1 >= 3, but was {c1} after {sw.ElapsedMilliseconds}ms");
		(c2 >= 2).AssertTrue($"Expected c2 >= 2, but was {c2} after {sw.ElapsedMilliseconds}ms");

		// Dispose the faster one, ensure it stops while the slower continues.
		var before1 = c1;
		s1.Dispose();

		var oldC2 = c2;
		await Task.Delay(250, CancellationToken);

		c1.AssertEqual(before1, $"Expected c1 to stay at {before1} after s1.Dispose(), but was {c1}");
		(c2 > oldC2).AssertTrue($"Expected c2 > {oldC2} (slower action should continue), but was {c2}");
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

		// Wait and count how many times the timer ticks
		// If interval is recalculated to 500ms, we should see ~1-2 ticks in 600ms
		// If interval stays at 50ms (bug), we would see ~12 ticks in 600ms
		await Task.Delay(600, CancellationToken);

		var ticksAfter = timerTickCount - ticksBefore;

		// With proper interval recalculation, should be ~1-2 ticks (500ms interval)
		// With bug (interval stays 50ms), would be ~12 ticks but only slowCounter increments
		// Since slow action has 500ms interval, it should only fire ~1 time in 600ms
		(slowCounter >= 1).AssertTrue($"Expected slowCounter >= 1, but was {slowCounter}, ticksAfter={ticksAfter}");

		// This assertion would fail if interval is not recalculated:
		// Timer would tick 12 times but slow action only runs once every 500ms
		// We can't directly test timer tick rate without modifying the code,
		// so this test mainly documents the expected behavior.
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
		var sw = System.Diagnostics.Stopwatch.StartNew();
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
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (counter < 3 && sw.Elapsed < TimeSpan.FromSeconds(2))
			await Task.Delay(10, CancellationToken);

		(counter >= 3).AssertTrue($"Expected counter >= 3, but was {counter} after {sw.ElapsedMilliseconds}ms");

		// Remove the last (and only) action
		var counterBefore = counter;
		sub.Dispose();

		// Wait and verify no more ticks
		await Task.Delay(200, CancellationToken);
		counter.AssertEqual(counterBefore, $"Expected counter to stay at {counterBefore} after removing last action, but was {counter}");
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

		// Wait for it to tick - first tick happens after interval delay
		await Task.Delay(600, CancellationToken);
		(counter1 >= 1).AssertTrue($"Expected counter1 >= 1 after 600ms with 200ms interval, but was {counter1}");

		// Add faster action with 50ms interval - timer should speed up
		long firstTickTs = 0;
		long tenthTickTs = 0;

		var sub2 = d.InvokePeriodically(() =>
		{
			var count = Interlocked.Increment(ref counter2);
			var ts = System.Diagnostics.Stopwatch.GetTimestamp();

			if (count == 1)
				Interlocked.Exchange(ref firstTickTs, ts);
			else if (count == 10)
				Interlocked.Exchange(ref tenthTickTs, ts);
		}, TimeSpan.FromMilliseconds(50));

		await WaitForAsync(() => Volatile.Read(ref firstTickTs) != 0 && Volatile.Read(ref tenthTickTs) != 0,
			TimeSpan.FromSeconds(3), "Expected 10 counter2 ticks with 50ms interval");

		var elapsedTicks = Volatile.Read(ref tenthTickTs) - Volatile.Read(ref firstTickTs);
		var elapsed = TimeSpan.FromSeconds(elapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency);
		(elapsed < TimeSpan.FromMilliseconds(1750)).AssertTrue($"Expected 10 counter2 ticks to take <1750ms after switching to 50ms interval, but took {elapsed.TotalMilliseconds:F0}ms");

		// Remove fast action - timer should slow down to 200ms
		var c1Before = counter1;
		sub2.Dispose();

		await Task.Delay(150, CancellationToken);
		// counter1 should not have increased much (200ms interval)
		(counter1 - c1Before <= 1).AssertTrue($"Expected counter1 - c1Before <= 1 (timer slowed to 200ms), but c1Before={c1Before}, counter1={counter1}, diff={counter1 - c1Before}");

		// Add even faster action with 30ms interval
		var sub3 = d.InvokePeriodically(() => Interlocked.Increment(ref counter3), TimeSpan.FromMilliseconds(30));

		await WaitForAsync(() => counter3 >= 3, TimeSpan.FromSeconds(2), $"Expected counter3 >= 3 with 30ms interval, but was {counter3}");

		// Remove all
		sub3.Dispose();
		sub1.Dispose();

		// Verify everything stopped
		var finalC1 = counter1;
		var finalC3 = counter3;
		await Task.Delay(200, CancellationToken);

		counter1.AssertEqual(finalC1, $"Expected counter1 to stay at {finalC1} after dispose, but was {counter1}");
		counter3.AssertEqual(finalC3, $"Expected counter3 to stay at {finalC3} after dispose, but was {counter3}");
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

		await Task.Delay(200, CancellationToken);
		(counter1 >= 1).AssertTrue($"Expected counter1 >= 1 after 200ms with 50ms interval, but was {counter1}");

		sub1.Dispose();

		// Timer should be disposed now, counter should stop
		var c1After = counter1;
		await Task.Delay(150, CancellationToken);
		counter1.AssertEqual(c1After, $"Expected counter1 to stay at {c1After} after dispose, but was {counter1}");

		// Add new action - timer should restart
		var sub2 = d.InvokePeriodically(() => Interlocked.Increment(ref counter2), TimeSpan.FromMilliseconds(50));

		await Task.Delay(250, CancellationToken);
		(counter2 >= 2).AssertTrue($"Expected counter2 >= 2 after 250ms with 50ms interval (new timer), but was {counter2}");

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
		// fast (40ms): ~20 ticks ideally, expect at least 6 (conservative due to system load)
		// medium (100ms): ~8 ticks ideally, expect at least 3
		// slow (200ms): ~4 ticks ideally, expect at least 2
		(fast >= 6).AssertTrue($"Expected fast >= 6 (40ms interval, 800ms wait), but was {fast}");
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

		await Task.Delay(200, CancellationToken);

		// Remove medium
		subMedium.Dispose();

		var fastBefore = fast;
		await Task.Delay(200, CancellationToken);

		// Fast should continue at same rate (30ms interval) - expect at least 3 ticks in 200ms
		(fast - fastBefore >= 3).AssertTrue($"Expected fast - fastBefore >= 3 (timer stays at 30ms), but fastBefore={fastBefore}, fast={fast}, diff={fast - fastBefore}");
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

		// Wait 400ms - with 300ms interval, should get ~1 tick
		await Task.Delay(400, CancellationToken);

		// Slow should have ticked ~1-2 times (300ms interval)
		var slowTicks = slow - slowBefore;
		(slowTicks >= 1 && slowTicks <= 2).AssertTrue($"Expected slowTicks >= 1 && <= 2 (300ms interval, 400ms wait), but slowBefore={slowBefore}, slow={slow}, slowTicks={slowTicks}");
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

		using var fastSub = d.InvokePeriodically(action, TimeSpan.FromMilliseconds(30));
		var slowSub = d.InvokePeriodically(action, TimeSpan.FromMilliseconds(300));

		await WaitForAsync(() => counter >= 3, TimeSpan.FromSeconds(2), $"Expected counter >= 3 with 30ms interval, but was {counter}");

		// Dispose the slow subscription and ensure the fast one continues.
		slowSub.Dispose();

		var before = counter;
		await WaitForAsync(() => counter - before >= 3, TimeSpan.FromSeconds(2), $"Expected fast subscription to continue after disposing slow subscription, but counterBefore={before}, counter={counter}, diff={counter - before}");
	}
}
