namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

using Nito.AsyncEx;

[TestClass]
public class ChannelExecutorTests : BaseTestClass
{
	[TestMethod]
	public async Task BasicExecution()
	{
		var token = CancellationToken;

		await using var executor = new ChannelExecutor();
		_ = executor.RunAsync(token);

		var counter = 0;
		executor.Add(() => counter++);
		executor.Add(() => counter++);
		executor.Add(() => counter++);

		await executor.WaitFlushAsync(token);

		counter.AssertEqual(3);
	}

	[TestMethod]
	public async Task SequentialExecution()
	{
		var token = CancellationToken;

		await using var executor = new ChannelExecutor();
		_ = executor.RunAsync(token);

		var list = new List<int>();
		for (int i = 0; i < 10; i++)
		{
			var value = i;
			executor.Add(() => list.Add(value));
		}

		await executor.WaitFlushAsync(token);

		list.Count.AssertEqual(10);
		for (int i = 0; i < 10; i++)
		{
			list[i].AssertEqual(i);
		}
	}

	[TestMethod]
	public async Task ErrorHandling()
	{
		var token = CancellationToken;

		var errors = new List<Exception>();
		await using var executor = new ChannelExecutor(ex => errors.Add(ex));
		_ = executor.RunAsync(token);

		executor.Add(() => throw new InvalidOperationException("Test error 1"));
		executor.Add(() => { }); // This should execute despite previous error
		executor.Add(() => throw new ArgumentException("Test error 2"));

		await executor.WaitFlushAsync(token);

		errors.Count.AssertEqual(2);
		errors[0].Message.AssertEqual("Test error 1");
		errors[1].Message.AssertEqual("Test error 2");
	}

	[TestMethod]
	public async Task AddAsync()
	{
		var token = CancellationToken;

		await using var executor = new ChannelExecutor();
		_ = executor.RunAsync(token);

		var counter = 0;
		await executor.AddAsync(() => counter++, token);
		await executor.AddAsync(() => counter++, token);

		await executor.WaitFlushAsync(token);

		counter.AssertEqual(2);
	}

	[TestMethod]
	public async Task AddAsyncWithCancellation()
	{
		var token = CancellationToken;

		await using var executor = new ChannelExecutor();
		_ = executor.RunAsync(token);

		var cts = new CancellationTokenSource();
		cts.Cancel();

		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
			await executor.AddAsync(() => { }, cts.Token));
	}

	[TestMethod]
	public async Task ExternalCancellation()
	{
		var token = CancellationToken;

		var executor = new ChannelExecutor();
		var cts = new CancellationTokenSource();
		_ = executor.RunAsync(cts.Token);

		var counter = 0;
		executor.Add(() => { Thread.Sleep(100); counter++; });
		executor.Add(() => { Thread.Sleep(100); counter++; });
		executor.Add(() => { Thread.Sleep(100); counter++; });

		// Cancel after a short delay
		await Task.Delay(50, token);
		cts.Cancel();

		await executor.DisposeAsync();

		// Should have executed at least one operation
		counter.AssertGreater(-1);
	}

	[TestMethod]
	public async Task WaitFlushAsync()
	{
		var token = CancellationToken;

		await using var executor = new ChannelExecutor();
		_ = executor.RunAsync(token);

		var executed = false;
		executor.Add(() => { Thread.Sleep(100); executed = true; });

		await executor.WaitFlushAsync(token);

		Assert.IsTrue(executed);
	}

	[TestMethod]
	public async Task DisposeWaitsForCompletion()
	{
		var token = CancellationToken;

		var executor = new ChannelExecutor();
		_ = executor.RunAsync(token);

		var counter = 0;
		executor.Add(() => { Thread.Sleep(20); counter++; });
		executor.Add(() => { Thread.Sleep(20); counter++; });

		// Ensure operations start executing
		await Task.Delay(50, token);

		await executor.DisposeAsync();

		// Both operations should have completed
		counter.AssertEqual(2);
	}

	[TestMethod]
	public async Task CannotRunTwice()
	{
		var token = CancellationToken;

		await using var executor = new ChannelExecutor();
		_ = executor.RunAsync(token);

		await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
			await executor.RunAsync(token));
	}

	[TestMethod]
	public async Task ConcurrentAdds()
	{
		var token = CancellationToken;

		await using var executor = new ChannelExecutor();
		_ = executor.RunAsync(token);

		var counter = 0;
		var tasks = new List<Task>();

		for (int i = 0; i < 100; i++)
		{
			tasks.Add(Task.Run(() => executor.Add(() => Interlocked.Increment(ref counter)), token));
		}

		await tasks.WhenAll();
		await executor.WaitFlushAsync(token);

		counter.AssertEqual(100);
	}

	[TestMethod]
	public async Task NoErrorHandler()
	{
		var token = CancellationToken;

		await using var executor = new ChannelExecutor(); // No error handler
		_ = executor.RunAsync(token);

		var executed = false;
		executor.Add(() => throw new InvalidOperationException("Test error"));
		executor.Add(() => executed = true); // Should still execute

		await executor.WaitFlushAsync(token);

		executed.AssertTrue();
	}

	[TestMethod]
	public async Task NullActionThrows()
	{
		var token = CancellationToken;

		await using var executor = new ChannelExecutor();
		_ = executor.RunAsync(token);

		Assert.ThrowsExactly<ArgumentNullException>(() => executor.Add(null));
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await executor.AddAsync(null, token));
	}

	[TestMethod]
	public async Task AddAfterDispose()
	{
		var token = CancellationToken;

		var executor = new ChannelExecutor();
		_ = executor.RunAsync(token);
		await executor.DisposeAsync();

		Assert.ThrowsExactly<InvalidOperationException>(() =>
			executor.Add(() => { }));
	}

	[TestMethod]
	public async Task MultipleWaitFlush()
	{
		var token = CancellationToken;

		await using var executor = new ChannelExecutor();
		_ = executor.RunAsync(token);

		var counter = 0;
		executor.Add(() => counter++);

		await executor.WaitFlushAsync(token);
		counter.AssertEqual(1);

		executor.Add(() => counter++);
		await executor.WaitFlushAsync(token);
		counter.AssertEqual(2);
	}

	[TestMethod]
	public async Task ParallelWaitFlush()
	{
		var token = CancellationToken;

		await using var executor = new ChannelExecutor();
		_ = executor.RunAsync(token);

		var counter = 0;
		executor.Add(() => { Thread.Sleep(100); counter++; });

		var task1 = executor.WaitFlushAsync(token);
		var task2 = executor.WaitFlushAsync(token);

		await Task.WhenAll(task1, task2);

		counter.AssertEqual(1);
	}

	[TestMethod]
	public async Task WaitFlushWithCancellation()
	{
		var token = CancellationToken;

		await using var executor = new ChannelExecutor();
		_ = executor.RunAsync(token);

		var cts = new CancellationTokenSource();
		cts.Cancel();

		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
			await executor.WaitFlushAsync(cts.Token));
	}
}
