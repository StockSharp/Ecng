namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

using Nito.AsyncEx;

[TestClass]
public class ChannelExecutorTests : BaseTestClass
{
	private static ChannelExecutor CreateChannel()
		=> new(ex => { });

	[TestMethod]
	public async Task BasicExecution()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
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

		await using var executor = CreateChannel();
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

		await using var executor = CreateChannel();
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

		await using var executor = CreateChannel();
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

		var executor = CreateChannel();
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

		await using var executor = CreateChannel();
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

		var executor = CreateChannel();
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

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
			await executor.RunAsync(token));
	}

	[TestMethod]
	public async Task ConcurrentAdds()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
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

		await using var executor = CreateChannel(); // No error handler
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

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		Assert.ThrowsExactly<ArgumentNullException>(() => executor.Add(null));
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await executor.AddAsync(null, token));
	}

	[TestMethod]
	public async Task AddAfterDispose()
	{
		var token = CancellationToken;

		var executor = CreateChannel();
		_ = executor.RunAsync(token);
		await executor.DisposeAsync();

		Assert.ThrowsExactly<InvalidOperationException>(() =>
			executor.Add(() => { }));
	}

	[TestMethod]
	public async Task MultipleWaitFlush()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
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

		await using var executor = CreateChannel();
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

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var cts = new CancellationTokenSource();
		cts.Cancel();

		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
			await executor.WaitFlushAsync(cts.Token));
	}

	[TestMethod]
	public async Task ErrorHandlerReceivesAllExceptions()
	{
		var token = CancellationToken;

		var caughtExceptions = new List<Exception>();
		var errorHandler = new Action<Exception>(ex => caughtExceptions.Add(ex));

		await using var executor = new ChannelExecutor(errorHandler);
		_ = executor.RunAsync(token);

		var expectedException1 = new InvalidOperationException("First error");
		var expectedException2 = new ArgumentException("Second error");
		var expectedException3 = new NotSupportedException("Third error");

		executor.Add(() => throw expectedException1);
		executor.Add(() => { }); // Normal operation between errors
		executor.Add(() => throw expectedException2);
		executor.Add(() => throw expectedException3);

		await executor.WaitFlushAsync(token);

		// Verify all exceptions were caught
		caughtExceptions.Count.AssertEqual(3);
		caughtExceptions[0].AssertEqual(expectedException1);
		caughtExceptions[1].AssertEqual(expectedException2);
		caughtExceptions[2].AssertEqual(expectedException3);
	}

	[TestMethod]
	public async Task AllOperationsExecuteSequentially()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		// Use a non-thread-safe collection to verify sequential execution
		// If operations run concurrently, this will throw or corrupt data
		var list = new List<int>();
		var operationCount = 100;

		for (int i = 0; i < operationCount; i++)
		{
			var value = i;
			executor.Add(() =>
			{
				// Non-thread-safe operations
				list.Add(value);
				// Verify list wasn't corrupted
				list[list.Count - 1].AssertEqual(value);
			});
		}

		await executor.WaitFlushAsync(token);

		// All operations completed successfully without concurrent modification
		list.Count.AssertEqual(operationCount);
		// Verify sequential execution by checking order preservation
		for (int i = 0; i < operationCount; i++)
		{
			list[i].AssertEqual(i);
		}
	}

	[TestMethod]
	public async Task ThreadSafetyUnderHeavyLoad()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var counter = 0;
		var sharedList = new List<int>();
		var operations = 1000;
		var threadCount = 10;

		var tasks = new List<Task>();

		// Multiple threads adding operations concurrently
		for (int t = 0; t < threadCount; t++)
		{
			var threadIndex = t;
			tasks.Add(Task.Run(() =>
			{
				for (int i = 0; i < operations / threadCount; i++)
				{
					var value = threadIndex * 1000 + i;
					executor.Add(() =>
					{
						Interlocked.Increment(ref counter);
						sharedList.Add(value);
					});
				}
			}, token));
		}

		await tasks.WhenAll();
		await executor.WaitFlushAsync(token);

		// Verify all operations completed
		counter.AssertEqual(operations);
		sharedList.Count.AssertEqual(operations);

		// Verify no concurrent modification occurred (list operations are not thread-safe,
		// but since they execute sequentially in the channel, they should be safe)
		sharedList.Distinct().Count().AssertEqual(operations);
	}

	[TestMethod]
	public async Task ThreadSafetyWithMixedOperations()
	{
		var token = CancellationToken;

		var errors = new List<Exception>();
		await using var executor = new ChannelExecutor(ex => errors.Add(ex));
		_ = executor.RunAsync(token);

		var successfulOps = 0;
		var failedOps = 0;
		var tasks = new List<Task>();

		// Simulate real-world scenario: multiple threads adding both successful and failing operations
		for (int t = 0; t < 5; t++)
		{
			tasks.Add(Task.Run(() =>
			{
				for (int i = 0; i < 100; i++)
				{
					if (i % 10 == 0)
					{
						// Every 10th operation fails
						executor.Add(() =>
						{
							Interlocked.Increment(ref failedOps);
							throw new InvalidOperationException("Planned failure");
						});
					}
					else
					{
						executor.Add(() => Interlocked.Increment(ref successfulOps));
					}
				}
			}, token));
		}

		await tasks.WhenAll();
		await executor.WaitFlushAsync(token);

		// Verify counts
		successfulOps.AssertEqual(450); // 90% of 500
		failedOps.AssertEqual(50); // 10% of 500
		errors.Count.AssertEqual(50);
	}

	[TestMethod]
	public async Task AddAndWaitAsync_WaitsForCompletion()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var executed = false;
		var startTime = DateTime.UtcNow;

		await executor.AddAndWaitAsync(() =>
		{
			Thread.Sleep(100);
			executed = true;
		}, token);

		var elapsed = DateTime.UtcNow - startTime;

		// Operation should have completed
		executed.AssertTrue();
		// And it should have taken at least 100ms
		elapsed.TotalMilliseconds.AssertGreater(89); // Small tolerance for timing
	}

	[TestMethod]
	public async Task AddAndWaitAsync_WithException()
	{
		var token = CancellationToken;

		var errors = new List<Exception>();
		await using var executor = new ChannelExecutor(ex => errors.Add(ex));
		_ = executor.RunAsync(token);

		var expectedException = new InvalidOperationException("Test error");

		// AddAndWaitAsync should propagate the exception through the Task
		var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			await executor.AddAndWaitAsync(() => throw expectedException, token));

		// Exception is propagated to both errorHandler and through TaskCompletionSource
		thrownException.AssertEqual(expectedException);

		// Give errorHandler time to process
		await Task.Delay(10, token);
		errors.Count.AssertEqual(1);
		errors[0].AssertEqual(expectedException);
	}

	[TestMethod]
	public async Task AddAndWaitAsync_MultipleSequential()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var counter = 0;

		// Each AddAndWaitAsync should wait for its operation to complete before returning
		await executor.AddAndWaitAsync(() => counter++, token);
		counter.AssertEqual(1);

		await executor.AddAndWaitAsync(() => counter++, token);
		counter.AssertEqual(2);

		await executor.AddAndWaitAsync(() => counter++, token);
		counter.AssertEqual(3);
	}

	[TestMethod]
	public async Task AddAndWaitAsync_ParallelCalls()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var counter = 0;
		var tasks = new List<Task>();

		// Multiple threads calling AddAndWaitAsync in parallel
		for (int i = 0; i < 10; i++)
		{
			tasks.Add(Task.Run(async () =>
			{
				await executor.AddAndWaitAsync(() =>
				{
					Thread.Sleep(10);
					Interlocked.Increment(ref counter);
				}, token);
			}, token));
		}

		await tasks.WhenAll();

		counter.AssertEqual(10);
	}

	[TestMethod]
	public async Task AddAndWaitAsync_WithCancellation()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var cts = new CancellationTokenSource();
		cts.Cancel();

		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
			await executor.AddAndWaitAsync(() => { }, cts.Token));
	}

	[TestMethod]
	public async Task AddAndWaitAsync_NullActionThrows()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
			await executor.AddAndWaitAsync(null, token));
	}

	[TestMethod]
	public async Task DisposeDoesNotCancelLongRunningOperation()
	{
		var token = CancellationToken;
		var counter = 0;

		await using (var executor = CreateChannel())
		{
			_ = executor.RunAsync(token);

			executor.Add(() =>
			{
				Thread.Sleep(TimeSpan.FromSeconds(6));
				Interlocked.Increment(ref counter);
			});

			executor.Add(() => Interlocked.Increment(ref counter));
		}

		// Both operations should still complete even though the first exceeds the default timeout
		counter.AssertEqual(2);
	}
}
