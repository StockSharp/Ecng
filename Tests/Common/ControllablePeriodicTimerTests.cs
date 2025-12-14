namespace Ecng.Tests.Common;

[TestClass]
public class ControllablePeriodicTimerTests : BaseTestClass
{
	[TestMethod]
	public async Task Start_ExecutesHandler()
	{
		var counter = 0;

		using var timer = new ControllablePeriodicTimer(() =>
		{
			Interlocked.Increment(ref counter);
			return Task.CompletedTask;
		});

		timer.Start(TimeSpan.FromMilliseconds(50));

		// Wait for a few ticks
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (counter < 3 && sw.Elapsed < TimeSpan.FromSeconds(2))
			await Task.Delay(10, CancellationToken);

		(counter >= 3).AssertTrue($"Expected counter >= 3, but was {counter} after {sw.ElapsedMilliseconds}ms");
	}

	[TestMethod]
	public void Start_SetsIsRunning()
	{
		using var timer = new ControllablePeriodicTimer(() => Task.CompletedTask);

		timer.IsRunning.AssertFalse("Timer should not be running before Start()");

		timer.Start(TimeSpan.FromMilliseconds(100));

		timer.IsRunning.AssertTrue("Timer should be running after Start()");
	}

	[TestMethod]
	public void Start_SetsInterval()
	{
		using var timer = new ControllablePeriodicTimer(() => Task.CompletedTask);

		var interval = TimeSpan.FromMilliseconds(123);
		timer.Start(interval);

		timer.Interval.AssertEqual(interval, $"Expected interval={interval}, but was {timer.Interval}");
	}

	[TestMethod]
	public async Task Start_WithInitialDelay()
	{
		var counter = 0;
		var sw = System.Diagnostics.Stopwatch.StartNew();
		var firstTickTime = TimeSpan.Zero;
		var initialDelay = TimeSpan.FromMilliseconds(200);
		var jitter = TimeSpan.FromMilliseconds(25);

		using var timer = new ControllablePeriodicTimer(() =>
		{
			if (Interlocked.Increment(ref counter) == 1)
				firstTickTime = sw.Elapsed;
			return Task.CompletedTask;
		});

		timer.Start(TimeSpan.FromMilliseconds(50), start: initialDelay);

		// Timer-based tests are inherently subject to OS scheduling jitter.
		// Validate that no tick happens too early, but allow a small tolerance.
		await Task.Delay(initialDelay - jitter, CancellationToken);
		counter.AssertEqual(0, $"Expected no ticks before {initialDelay - jitter}, but was {counter}");

		// Wait for first tick
		while (counter < 1 && sw.Elapsed < TimeSpan.FromSeconds(2))
			await Task.Delay(10, CancellationToken);

		(counter >= 1).AssertTrue($"Expected at least 1 tick, but was {counter}");
		// First tick should happen after initial delay + first interval
		// Initial delay is 200ms, interval is 50ms, so first tick around 250ms+
		(firstTickTime >= initialDelay - jitter).AssertTrue($"Expected first tick after at least {(initialDelay - jitter).TotalMilliseconds}ms (tolerance applied), but was at {firstTickTime.TotalMilliseconds}ms");
	}

	[TestMethod]
	public void Stop_StopsTimer()
	{
		using var timer = new ControllablePeriodicTimer(() => Task.CompletedTask);

		timer.Start(TimeSpan.FromMilliseconds(50));
		timer.IsRunning.AssertTrue("Timer should be running after Start()");

		timer.Stop();
		timer.IsRunning.AssertFalse("Timer should not be running after Stop()");
	}

	[TestMethod]
	public async Task Stop_NoMoreTicks()
	{
		var counter = 0;

		using var timer = new ControllablePeriodicTimer(() =>
		{
			Interlocked.Increment(ref counter);
			return Task.CompletedTask;
		});

		timer.Start(TimeSpan.FromMilliseconds(50));

		// Wait for a few ticks
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (counter < 3 && sw.Elapsed < TimeSpan.FromSeconds(2))
			await Task.Delay(10, CancellationToken);

		(counter >= 3).AssertTrue($"Expected counter >= 3, but was {counter}");

		var counterBefore = counter;
		timer.Stop();

		await Task.Delay(200, CancellationToken);

		counter.AssertEqual(counterBefore, $"Expected counter to stay at {counterBefore} after Stop(), but was {counter}");
	}

	[TestMethod]
	public void Stop_CanBeCalledMultipleTimes()
	{
		using var timer = new ControllablePeriodicTimer(() => Task.CompletedTask);

		timer.Start(TimeSpan.FromMilliseconds(50));
		timer.Stop();
		timer.Stop(); // Should not throw
		timer.Stop(); // Should not throw
	}

	[TestMethod]
	public void Stop_CanBeCalledWithoutStart()
	{
		using var timer = new ControllablePeriodicTimer(() => Task.CompletedTask);

		timer.Stop(); // Should not throw
	}

	[TestMethod]
	public async Task ChangeInterval_WhileRunning_ChangesInterval()
	{
		var counter = 0;

		using var timer = new ControllablePeriodicTimer(() =>
		{
			Interlocked.Increment(ref counter);
			return Task.CompletedTask;
		});

		// Start with fast interval
		timer.Start(TimeSpan.FromMilliseconds(30));
		timer.Interval.AssertEqual(TimeSpan.FromMilliseconds(30), "Initial interval should be 30ms");

		await Task.Delay(150, CancellationToken);
		var countAtFastRate = counter;
		(countAtFastRate >= 2).AssertTrue($"Expected at least 2 ticks at fast rate, but was {countAtFastRate}");

		// Change to slower interval
		var newInterval = TimeSpan.FromMilliseconds(200);
		timer.ChangeInterval(newInterval);

		timer.Interval.AssertEqual(newInterval, $"Expected interval={newInterval}, but was {timer.Interval}");
		timer.IsRunning.AssertTrue("Timer should still be running after ChangeInterval()");

		var counterAfterChange = counter;
		await Task.Delay(150, CancellationToken);

		// With 200ms interval, should get at most 1 tick in 150ms
		var ticksAtSlowRate = counter - counterAfterChange;
		(ticksAtSlowRate <= 1).AssertTrue(
			$"Expected at most 1 tick at slow rate (200ms) in 150ms, but got {ticksAtSlowRate}");
	}

	[TestMethod]
	public void ChangeInterval_WhileNotRunning_OnlySetsInterval()
	{
		using var timer = new ControllablePeriodicTimer(() => Task.CompletedTask);

		var newInterval = TimeSpan.FromMilliseconds(500);
		timer.ChangeInterval(newInterval);

		timer.Interval.AssertEqual(newInterval, $"Expected interval={newInterval}, but was {timer.Interval}");
		timer.IsRunning.AssertFalse("Timer should not be running");
	}

	[TestMethod]
	public async Task ChangeInterval_ToFaster_IncreasesTickRate()
	{
		var counter = 0;

		using var timer = new ControllablePeriodicTimer(() =>
		{
			Interlocked.Increment(ref counter);
			return Task.CompletedTask;
		});

		// Start with slow interval
		timer.Start(TimeSpan.FromMilliseconds(200));

		await Task.Delay(300, CancellationToken);
		var countAtSlowRate = counter;

		// Change to faster interval
		timer.ChangeInterval(TimeSpan.FromMilliseconds(30));

		var counterAfterChange = counter;
		await Task.Delay(200, CancellationToken);

		var ticksAtFastRate = counter - counterAfterChange;
		// With 30ms interval, expect at least 3 ticks in 200ms
		(ticksAtFastRate >= 3).AssertTrue(
			$"Expected at least 3 ticks at fast rate (30ms) in 200ms, but got {ticksAtFastRate}");
	}

	[TestMethod]
	public async Task ChangeInterval_MultipleChanges()
	{
		var counter = 0;

		using var timer = new ControllablePeriodicTimer(() =>
		{
			Interlocked.Increment(ref counter);
			return Task.CompletedTask;
		});

		timer.Start(TimeSpan.FromMilliseconds(50));
		timer.Interval.AssertEqual(TimeSpan.FromMilliseconds(50), "Interval should be 50ms");

		await Task.Delay(100, CancellationToken);

		timer.ChangeInterval(TimeSpan.FromMilliseconds(100));
		timer.Interval.AssertEqual(TimeSpan.FromMilliseconds(100), "Interval should be 100ms after first change");

		await Task.Delay(50, CancellationToken);

		timer.ChangeInterval(TimeSpan.FromMilliseconds(30));
		timer.Interval.AssertEqual(TimeSpan.FromMilliseconds(30), "Interval should be 30ms after second change");

		await Task.Delay(50, CancellationToken);

		timer.ChangeInterval(TimeSpan.FromMilliseconds(200));
		timer.Interval.AssertEqual(TimeSpan.FromMilliseconds(200), "Interval should be 200ms after third change");

		timer.IsRunning.AssertTrue("Timer should still be running after multiple interval changes");
	}

	[TestMethod]
	public void ChangeInterval_ReturnsThis_ForChaining()
	{
		using var timer = new ControllablePeriodicTimer(() => Task.CompletedTask);

		var result = timer.ChangeInterval(TimeSpan.FromMilliseconds(100));

		result.AssertSame(timer, "ChangeInterval should return the same timer instance");
	}

	[TestMethod]
	public void Start_ReturnsThis_ForChaining()
	{
		using var timer = new ControllablePeriodicTimer(() => Task.CompletedTask);

		var result = timer.Start(TimeSpan.FromMilliseconds(100));

		result.AssertSame(timer, "Start should return the same timer instance");
	}

	[TestMethod]
	public void Dispose_StopsTimer()
	{
		var timer = new ControllablePeriodicTimer(() => Task.CompletedTask);
		timer.Start(TimeSpan.FromMilliseconds(50));

		timer.IsRunning.AssertTrue("Timer should be running before Dispose()");

		timer.Dispose();

		timer.IsRunning.AssertFalse("Timer should not be running after Dispose()");
	}

	[TestMethod]
	public void Dispose_CanBeCalledMultipleTimes()
	{
		var timer = new ControllablePeriodicTimer(() => Task.CompletedTask);
		timer.Start(TimeSpan.FromMilliseconds(50));

		timer.Dispose();
		timer.Dispose(); // Should not throw
		timer.Dispose(); // Should not throw
	}

	[TestMethod]
	public async Task Start_AfterStop_RestartsTimer()
	{
		var counter = 0;

		using var timer = new ControllablePeriodicTimer(() =>
		{
			Interlocked.Increment(ref counter);
			return Task.CompletedTask;
		});

		timer.Start(TimeSpan.FromMilliseconds(50));

		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (counter < 2 && sw.Elapsed < TimeSpan.FromSeconds(2))
			await Task.Delay(10, CancellationToken);

		timer.Stop();
		var counterAfterStop = counter;

		await Task.Delay(100, CancellationToken);
		counter.AssertEqual(counterAfterStop, "Counter should not change after Stop()");

		// Start again
		timer.Start(TimeSpan.FromMilliseconds(50));

		sw.Restart();
		while (counter < counterAfterStop + 2 && sw.Elapsed < TimeSpan.FromSeconds(2))
			await Task.Delay(10, CancellationToken);

		(counter >= counterAfterStop + 2).AssertTrue(
			$"Expected at least {counterAfterStop + 2} ticks after restart, but was {counter}");
	}

	[TestMethod]
	public async Task Start_CalledTwice_RestartsWith_NewInterval()
	{
		var counter = 0;

		using var timer = new ControllablePeriodicTimer(() =>
		{
			Interlocked.Increment(ref counter);
			return Task.CompletedTask;
		});

		// Start with slow interval
		timer.Start(TimeSpan.FromMilliseconds(200));
		timer.Interval.AssertEqual(TimeSpan.FromMilliseconds(200));

		await Task.Delay(100, CancellationToken);

		// Start again with faster interval - should restart
		timer.Start(TimeSpan.FromMilliseconds(30));
		timer.Interval.AssertEqual(TimeSpan.FromMilliseconds(30));

		var counterAfterRestart = counter;
		await Task.Delay(200, CancellationToken);

		var ticksAfterRestart = counter - counterAfterRestart;
		(ticksAfterRestart >= 3).AssertTrue(
			$"Expected at least 3 ticks with new fast interval, but got {ticksAfterRestart}");
	}

	[TestMethod]
	public async Task Handler_Exception_DoesNotStopTimer()
	{
		var counter = 0;
		var throwOnFirst = true;

		using var timer = new ControllablePeriodicTimer(() =>
		{
			Interlocked.Increment(ref counter);
			if (throwOnFirst)
			{
				throwOnFirst = false;
				throw new InvalidOperationException("Test exception");
			}
			return Task.CompletedTask;
		});

		timer.Start(TimeSpan.FromMilliseconds(50));

		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (counter < 3 && sw.Elapsed < TimeSpan.FromSeconds(2))
			await Task.Delay(10, CancellationToken);

		(counter >= 3).AssertTrue(
			$"Expected at least 3 ticks (timer continues after exception), but was {counter}");
		timer.IsRunning.AssertTrue("Timer should still be running after handler exception");
	}

	[TestMethod]
	public async Task Handler_AsyncException_DoesNotStopTimer()
	{
		var counter = 0;
		var throwOnFirst = true;

		using var timer = new ControllablePeriodicTimer(async () =>
		{
			Interlocked.Increment(ref counter);
			await Task.Yield();
			if (throwOnFirst)
			{
				throwOnFirst = false;
				throw new InvalidOperationException("Test async exception");
			}
		});

		timer.Start(TimeSpan.FromMilliseconds(50));

		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (counter < 3 && sw.Elapsed < TimeSpan.FromSeconds(2))
			await Task.Delay(10, CancellationToken);

		(counter >= 3).AssertTrue(
			$"Expected at least 3 ticks (timer continues after async exception), but was {counter}");
	}

	[TestMethod]
	public async Task Handler_Async_ExecutesCorrectly()
	{
		var counter = 0;

		using var timer = new ControllablePeriodicTimer(async () =>
		{
			await Task.Delay(10, CancellationToken);
			Interlocked.Increment(ref counter);
		});

		timer.Start(TimeSpan.FromMilliseconds(50));

		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (counter < 3 && sw.Elapsed < TimeSpan.FromSeconds(2))
			await Task.Delay(10, CancellationToken);

		(counter >= 3).AssertTrue($"Expected counter >= 3, but was {counter}");
	}

	[TestMethod]
	public async Task ChangeInterval_DuringHandlerExecution()
	{
		var inHandler = new ManualResetEventSlim(false);
		var canContinue = new ManualResetEventSlim(false);
		var counter = 0;

		using var timer = new ControllablePeriodicTimer(() =>
		{
			Interlocked.Increment(ref counter);
			inHandler.Set();
			canContinue.Wait(TimeSpan.FromSeconds(1), CancellationToken);
			return Task.CompletedTask;
		});

		timer.Start(TimeSpan.FromMilliseconds(30));

		// Wait for handler to start executing
		inHandler.Wait(TimeSpan.FromSeconds(1), CancellationToken).AssertTrue("Handler should have started");

		// Change interval while handler is executing
		timer.ChangeInterval(TimeSpan.FromMilliseconds(200));
		timer.Interval.AssertEqual(TimeSpan.FromMilliseconds(200));

		// Let handler continue
		canContinue.Set();

		await Task.Delay(100, CancellationToken);

		timer.IsRunning.AssertTrue("Timer should still be running after ChangeInterval during handler");
	}
}

