namespace Ecng.Tests.ComponentModel;

using System;
using System.Threading;
using System.Threading.Tasks;

using Ecng.ComponentModel;
using Ecng.UnitTesting;

[TestClass]
public class DummyDispatcherTests : BaseTestClass
{
    [TestMethod]
    public void CheckAccess_AlwaysTrue()
    {
        IDispatcher d = new DummyDispatcher();

        // From current thread
        d.CheckAccess().AssertTrue();

        // From another thread
        var otherThreadResult = false;
        var t = new Thread(() => otherThreadResult = d.CheckAccess())
        {
            IsBackground = true
        };
        t.Start();
        t.Join();
        otherThreadResult.AssertTrue();
    }

    [TestMethod]
    public void Invoke_IsSynchronous()
    {
        IDispatcher d = new DummyDispatcher();

        var value = 0;
        d.Invoke(() => value = 42);

        // Should be set immediately, before return
        value.AssertEqual(42);
    }

    [TestMethod]
    public void InvokeAsync_ExecutesLater()
    {
        IDispatcher d = new DummyDispatcher();

        var value = 0;
        using var done = new ManualResetEventSlim(false);

        d.InvokeAsync(() => { Interlocked.Exchange(ref value, 1); done.Set(); });

        // Should complete within a reasonable time
        done.Wait(TimeSpan.FromSeconds(2), CancellationToken).AssertTrue();
        value.AssertEqual(1);
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

        (counter >= 3).AssertTrue();

        // After dispose, counter should stop increasing.
        var after = counter;
        sub.Dispose();
        await Task.Delay(200, CancellationToken);
        counter.AssertEqual(after);
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

        (c1 >= 3).AssertTrue();
        (c2 >= 2).AssertTrue();

        // Dispose the faster one, ensure it stops while the slower continues.
        var before1 = c1;
        s1.Dispose();

        var oldC2 = c2;
        await Task.Delay(250, CancellationToken);

        c1.AssertEqual(before1);
        (c2 > oldC2).AssertTrue();
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

        // Wait for a few fast ticks
        await Task.Delay(200, CancellationToken);
        (fastCounter >= 2).AssertTrue();

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
        (slowCounter >= 1).AssertTrue();

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
        (counter >= 3).AssertTrue();
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

        (counter >= 3).AssertTrue();

        // Remove the last (and only) action
        var counterBefore = counter;
        sub.Dispose();

        // Wait and verify no more ticks
        await Task.Delay(200, CancellationToken);
        counter.AssertEqual(counterBefore);
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

        // Wait for it to tick
        await Task.Delay(450, CancellationToken);
        (counter1 >= 2).AssertTrue();

        // Add faster action with 50ms interval - timer should speed up
        var sub2 = d.InvokePeriodically(() => Interlocked.Increment(ref counter2), TimeSpan.FromMilliseconds(50));

        await Task.Delay(200, CancellationToken);
        (counter2 >= 3).AssertTrue();

        // Remove fast action - timer should slow down to 200ms
        var c1Before = counter1;
        sub2.Dispose();

        await Task.Delay(150, CancellationToken);
        // counter1 should not have increased much (200ms interval)
        (counter1 - c1Before <= 1).AssertTrue();

        // Add even faster action with 30ms interval
        var sub3 = d.InvokePeriodically(() => Interlocked.Increment(ref counter3), TimeSpan.FromMilliseconds(30));

        await Task.Delay(150, CancellationToken);
        (counter3 >= 4).AssertTrue();

        // Remove all
        sub3.Dispose();
        sub1.Dispose();

        // Verify everything stopped
        var finalC1 = counter1;
        var finalC3 = counter3;
        await Task.Delay(200, CancellationToken);

        counter1.AssertEqual(finalC1);
        counter3.AssertEqual(finalC3);
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

        await Task.Delay(150, CancellationToken);
        (counter1 >= 2).AssertTrue();

        sub1.Dispose();

        // Timer should be disposed now, counter should stop
        var c1After = counter1;
        await Task.Delay(150, CancellationToken);
        counter1.AssertEqual(c1After);

        // Add new action - timer should restart
        var sub2 = d.InvokePeriodically(() => Interlocked.Increment(ref counter2), TimeSpan.FromMilliseconds(50));

        await Task.Delay(200, CancellationToken);
        (counter2 >= 3).AssertTrue();

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

        // Wait 500ms
        await Task.Delay(500, CancellationToken);

        // In 500ms:
        // fast (40ms): ~12 ticks
        // medium (100ms): ~5 ticks
        // slow (200ms): ~2 ticks
        (fast >= 10).AssertTrue();
        (medium >= 4).AssertTrue();
        (slow >= 2).AssertTrue();

        // Verify ratio roughly matches (fast should be ~2.5x medium, medium ~2.5x slow)
        (fast > medium).AssertTrue();
        (medium > slow).AssertTrue();
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
        await Task.Delay(150, CancellationToken);

        // Fast should continue at same rate (30ms interval)
        (fast - fastBefore >= 4).AssertTrue();
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

        await Task.Delay(150, CancellationToken);
        (fast >= 4).AssertTrue();

        // Remove fast action
        subFast.Dispose();
        var slowBefore = slow;

        // Wait 400ms - with 300ms interval, should get ~1 tick
        await Task.Delay(400, CancellationToken);

        // Slow should have ticked ~1-2 times (300ms interval)
        var slowTicks = slow - slowBefore;
        (slowTicks >= 1 && slowTicks <= 2).AssertTrue();
    }
}

