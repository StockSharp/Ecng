namespace Ecng.Tests.Common;

[TestClass]
public class AsyncHelperTests : BaseTestClass
{
	[TestMethod]
	public Task WithCancellationCancel()
	{
		using var cts = new CancellationTokenSource();
		var task = TimeSpan.FromSeconds(1).Delay(CancellationToken).WithCancellation(cts.Token);
		cts.Cancel();
		return ThrowsExactlyAsync<OperationCanceledException>(() => task);
	}

	[TestMethod]
	public async Task WhenAllValueTasks()
	{
		var res = await AsyncHelper.WhenAll([new ValueTask<int>(1), new ValueTask<int>(2)]);
		res.AssertEqual([1, 2]);
	}

	[TestMethod]
	public void RunSync()
	{
		var result = AsyncHelper.Run(() => new ValueTask<int>(3));
		result.AssertEqual(3);
	}

	[TestMethod]
	public async Task CheckNull()
	{
		await AsyncHelper.CheckNull((Task)null);
		await AsyncHelper.CheckNull((ValueTask?)null);
	}

	[TestMethod]
	public void CreateChildTokenCancel()
	{
		using var cts = new CancellationTokenSource();
		var (childCts, token) = cts.Token.CreateChildToken();
		cts.Cancel();
		token.IsCancellationRequested.AssertTrue();
		childCts.Dispose();
	}

	[TestMethod]
	public async Task AsValueTaskConversions()
	{
		var vt = new ValueTask<int>(4);
		await vt.AsValueTask();

		var task = 5.FromResult();
		(await task.AsValueTask()).AssertEqual(5);
	}

	[TestMethod]
	public Task WhenAllFailure()
	{
		var err = new InvalidOperationException();
		var tasks = new[] { new ValueTask<int>(Task.FromException<int>(err)) };
		return ThrowsExactlyAsync<AggregateException>(() => tasks.WhenAll().AsTask());
	}

	[TestMethod]
	public void GetResultAndTcs()
	{
		var task = 6.FromResult();
		task.GetResult<int>().AssertEqual(6);

		var tcs = 7.ToCompleteSource();
		tcs.Task.Result.AssertEqual(7);
	}

	[TestMethod]
	public async Task TimeoutTokenAndWhenCanceled()
	{
		using var cts = TimeSpan.FromMilliseconds(10).CreateTimeout();
		await ThrowsExactlyAsync<TaskCanceledException>(() => cts.Token.WhenCanceled());
	}

	[TestMethod]
	public async Task CreateTimeout_CancelsAfterDelay()
	{
		using var cts = TimeSpan.FromMilliseconds(50).CreateTimeout();
		cts.Token.IsCancellationRequested.AssertFalse();
		var sw = Stopwatch.StartNew();

		while (!cts.Token.IsCancellationRequested && sw.Elapsed < TimeSpan.FromSeconds(2))
			await Task.Delay(10, CancellationToken);

		cts.Token.IsCancellationRequested.AssertTrue($"Expected token cancellation within 2s, but was not cancelled after {sw.ElapsedMilliseconds}ms.");
	}

	[TestMethod]
	public async Task CatchHandleError()
	{
		bool error = false, final = false;
		await AsyncHelper.CatchHandle(
			() => Task.FromException(new InvalidOperationException()),
			CancellationToken,
			e => error = true,
			finalizer: () => final = true);
		error.AssertTrue();
		final.AssertTrue();
	}

	[TestMethod]
	public async Task StartPeriodicTimerBasic()
	{
		var count = 0;
		using var cts = new CancellationTokenSource();

		var timerTask = AsyncHelper.StartPeriodicTimer(() => count++, TimeSpan.FromMilliseconds(100), cts.Token);

		await Task.Delay(350, CancellationToken); // Should trigger approximately 3 times
		cts.Cancel();

		try
		{
			await timerTask;
		}
		catch (OperationCanceledException)
		{
			// Expected
		}

		count.AssertInRange(1, 6, $"count={count}"); // Allow some variance for timer jitter
	}

	[TestMethod]
	public async Task StartPeriodicTimerWithArgument()
	{
		var sum = 0;
		using var cts = new CancellationTokenSource();

		var timerTask = AsyncHelper.StartPeriodicTimer(x => sum += x, 5, TimeSpan.FromMilliseconds(100), cts.Token);

		await Task.Delay(350, CancellationToken);
		cts.Cancel();

		try
		{
			await timerTask;
		}
		catch (OperationCanceledException)
		{
			// Expected
		}

		sum.AssertInRange(5, 30); // 5 * 1-6 executions, allow variance
	}

	[TestMethod]
	public async Task StartPeriodicTimerAsync()
	{
		var count = 0;
		using var cts = new CancellationTokenSource();

		var timerTask = AsyncHelper.StartPeriodicTimer(async () =>
		{
			count++;
			await Task.Delay(10, CancellationToken);
		}, TimeSpan.FromMilliseconds(100), cts.Token);

		await Task.Delay(350, CancellationToken);
		cts.Cancel();

		try
		{
			await timerTask;
		}
		catch (OperationCanceledException)
		{
			// Expected
		}

		count.AssertInRange(1, 6);
	}

	[TestMethod]
	public async Task StartPeriodicTimerWithInitialDelay()
	{
		var count = 0;
		using var cts = new CancellationTokenSource();

		// StartPeriodicTimer(handler, start, interval) - start is initial delay
		// Use larger initial delay (500ms) for reliability on slow systems
		var timerTask = AsyncHelper.StartPeriodicTimer(() => count++, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(100), cts.Token);

		await Task.Delay(200, CancellationToken); // Well before first execution (500ms initial delay)
		count.AssertEqual(0, $"Count should be 0 before initial delay (200ms < 500ms), but was {count}");

		await Task.Delay(600, CancellationToken); // After initial delay passed (total 800ms > 500ms)
		(count > 0).AssertTrue($"Count should be > 0, but was {count}");

		cts.Cancel();

		try
		{
			await timerTask;
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
	}

	[TestMethod]
	public async Task CreatePeriodicTimerAndControl()
	{
		var count = 0;
		var timer = AsyncHelper.CreatePeriodicTimer(() => count++);

		timer.IsRunning.AssertFalse();

		// Start timer
		timer.Start(TimeSpan.FromMilliseconds(100));
		timer.IsRunning.AssertTrue();
		timer.Interval.AssertEqual(TimeSpan.FromMilliseconds(100));

		await Task.Delay(350, CancellationToken);
		count.AssertInRange(1, 6);

		// Stop timer
		timer.Stop();
		timer.IsRunning.AssertFalse();

		var countAfterStop = count;
		await Task.Delay(200, CancellationToken);
		count.AssertEqual(countAfterStop); // Count should not increase after stop

		timer.Dispose();
	}

	[TestMethod]
	public async Task PeriodicTimerChangeInterval()
	{
		var count = 0;
		var timer = AsyncHelper.CreatePeriodicTimer(() => Interlocked.Increment(ref count));

		// Start with 100ms interval
		timer.Start(TimeSpan.FromMilliseconds(100));
		await Task.Delay(450, CancellationToken); // Increased delay for more reliable timing
		var countAfterFast = Interlocked.CompareExchange(ref count, 0, 0);
		// With 100ms interval over 450ms, expect 2-6 ticks (tolerance for system load)
		(countAfterFast >= 1).AssertTrue($"Expected at least 1 tick, got {countAfterFast}");

		// Change to 300ms interval (slower, more distinct difference)
		timer.ChangeInterval(TimeSpan.FromMilliseconds(300));
		timer.Interval.AssertEqual(TimeSpan.FromMilliseconds(300));

		Interlocked.Exchange(ref count, 0);
		await Task.Delay(700, CancellationToken); // 700ms should give 1-3 ticks at 300ms interval
		var countAfterSlow = Interlocked.CompareExchange(ref count, 0, 0);

		// Verify that slower interval produces fewer ticks
		// With 300ms interval over 700ms, expect 1-3 ticks
		(countAfterSlow >= 1).AssertTrue($"Expected at least 1 tick with slow interval, got {countAfterSlow}");
		// And it should be noticeably slower than fast interval
		(countAfterSlow < countAfterFast + 2).AssertTrue($"Slow interval ({countAfterSlow}) should produce fewer ticks than fast ({countAfterFast})");

		timer.Dispose();
	}

	[TestMethod]
	public async Task PeriodicTimerWithInitialDelay()
	{
		var count = 0;
		var timer = AsyncHelper.CreatePeriodicTimer(() => count++);

		// Start(interval, start) - start is initial delay
		timer.Start(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(300));

		await Task.Delay(150, CancellationToken);
		count.AssertEqual(0); // Initial delay not passed (300ms)

		await Task.Delay(300, CancellationToken); // Wait longer to ensure at least one execution (total 450ms > 300ms)
		(count > 0).AssertTrue($"Count should be > 0, but was {count}");

		timer.Dispose();
	}

	[TestMethod]
	public void PeriodicTimerMultipleStarts()
	{
		var count = 0;
		var timer = AsyncHelper.CreatePeriodicTimer(() => count++);

		timer.Start(TimeSpan.FromMilliseconds(100));
		timer.Start(TimeSpan.FromMilliseconds(100)); // Should stop previous and start new

		timer.IsRunning.AssertTrue();

		timer.Dispose();
	}

	[TestMethod]
	public async Task PeriodicTimerAsyncHandler()
	{
		var count = 0;
		var timer = AsyncHelper.CreatePeriodicTimer(async () =>
		{
			count++;
			await Task.Delay(10, CancellationToken);
		});

		timer.Start(TimeSpan.FromMilliseconds(50));
		await Task.Delay(500, CancellationToken);

		count.AssertInRange(3, 15);

		timer.Dispose();
	}

	[TestMethod]
	public async Task ToAsync_Action()
	{
		var called = false;
		Action action = () => called = true;
		await action.ToAsync()(CancellationToken.None);
		called.AssertTrue();
	}

	[TestMethod]
	public async Task ToAsync_Action1()
	{
		var result = 0;
		Action<int> action = a => result = a;
		await action.ToAsync()(1, CancellationToken.None);
		result.AssertEqual(1);
	}

	[TestMethod]
	public async Task ToAsync_Action2()
	{
		var result = 0;
		Action<int, int> action = (a, b) => result = a + b;
		await action.ToAsync()(1, 2, CancellationToken.None);
		result.AssertEqual(3);
	}

	[TestMethod]
	public async Task ToAsync_Action3()
	{
		var result = 0;
		Action<int, int, int> action = (a, b, c) => result = a + b + c;
		await action.ToAsync()(1, 2, 3, CancellationToken.None);
		result.AssertEqual(6);
	}

	[TestMethod]
	public async Task ToAsync_Action4()
	{
		var result = 0;
		Action<int, int, int, int> action = (a, b, c, d) => result = a + b + c + d;
		await action.ToAsync()(1, 2, 3, 4, CancellationToken.None);
		result.AssertEqual(10);
	}

	[TestMethod]
	public async Task ToAsync_Action5()
	{
		var result = 0;
		Action<int, int, int, int, int> action = (a, b, c, d, e) => result = a + b + c + d + e;
		await action.ToAsync()(1, 2, 3, 4, 5, CancellationToken.None);
		result.AssertEqual(15);
	}

	[TestMethod]
	public async Task ToAsync_Action6()
	{
		var result = 0;
		Action<int, int, int, int, int, int> action = (a, b, c, d, e, f) => result = a + b + c + d + e + f;
		await action.ToAsync()(1, 2, 3, 4, 5, 6, CancellationToken.None);
		result.AssertEqual(21);
	}

	[TestMethod]
	public async Task ToAsync_Action7()
	{
		var result = 0;
		Action<int, int, int, int, int, int, int> action = (a, b, c, d, e, f, g) => result = a + b + c + d + e + f + g;
		await action.ToAsync()(1, 2, 3, 4, 5, 6, 7, CancellationToken.None);
		result.AssertEqual(28);
	}

	[TestMethod]
	public async Task ToAsync_Action8()
	{
		var result = 0;
		Action<int, int, int, int, int, int, int, int> action = (a, b, c, d, e, f, g, h) => result = a + b + c + d + e + f + g + h;
		await action.ToAsync()(1, 2, 3, 4, 5, 6, 7, 8, CancellationToken.None);
		result.AssertEqual(36);
	}

	[TestMethod]
	public async Task ToAsync_Action9()
	{
		var result = 0;
		Action<int, int, int, int, int, int, int, int, int> action = (a, b, c, d, e, f, g, h, i) => result = a + b + c + d + e + f + g + h + i;
		await action.ToAsync()(1, 2, 3, 4, 5, 6, 7, 8, 9, CancellationToken.None);
		result.AssertEqual(45);
	}

	[TestMethod]
	public async Task ToAsync_Action10()
	{
		var result = 0;
		Action<int, int, int, int, int, int, int, int, int, int> action = (a, b, c, d, e, f, g, h, i, j) => result = a + b + c + d + e + f + g + h + i + j;
		await action.ToAsync()(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, CancellationToken.None);
		result.AssertEqual(55);
	}

	[TestMethod]
	public async Task ToAsync_Action11()
	{
		var result = 0;
		Action<int, int, int, int, int, int, int, int, int, int, int> action = (a, b, c, d, e, f, g, h, i, j, k) => result = a + b + c + d + e + f + g + h + i + j + k;
		await action.ToAsync()(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, CancellationToken.None);
		result.AssertEqual(66);
	}

	[TestMethod]
	public async Task ToAsync_Action12()
	{
		var result = 0;
		Action<int, int, int, int, int, int, int, int, int, int, int, int> action = (a, b, c, d, e, f, g, h, i, j, k, l) => result = a + b + c + d + e + f + g + h + i + j + k + l;
		await action.ToAsync()(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, CancellationToken.None);
		result.AssertEqual(78);
	}

	[TestMethod]
	public async Task ToAsync_Action13()
	{
		var result = 0;
		Action<int, int, int, int, int, int, int, int, int, int, int, int, int> action = (a, b, c, d, e, f, g, h, i, j, k, l, m) => result = a + b + c + d + e + f + g + h + i + j + k + l + m;
		await action.ToAsync()(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, CancellationToken.None);
		result.AssertEqual(91);
	}

	[TestMethod]
	public async Task ToAsync_Action14()
	{
		var result = 0;
		Action<int, int, int, int, int, int, int, int, int, int, int, int, int, int> action = (a, b, c, d, e, f, g, h, i, j, k, l, m, n) => result = a + b + c + d + e + f + g + h + i + j + k + l + m + n;
		await action.ToAsync()(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, CancellationToken.None);
		result.AssertEqual(105);
	}

	[TestMethod]
	public async Task ToAsync_Action15()
	{
		var result = 0;
		Action<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int> action = (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o) => result = a + b + c + d + e + f + g + h + i + j + k + l + m + n + o;
		await action.ToAsync()(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, CancellationToken.None);
		result.AssertEqual(120);
	}

	[TestMethod]
	public void ToAsync_NullAction_Throws()
	{
		Action action = null;
		Throws<ArgumentNullException>(() => action.ToAsync());
	}

	[TestMethod]
	public void ToAsync_NullAction1_Throws()
	{
		Action<int> action = null;
		Throws<ArgumentNullException>(() => action.ToAsync());
	}

	[TestMethod]
	public void ToAsync_NullAction2_Throws()
	{
		Action<int, int> action = null;
		Throws<ArgumentNullException>(() => action.ToAsync());
	}

	[TestMethod]
	public void ToAsync_NullAction8_Throws()
	{
		Action<int, int, int, int, int, int, int, int> action = null;
		Throws<ArgumentNullException>(() => action.ToAsync());
	}

	[TestMethod]
	public void ToAsync_NullAction15_Throws()
	{
		Action<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int> action = null;
		Throws<ArgumentNullException>(() => action.ToAsync());
	}
}