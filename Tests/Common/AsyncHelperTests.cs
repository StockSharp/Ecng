namespace Ecng.Tests.Common;

[TestClass]
public class AsyncHelperTests : BaseTestClass
{
	[TestMethod]
	public async Task WithCancellationCancel()
	{
		using var cts = new CancellationTokenSource();
		var task = Task.Delay(1000, CancellationToken).WithCancellation(cts.Token);
		cts.Cancel();
		await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await task);
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
	public async Task WhenAllFailure()
	{
		var err = new InvalidOperationException();
		var tasks = new[] { new ValueTask<int>(Task.FromException<int>(err)) };
		await Assert.ThrowsExactlyAsync<AggregateException>(async () => await AsyncHelper.WhenAll(tasks));
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
		var token = TimeSpan.FromMilliseconds(10).CreateTimeoutToken();
		await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await token.WhenCanceled());
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

		var timerTask = AsyncHelper.StartPeriodicTimer(() => count++, TimeSpan.FromMilliseconds(50), cts.Token);

		await Task.Delay(150); // Should trigger approximately 3 times
		cts.Cancel();

		try
		{
			await timerTask;
		}
		catch (OperationCanceledException)
		{
			// Expected
		}

		count.AssertInRange(1, 5); // Allow some variance (2-4 expected)
	}

	[TestMethod]
	public async Task StartPeriodicTimerWithArgument()
	{
		var sum = 0;
		using var cts = new CancellationTokenSource();

		var timerTask = AsyncHelper.StartPeriodicTimer<int>(x => sum += x, 5, TimeSpan.FromMilliseconds(50), cts.Token);

		await Task.Delay(150);
		cts.Cancel();

		try
		{
			await timerTask;
		}
		catch (OperationCanceledException)
		{
			// Expected
		}

		sum.AssertInRange(9, 21); // 5 * 2-4 executions
	}

	[TestMethod]
	public async Task StartPeriodicTimerAsync()
	{
		var count = 0;
		using var cts = new CancellationTokenSource();

		var timerTask = AsyncHelper.StartPeriodicTimer(async () =>
		{
			count++;
			await Task.Delay(10);
		}, TimeSpan.FromMilliseconds(50), cts.Token);

		await Task.Delay(150);
		cts.Cancel();

		try
		{
			await timerTask;
		}
		catch (OperationCanceledException)
		{
			// Expected
		}

		count.AssertInRange(1, 5);
	}

	[TestMethod]
	public async Task StartPeriodicTimerWithInitialDelay()
	{
		var count = 0;
		using var cts = new CancellationTokenSource();

		var timerTask = AsyncHelper.StartPeriodicTimer(() => count++, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50), cts.Token);

		await Task.Delay(75); // Before first execution
		count.AssertEqual(0);

		await Task.Delay(200); // After first execution - wait longer
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
		timer.Start(TimeSpan.FromMilliseconds(50));
		timer.IsRunning.AssertTrue();
		timer.Interval.AssertEqual(TimeSpan.FromMilliseconds(50));

		await Task.Delay(150);
		count.AssertInRange(1, 5);

		// Stop timer
		timer.Stop();
		timer.IsRunning.AssertFalse();

		var countAfterStop = count;
		await Task.Delay(100);
		count.AssertEqual(countAfterStop); // Count should not increase after stop

		timer.Dispose();
	}

	[TestMethod]
	public async Task PeriodicTimerChangeInterval()
	{
		var count = 0;
		var timer = AsyncHelper.CreatePeriodicTimer(() => count++);

		// Start with 50ms interval
		timer.Start(TimeSpan.FromMilliseconds(50));
		await Task.Delay(150);
		var countAfterFast = count;
		countAfterFast.AssertInRange(1, 5);

		// Change to 100ms interval
		timer.ChangeInterval(TimeSpan.FromMilliseconds(100));
		timer.Interval.AssertEqual(TimeSpan.FromMilliseconds(100));

		count = 0;
		await Task.Delay(250);
		count.AssertInRange(1, 4); // Slower interval

		timer.Dispose();
	}

	[TestMethod]
	public async Task PeriodicTimerWithInitialDelay()
	{
		var count = 0;
		var timer = AsyncHelper.CreatePeriodicTimer(() => count++);

		timer.Start(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100));

		await Task.Delay(75);
		count.AssertEqual(0); // Initial delay not passed

		await Task.Delay(200); // Wait longer to ensure at least one execution
		(count > 0).AssertTrue($"Count should be > 0, but was {count}");

		timer.Dispose();
	}

	[TestMethod]
	public void PeriodicTimerMultipleStarts()
	{
		var count = 0;
		var timer = AsyncHelper.CreatePeriodicTimer(() => count++);

		timer.Start(TimeSpan.FromMilliseconds(50));
		timer.Start(TimeSpan.FromMilliseconds(50)); // Should stop previous and start new

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
			await Task.Delay(10);
		});

		timer.Start(TimeSpan.FromMilliseconds(50));
		await Task.Delay(150);

		count.AssertInRange(1, 5);

		timer.Dispose();
	}
}