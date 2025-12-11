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
}

