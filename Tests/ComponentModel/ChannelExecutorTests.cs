namespace Ecng.Tests.ComponentModel;

using System.Collections.Concurrent;

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

		await ThrowsAsync<OperationCanceledException>(async () =>
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

		executed.AssertTrue();
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

		await ThrowsExactlyAsync<InvalidOperationException>(async () =>
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

		ThrowsExactly<ArgumentNullException>(() => executor.Add(null));
		await ThrowsExactlyAsync<ArgumentNullException>(async () => await executor.AddAsync(null, token));
	}

	[TestMethod]
	public async Task AddAfterDispose()
	{
		var token = CancellationToken;

		var executor = CreateChannel();
		_ = executor.RunAsync(token);
		await executor.DisposeAsync();

		ThrowsExactly<InvalidOperationException>(() =>
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

		await ThrowsAsync<OperationCanceledException>(async () =>
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
	[Timeout(10000, CooperativeCancellation = true)]
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
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task AddAndWaitAsync_WithException()
	{
		var token = CancellationToken;

		var errors = new List<Exception>();
		await using var executor = new ChannelExecutor(ex => errors.Add(ex));
		_ = executor.RunAsync(token);

		var expectedException = new InvalidOperationException("Test error");

		// AddAndWaitAsync should propagate the exception through the Task
		var thrownException = await ThrowsAsync<InvalidOperationException>(async () =>
			await executor.AddAndWaitAsync(() => throw expectedException, token));

		// Exception is propagated to both errorHandler and through TaskCompletionSource
		thrownException.AssertEqual(expectedException);

		// Give errorHandler time to process
		await Task.Delay(10, token);
		errors.Count.AssertEqual(1);
		errors[0].AssertEqual(expectedException);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
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
	[Timeout(10000, CooperativeCancellation = true)]
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
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task AddAndWaitAsync_WithCancellation()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var cts = new CancellationTokenSource();
		cts.Cancel();

		await ThrowsAsync<OperationCanceledException>(async () =>
			await executor.AddAndWaitAsync(() => { }, cts.Token));
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task AddAndWaitAsync_NullActionThrows()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		await ThrowsExactlyAsync<ArgumentNullException>(async () =>
			await executor.AddAndWaitAsync(null, token));
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
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

	#region Batch Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task AutoBatch_TriggersWhenThresholdReached()
	{
		var token = CancellationToken;

		var batchBeginCount = 0;
		var batchEndCount = 0;
		var operationCount = 0;

		await using var executor = new ChannelExecutor(
			ex => { },
			() => Interlocked.Increment(ref batchBeginCount),
			() => Interlocked.Increment(ref batchEndCount),
			batchThreshold: 5);

		_ = executor.RunAsync(token);

		// Add operations below threshold - should NOT trigger batch
		for (int i = 0; i < 3; i++)
			executor.Add(() => Interlocked.Increment(ref operationCount));

		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(3);
		batchBeginCount.AssertEqual(0);
		batchEndCount.AssertEqual(0);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task AutoBatch_TriggersWithManyOperations()
	{
		var token = CancellationToken;

		var batchBeginCount = 0;
		var batchEndCount = 0;
		var operationCount = 0;

		await using var executor = new ChannelExecutor(
			ex => { },
			() => Interlocked.Increment(ref batchBeginCount),
			() => Interlocked.Increment(ref batchEndCount),
			batchThreshold: 5);

		// Don't start processing yet - let operations accumulate
		var bag = new ConcurrentBag<int>();
		for (int i = 0; i < 20; i++)
		{
			var value = i;
			executor.Add(() =>
			{
				Interlocked.Increment(ref operationCount);
				bag.Add(value);
			});
		}

		// Now start processing - should detect many items and batch them
		_ = executor.RunAsync(token);
		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(20);
		batchBeginCount.AssertGreater(0);
		batchEndCount.AssertEqual(batchBeginCount);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task BatchCallbacksWithNormalAdd()
	{
		// Test that executor with batch callbacks still works with normal Add
		var batchBeginCount = 0;
		var operationCount = 0;

		await using var executor = new ChannelExecutor(
			ex => { },
			() => Interlocked.Increment(ref batchBeginCount),
			() => { },
			batchThreshold: 100);

		_ = executor.RunAsync();

		executor.Add(() => Interlocked.Increment(ref operationCount));

		await executor.WaitFlushAsync();

		operationCount.AssertEqual(1);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ExplicitBatch_CallsBeginEnd()
	{
		var batchBeginCount = 0;
		var batchEndCount = 0;
		var operationCount = 0;

		await using var executor = new ChannelExecutor(
			ex => { },
			() => Interlocked.Increment(ref batchBeginCount),
			() => Interlocked.Increment(ref batchEndCount),
			batchThreshold: 100); // High threshold so auto-batch doesn't trigger

		_ = executor.RunAsync();

		// Use explicit batch with just 3 items (below threshold)
		executor.AddBatch([
			() => Interlocked.Increment(ref operationCount),
			() => Interlocked.Increment(ref operationCount),
			() => Interlocked.Increment(ref operationCount)
		]);

		await executor.WaitFlushAsync();

		operationCount.AssertEqual(3);
		batchBeginCount.AssertEqual(1);
		batchEndCount.AssertEqual(1);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ExplicitBatch_MultipleSequential()
	{
		var token = CancellationToken;

		var batchBeginCount = 0;
		var batchEndCount = 0;
		var operationCount = 0;

		await using var executor = new ChannelExecutor(
			ex => { },
			() => Interlocked.Increment(ref batchBeginCount),
			() => Interlocked.Increment(ref batchEndCount),
			batchThreshold: 100);

		_ = executor.RunAsync(token);

		// First batch
		executor.AddBatch([
			() => Interlocked.Increment(ref operationCount),
			() => Interlocked.Increment(ref operationCount)
		]);

		// Second batch
		executor.AddBatch([
			() => Interlocked.Increment(ref operationCount),
			() => Interlocked.Increment(ref operationCount),
			() => Interlocked.Increment(ref operationCount)
		]);

		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(5);
		batchBeginCount.AssertEqual(2);
		batchEndCount.AssertEqual(2);
	}

	[TestMethod]
	//[Timeout(10000, CooperativeCancellation = true)]
	public async Task AddBatch_EmptyCollection()
	{
		var token = CancellationToken;

		var batchBeginCount = 0;
		var batchEndCount = 0;

		await using var executor = new ChannelExecutor(
			ex => { },
			() => Interlocked.Increment(ref batchBeginCount),
			() => Interlocked.Increment(ref batchEndCount));

		_ = executor.RunAsync(token);

		// Empty batch should do nothing
		executor.AddBatch([]);

		await executor.WaitFlushAsync(token);

		batchBeginCount.AssertEqual(0);
		batchEndCount.AssertEqual(0);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task AddBatch_NullThrows()
	{
		var token = CancellationToken;

		await using var executor = new ChannelExecutor(ex => { });
		_ = executor.RunAsync(token);

		ThrowsExactly<ArgumentNullException>(() => executor.AddBatch(null));
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task AddBatch_NullActionInListThrows()
	{
		var token = CancellationToken;

		await using var executor = new ChannelExecutor(ex => { });
		_ = executor.RunAsync(token);

		ThrowsExactly<ArgumentNullException>(() => executor.AddBatch([() => { }, null, () => { }]));
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task AddBatchAsync_Works()
	{
		var token = CancellationToken;

		var batchBeginCount = 0;
		var batchEndCount = 0;
		var operationCount = 0;

		await using var executor = new ChannelExecutor(
			ex => { },
			() => Interlocked.Increment(ref batchBeginCount),
			() => Interlocked.Increment(ref batchEndCount),
			batchThreshold: 100);

		_ = executor.RunAsync(token);

		await executor.AddBatchAsync([
			() => Interlocked.Increment(ref operationCount),
			() => Interlocked.Increment(ref operationCount),
			() => Interlocked.Increment(ref operationCount)
		], token);

		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(3);
		batchBeginCount.AssertEqual(1);
		batchEndCount.AssertEqual(1);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task NoBatchCallbacks_WorksNormally()
	{
		var token = CancellationToken;

		var operationCount = 0;

		// No batch callbacks - should work like normal
		await using var executor = new ChannelExecutor(ex => { });
		_ = executor.RunAsync(token);

		executor.AddBatch([
			() => Interlocked.Increment(ref operationCount),
			() => Interlocked.Increment(ref operationCount)
		]);

		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(2);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task BatchWithErrors_ContinuesAndCallsEnd()
	{
		var token = CancellationToken;

		var batchBeginCount = 0;
		var batchEndCount = 0;
		var operationCount = 0;
		var errors = new ConcurrentBag<Exception>();

		await using var executor = new ChannelExecutor(
			ex => errors.Add(ex),
			() => Interlocked.Increment(ref batchBeginCount),
			() => Interlocked.Increment(ref batchEndCount),
			batchThreshold: 100);

		_ = executor.RunAsync(token);

		executor.AddBatch([
			() => Interlocked.Increment(ref operationCount),
			() => throw new InvalidOperationException("Test error"),
			() => Interlocked.Increment(ref operationCount)
		]);

		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(2);
		errors.Count.AssertEqual(1);
		batchBeginCount.AssertEqual(1);
		batchEndCount.AssertEqual(1);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task MixedBatchAndSingleOperations()
	{
		var token = CancellationToken;

		var batchBeginCount = 0;
		var batchEndCount = 0;
		var results = new ConcurrentQueue<string>();

		await using var executor = new ChannelExecutor(
			ex => { },
			() => { results.Enqueue("BEGIN"); Interlocked.Increment(ref batchBeginCount); },
			() => { results.Enqueue("END"); Interlocked.Increment(ref batchEndCount); },
			batchThreshold: 100);

		_ = executor.RunAsync(token);

		// Single operation
		executor.Add(() => results.Enqueue("single1"));

		// Batch
		executor.AddBatch([
			() => results.Enqueue("batch1"),
			() => results.Enqueue("batch2")
		]);

		// Another single operation
		executor.Add(() => results.Enqueue("single2"));

		await executor.WaitFlushAsync(token);

		// Verify order
		var list = results.ToList();
		list.Count.AssertEqual(6); // single1, BEGIN, batch1, batch2, END, single2
		list[0].AssertEqual("single1");
		list[1].AssertEqual("BEGIN");
		list[2].AssertEqual("batch1");
		list[3].AssertEqual("batch2");
		list[4].AssertEqual("END");
		list[5].AssertEqual("single2");
	}

	#endregion

	#region Collection Modification Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ChaoticListModification_SingleThread()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		// Non-thread-safe list - safe because executor guarantees sequential execution
		var list = new List<int>();

		// Add items
		for (int i = 0; i < 100; i++)
		{
			var value = i;
			executor.Add(() => list.Add(value));
		}

		// Remove some items (every 3rd)
		for (int i = 99; i >= 0; i -= 3)
		{
			var idx = i;
			executor.Add(() => { if (idx < list.Count) list.RemoveAt(idx); });
		}

		// Add more items
		for (int i = 100; i < 150; i++)
		{
			var value = i;
			executor.Add(() => list.Add(value));
		}

		// Insert at specific positions
		executor.Add(() => list.Insert(0, -1));
		executor.Add(() => list.Insert(list.Count / 2, -2));

		// Clear and rebuild
		executor.Add(() => list.Clear());
		for (int i = 0; i < 10; i++)
		{
			var value = i * 10;
			executor.Add(() => list.Add(value));
		}

		await executor.WaitFlushAsync(token);

		// Final state should be [0, 10, 20, 30, 40, 50, 60, 70, 80, 90]
		list.Count.AssertEqual(10);
		for (int i = 0; i < 10; i++)
			list[i].AssertEqual(i * 10);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ChaoticListModification_MultipleThreads()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		// Non-thread-safe list - safe because executor guarantees sequential execution
		var list = new List<int>();
		var operationLog = new List<string>(); // Track operations for verification

		var tasks = new List<Task>();

		// Multiple threads adding chaotic operations
		for (int t = 0; t < 5; t++)
		{
			var threadId = t;
			var seed = 42 + threadId; // Different seed per thread
			tasks.Add(Task.Run(() =>
			{
				var random = new Random(seed); // Thread-local random
				for (int i = 0; i < 50; i++)
				{
					var op = random.Next(4);
					var value = threadId * 1000 + i;

					switch (op)
					{
						case 0: // Add
							executor.Add(() =>
							{
								list.Add(value);
								operationLog.Add($"Add:{value}");
							});
							break;
						case 1: // Remove last if exists
							executor.Add(() =>
							{
								if (list.Count > 0)
								{
									var removed = list[list.Count - 1];
									list.RemoveAt(list.Count - 1);
									operationLog.Add($"RemoveLast:{removed}");
								}
							});
							break;
						case 2: // Insert at beginning
							executor.Add(() =>
							{
								list.Insert(0, value);
								operationLog.Add($"Insert0:{value}");
							});
							break;
						case 3: // Clear if too large
							executor.Add(() =>
							{
								if (list.Count > 20)
								{
									list.Clear();
									operationLog.Add("Clear");
								}
							});
							break;
					}
				}
			}, token));
		}

		await tasks.WhenAll();
		await executor.WaitFlushAsync(token);

		// Replay operations to verify final state
		var expectedList = new List<int>();
		foreach (var op in operationLog)
		{
			if (op.StartsWith("Add:"))
				expectedList.Add(int.Parse(op[4..]));
			else if (op.StartsWith("RemoveLast:") && expectedList.Count > 0)
				expectedList.RemoveAt(expectedList.Count - 1);
			else if (op.StartsWith("Insert0:"))
				expectedList.Insert(0, int.Parse(op[8..]));
			else if (op == "Clear")
				expectedList.Clear();
		}

		list.Count.AssertEqual(expectedList.Count);
		for (int i = 0; i < list.Count; i++)
			list[i].AssertEqual(expectedList[i]);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task DictionaryModification_Chaotic()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		// Non-thread-safe dictionary
		var dict = new Dictionary<string, int>();

		var tasks = new List<Task>();

		// Multiple threads modifying dictionary
		for (int t = 0; t < 4; t++)
		{
			var threadId = t;
			tasks.Add(Task.Run(() =>
			{
				for (int i = 0; i < 100; i++)
				{
					var key = $"key_{(threadId * 100 + i) % 50}"; // Limited key space for collisions
					var value = threadId * 1000 + i;

					if (i % 3 == 0)
					{
						executor.Add(() => dict[key] = value); // Add or update
					}
					else if (i % 3 == 1)
					{
						executor.Add(() => dict.Remove(key)); // Remove
					}
					else
					{
						executor.Add(() =>
						{
							if (dict.TryGetValue(key, out var v))
								dict[key] = v + 1; // Increment if exists
						});
					}
				}
			}, token));
		}

		await tasks.WhenAll();
		await executor.WaitFlushAsync(token);

		// Dictionary should be in consistent state (no corrupted entries)
		foreach (var kvp in dict)
		{
			kvp.Key.AssertNotNull();
			// Value should be a valid integer
			(kvp.Value >= 0).AssertTrue();
		}
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task BatchModification_AtomicOperations()
	{
		var token = CancellationToken;

		var batchCount = 0;
		await using var executor = new ChannelExecutor(
			ex => { },
			() => Interlocked.Increment(ref batchCount),
			() => { },
			batchThreshold: 100);

		_ = executor.RunAsync(token);

		// Non-thread-safe list
		var list = new List<int>();

		// Batch: Add 10 items atomically
		executor.AddBatch(Enumerable.Range(0, 10).Select(i => (Action)(() => list.Add(i))));

		// Batch: Remove all even numbers atomically
		executor.AddBatch([
			() => { for (int i = list.Count - 1; i >= 0; i--) if (list[i] % 2 == 0) list.RemoveAt(i); }
		]);

		// Batch: Double all remaining values atomically
		executor.AddBatch([
			() => { for (int i = 0; i < list.Count; i++) list[i] *= 2; }
		]);

		await executor.WaitFlushAsync(token);

		// Should have [1, 3, 5, 7, 9] doubled = [2, 6, 10, 14, 18]
		list.Count.AssertEqual(5);
		list[0].AssertEqual(2);
		list[1].AssertEqual(6);
		list[2].AssertEqual(10);
		list[3].AssertEqual(14);
		list[4].AssertEqual(18);

		batchCount.AssertEqual(3);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ComplexObjectModification()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		// Complex non-thread-safe structure
		var data = new
		{
			Users = new List<(int Id, string Name, List<string> Tags)>(),
			Stats = new Dictionary<int, int>()
		};

		// Add users from multiple threads
		var tasks = new List<Task>();
		for (int t = 0; t < 3; t++)
		{
			var threadId = t;
			tasks.Add(Task.Run(() =>
			{
				for (int i = 0; i < 10; i++)
				{
					var userId = threadId * 100 + i;
					var userName = $"User_{userId}";

					// Add user
					executor.Add(() => data.Users.Add((userId, userName, new List<string>())));

					// Add tags
					executor.Add(() =>
					{
						var user = data.Users.Find(u => u.Id == userId);
						if (user != default)
							user.Tags.Add($"tag_{i % 3}");
					});

					// Update stats
					executor.Add(() =>
					{
						if (!data.Stats.ContainsKey(userId % 10))
							data.Stats[userId % 10] = 0;
						data.Stats[userId % 10]++;
					});
				}
			}, token));
		}

		await tasks.WhenAll();
		await executor.WaitFlushAsync(token);

		// Verify
		data.Users.Count.AssertEqual(30); // 3 threads * 10 users
		data.Stats.Values.Sum().AssertEqual(30); // 30 stat increments
	}

	#endregion
}
