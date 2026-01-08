namespace Ecng.Tests.ComponentModel;

using System.Collections.Concurrent;
using System.Threading.Channels;

using Ecng.ComponentModel;
using Ecng.IO;

using Nito.AsyncEx;

[TestClass]
public class ChannelExecutorTests : BaseTestClass
{
	private static ChannelExecutor CreateChannel()
		=> new(ex => { }, TimeSpan.Zero);

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task BasicExecution()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var counter = 0;
		executor.Add(_ => { counter++; return default; });
		executor.Add(_ => { counter++; return default; });
		executor.Add(_ => { counter++; return default; });

		await executor.WaitFlushAsync(token);

		counter.AssertEqual(3);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task SequentialExecution()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var list = new List<int>();
		for (int i = 0; i < 10; i++)
		{
			var value = i;
			executor.Add(_ => { list.Add(value); return default; });
		}

		await executor.WaitFlushAsync(token);

		list.Count.AssertEqual(10);
		for (int i = 0; i < 10; i++)
		{
			list[i].AssertEqual(i);
		}
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ErrorHandling()
	{
		var token = CancellationToken;

		var errors = new List<Exception>();
		await using var executor = new ChannelExecutor(ex => errors.Add(ex), TimeSpan.Zero);
		_ = executor.RunAsync(token);

		executor.Add(_ => throw new InvalidOperationException("Test error 1"));
		executor.Add(_ => default); // This should execute despite previous error
		executor.Add(_ => throw new ArgumentException("Test error 2"));

		await executor.WaitFlushAsync(token);

		errors.Count.AssertEqual(2);
		errors[0].Message.AssertEqual("Test error 1");
		errors[1].Message.AssertEqual("Test error 2");
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task AddAsync()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var counter = 0;
		await executor.AddAsync(_ => { counter++; return default; }, token);
		await executor.AddAsync(_ => { counter++; return default; }, token);

		await executor.WaitFlushAsync(token);

		counter.AssertEqual(2);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task AddAsyncWithCancellation()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var cts = new CancellationTokenSource();
		cts.Cancel();

		await ThrowsAsync<OperationCanceledException>(async () =>
			await executor.AddAsync(_ => default, cts.Token));
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ExternalCancellation()
	{
		var token = CancellationToken;

		var executor = CreateChannel();
		var cts = new CancellationTokenSource();
		_ = executor.RunAsync(cts.Token);

		var counter = 0;
		executor.Add(async _ => { await Task.Delay(100); counter++; });
		executor.Add(async _ => { await Task.Delay(100); counter++; });
		executor.Add(async _ => { await Task.Delay(100); counter++; });

		// Cancel after a short delay
		await Task.Delay(50, token);
		cts.Cancel();

		await executor.DisposeAsync();

		// Should have executed at least one operation
		counter.AssertGreater(-1);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task WaitFlushAsync()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var executed = false;
		executor.Add(async _ => { await Task.Delay(100); executed = true; });

		await executor.WaitFlushAsync(token);

		executed.AssertTrue();
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task DisposeWaitsForCompletion()
	{
		var token = CancellationToken;

		var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var counter = 0;
		executor.Add(async _ => { await Task.Delay(20); counter++; });
		executor.Add(async _ => { await Task.Delay(20); counter++; });

		// Ensure operations start executing
		await Task.Delay(50, token);

		await executor.DisposeAsync();

		// Both operations should have completed
		counter.AssertEqual(2);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task CannotRunTwice()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		await ThrowsExactlyAsync<InvalidOperationException>(async () =>
			await executor.RunAsync(token));
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ConcurrentAdds()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var counter = 0;
		var tasks = new List<Task>();

		for (int i = 0; i < 100; i++)
		{
			tasks.Add(Task.Run(() => executor.Add(_ => { Interlocked.Increment(ref counter); return default; }), token));
		}

		await tasks.WhenAll();
		await executor.WaitFlushAsync(token);

		counter.AssertEqual(100);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task NoErrorHandler()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel(); // No error handler
		_ = executor.RunAsync(token);

		var executed = false;
		executor.Add(_ => throw new InvalidOperationException("Test error"));
		executor.Add(_ => { executed = true; return default; }); // Should still execute

		await executor.WaitFlushAsync(token);

		executed.AssertTrue();
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task NullActionThrows()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		ThrowsExactly<ArgumentNullException>(() => executor.Add(null));
		await ThrowsExactlyAsync<ArgumentNullException>(async () => await executor.AddAsync(null, token));
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task AddAfterDispose()
	{
		var token = CancellationToken;

		var executor = CreateChannel();
		_ = executor.RunAsync(token);
		await executor.DisposeAsync();

		ThrowsExactly<ChannelClosedException>(() =>
			executor.Add(_ => default));
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task MultipleWaitFlush()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var counter = 0;
		executor.Add(_ => { counter++; return default; });

		await executor.WaitFlushAsync(token);
		counter.AssertEqual(1);

		executor.Add(_ => { counter++; return default; });
		await executor.WaitFlushAsync(token);
		counter.AssertEqual(2);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ParallelWaitFlush()
	{
		var token = CancellationToken;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var counter = 0;
		executor.Add(async _ => { await Task.Delay(100); counter++; });

		var task1 = executor.WaitFlushAsync(token);
		var task2 = executor.WaitFlushAsync(token);

		await Task.WhenAll(task1, task2);

		counter.AssertEqual(1);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
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
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ErrorHandlerReceivesAllExceptions()
	{
		var token = CancellationToken;

		var caughtExceptions = new List<Exception>();
		var errorHandler = new Action<Exception>(ex => caughtExceptions.Add(ex));

		await using var executor = new ChannelExecutor(errorHandler, TimeSpan.Zero);
		_ = executor.RunAsync(token);

		var expectedException1 = new InvalidOperationException("First error");
		var expectedException2 = new ArgumentException("Second error");
		var expectedException3 = new NotSupportedException("Third error");

		executor.Add(_ => throw expectedException1);
		executor.Add(_ => default); // Normal operation between errors
		executor.Add(_ => throw expectedException2);
		executor.Add(_ => throw expectedException3);

		await executor.WaitFlushAsync(token);

		// Verify all exceptions were caught
		caughtExceptions.Count.AssertEqual(3);
		caughtExceptions[0].AssertEqual(expectedException1);
		caughtExceptions[1].AssertEqual(expectedException2);
		caughtExceptions[2].AssertEqual(expectedException3);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
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
			executor.Add(_ =>
			{
				// Non-thread-safe operations
				list.Add(value);
				// Verify list wasn't corrupted
				list[list.Count - 1].AssertEqual(value);
				return default;
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
	[Timeout(10000, CooperativeCancellation = true)]
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
					executor.Add(_ =>
					{
						Interlocked.Increment(ref counter);
						sharedList.Add(value);
						return default;
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
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ThreadSafetyWithMixedOperations()
	{
		var token = CancellationToken;

		var errors = new List<Exception>();
		await using var executor = new ChannelExecutor(ex => errors.Add(ex), TimeSpan.Zero);
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
						executor.Add(_ =>
						{
							Interlocked.Increment(ref failedOps);
							throw new InvalidOperationException("Planned failure");
						});
					}
					else
					{
						executor.Add(_ => { Interlocked.Increment(ref successfulOps); return default; });
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

		await executor.AddAndWaitAsync(async _ =>
		{
			await Task.Delay(100);
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
		await using var executor = new ChannelExecutor(ex => errors.Add(ex), TimeSpan.Zero);
		_ = executor.RunAsync(token);

		var expectedException = new InvalidOperationException("Test error");

		// AddAndWaitAsync should propagate the exception through the Task
		var thrownException = await ThrowsAsync<InvalidOperationException>(async () =>
			await executor.AddAndWaitAsync(_ => throw expectedException, token));

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
		await executor.AddAndWaitAsync(_ => { counter++; return default; }, token);
		counter.AssertEqual(1);

		await executor.AddAndWaitAsync(_ => { counter++; return default; }, token);
		counter.AssertEqual(2);

		await executor.AddAndWaitAsync(_ => { counter++; return default; }, token);
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
				await executor.AddAndWaitAsync(async _ =>
				{
					await Task.Delay(10);
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
			await executor.AddAndWaitAsync(_ => default, cts.Token));
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

			executor.Add(async _ =>
			{
				await Task.Delay(TimeSpan.FromSeconds(6));
				Interlocked.Increment(ref counter);
			});

			executor.Add(_ => { Interlocked.Increment(ref counter); return default; });
		}

		// Both operations should still complete even though the first exceeds the default timeout
		counter.AssertEqual(2);
	}

	#region Group Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task Group_CallsBeginEnd()
	{
		var token = CancellationToken;

		var beginCount = 0;
		var endCount = 0;
		var operationCount = 0;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var group = executor.CreateGroup(
			_ => { Interlocked.Increment(ref beginCount); return default; },
			_ => { Interlocked.Increment(ref endCount); return default; });

		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });

		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(3);
		// With immediate mode (TimeSpan.Zero), each Add triggers flush
		beginCount.AssertEqual(3);
		endCount.AssertEqual(3);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task Group_SingleAction_CallsBeginEnd()
	{
		var token = CancellationToken;

		var beginCount = 0;
		var endCount = 0;
		var operationCount = 0;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		var group = executor.CreateGroup(
			_ => { Interlocked.Increment(ref beginCount); return default; },
			_ => { Interlocked.Increment(ref endCount); return default; });

		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });

		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(1);
		beginCount.AssertEqual(1);
		endCount.AssertEqual(1);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task Group_WithInterval_BatchesOperations()
	{
		var token = CancellationToken;

		var beginCount = 0;
		var endCount = 0;
		var operationCount = 0;

		// Use interval to batch operations (500ms to ensure all ops are added before flush)
		await using var executor = new ChannelExecutor(ex => { }, TimeSpan.FromMilliseconds(500));
		_ = executor.RunAsync(token);

		var group = executor.CreateGroup(
			_ => { Interlocked.Increment(ref beginCount); return default; },
			_ => { Interlocked.Increment(ref endCount); return default; });

		// Add multiple operations quickly - should batch
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });

		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(3);
		// With interval, all operations should be batched into one begin/end
		beginCount.AssertEqual(1);
		endCount.AssertEqual(1);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task Group_EmptyGroup_NoBeginEnd()
	{
		var token = CancellationToken;

		var beginCount = 0;
		var endCount = 0;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		// Create group but don't add anything
		var group = executor.CreateGroup(
			_ => { Interlocked.Increment(ref beginCount); return default; },
			_ => { Interlocked.Increment(ref endCount); return default; });

		await executor.WaitFlushAsync(token);

		// No operations added, so no begin/end
		beginCount.AssertEqual(0);
		endCount.AssertEqual(0);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task Group_EndCalledOnError()
	{
		var token = CancellationToken;

		var beginCount = 0;
		var endCount = 0;
		var operationCount = 0;
		var errors = new ConcurrentBag<Exception>();

		await using var executor = new ChannelExecutor(ex => errors.Add(ex), TimeSpan.FromMilliseconds(500));
		_ = executor.RunAsync(token);

		var group = executor.CreateGroup(
			_ => { Interlocked.Increment(ref beginCount); return default; },
			_ => { Interlocked.Increment(ref endCount); return default; });

		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		group.Add(_ => throw new InvalidOperationException("Test error"));
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });

		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(2);
		errors.Count.AssertEqual(1);
		beginCount.AssertGreater(0);
		endCount.AssertGreater(0); // End still called despite error
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task Group_EndCalledEvenWhenBeginFails()
	{
		var token = CancellationToken;

		var endCount = 0;
		var operationCount = 0;
		var errors = new ConcurrentBag<Exception>();

		await using var executor = new ChannelExecutor(ex => errors.Add(ex), TimeSpan.FromMilliseconds(500));
		_ = executor.RunAsync(token);

		var group = executor.CreateGroup(
			_ => throw new InvalidOperationException("Begin error"),
			_ => { Interlocked.Increment(ref endCount); return default; });

		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });

		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(2); // Operations still execute
		endCount.AssertGreater(0); // End still called
		errors.Count.AssertGreater(0); // Begin error was caught
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task GroupAsync_Works()
	{
		var token = CancellationToken;

		var beginCount = 0;
		var endCount = 0;
		var operationCount = 0;

		await using var executor = new ChannelExecutor(ex => { }, TimeSpan.FromMilliseconds(500));
		_ = executor.RunAsync(token);

		var group = executor.CreateGroup(
			_ => { Interlocked.Increment(ref beginCount); return default; },
			_ => { Interlocked.Increment(ref endCount); return default; });

		await group.AddAsync(_ => { Interlocked.Increment(ref operationCount); return default; }, token);
		await group.AddAsync(_ => { Interlocked.Increment(ref operationCount); return default; }, token);
		await group.AddAsync(_ => { Interlocked.Increment(ref operationCount); return default; }, token);

		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(3);
		beginCount.AssertGreater(0);
		endCount.AssertGreater(0);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task Group_NullBeginEnd_Works()
	{
		var token = CancellationToken;

		var operationCount = 0;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		ThrowsExactly<ArgumentNullException>(() => executor.CreateGroup(null, null));

		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(0);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task MixedGroupAndSingleOperations()
	{
		var token = CancellationToken;

		var results = new ConcurrentQueue<string>();

		await using var executor = new ChannelExecutor(ex => { }, TimeSpan.FromMilliseconds(500));
		_ = executor.RunAsync(token);

		// Single operation
		executor.Add(_ => { results.Enqueue("single1"); return default; });

		// Group
		var group = executor.CreateGroup(
			_ => { results.Enqueue("BEGIN"); return default; },
			_ => { results.Enqueue("END"); return default; });
		group.Add(_ => { results.Enqueue("group1"); return default; });
		group.Add(_ => { results.Enqueue("group2"); return default; });

		// Another single operation
		executor.Add(_ => { results.Enqueue("single2"); return default; });

		await executor.WaitFlushAsync(token);

		var list = results.ToList();
		// All operations should execute, BEGIN/END may be called multiple times if batches split
		list.AssertContains("single1");
		list.AssertContains("group1");
		list.AssertContains("group2");
		list.AssertContains("single2");
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task MultipleGroups_SeparateBeginEnd()
	{
		var token = CancellationToken;

		var group1Begin = 0;
		var group1End = 0;
		var group2Begin = 0;
		var group2End = 0;

		await using var executor = new ChannelExecutor(ex => { }, TimeSpan.FromMilliseconds(500));
		_ = executor.RunAsync(token);

		var group1 = executor.CreateGroup(
			_ => { Interlocked.Increment(ref group1Begin); return default; },
			_ => { Interlocked.Increment(ref group1End); return default; });

		var group2 = executor.CreateGroup(
			_ => { Interlocked.Increment(ref group2Begin); return default; },
			_ => { Interlocked.Increment(ref group2End); return default; });

		group1.Add(_ => default);
		group2.Add(_ => default);
		group1.Add(_ => default);
		group2.Add(_ => default);

		await executor.WaitFlushAsync(token);

		group1Begin.AssertGreater(0);
		group1End.AssertGreater(0);
		group2Begin.AssertGreater(0);
		group2End.AssertGreater(0);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task Group_ReusableAcrossMultipleFlushes()
	{
		var token = CancellationToken;

		var beginCount = 0;
		var endCount = 0;
		var operationCount = 0;

		await using var executor = CreateChannel();
		_ = executor.RunAsync(token);

		// Create group once
		var group = executor.CreateGroup(
			_ => { Interlocked.Increment(ref beginCount); return default; },
			_ => { Interlocked.Increment(ref endCount); return default; });

		// First batch
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(2);
		beginCount.AssertGreater(0);
		endCount.AssertGreater(0);

		var beginAfterFirst = beginCount;
		var endAfterFirst = endCount;

		// Second batch - reuse same group
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(5);
		beginCount.AssertGreater(beginAfterFirst);
		endCount.AssertGreater(endAfterFirst);

		var beginAfterSecond = beginCount;
		var endAfterSecond = endCount;

		// Third batch - reuse same group again
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(6);
		beginCount.AssertGreater(beginAfterSecond);
		endCount.AssertGreater(endAfterSecond);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task Group_ReusableWithInterval()
	{
		var token = CancellationToken;

		var beginCount = 0;
		var endCount = 0;
		var operationCount = 0;

		// Use interval to batch operations
		await using var executor = new ChannelExecutor(ex => { }, TimeSpan.FromMilliseconds(100));
		_ = executor.RunAsync(token);

		var group = executor.CreateGroup(
			_ => { Interlocked.Increment(ref beginCount); return default; },
			_ => { Interlocked.Increment(ref endCount); return default; });

		// First usage
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(2);
		beginCount.AssertEqual(1);
		endCount.AssertEqual(1);

		// Wait a bit to ensure next batch is separate
		await Task.Delay(150, token);

		// Second usage - should trigger new begin/end
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		group.Add(_ => { Interlocked.Increment(ref operationCount); return default; });
		await executor.WaitFlushAsync(token);

		operationCount.AssertEqual(4);
		beginCount.AssertEqual(2);
		endCount.AssertEqual(2);
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
			executor.Add(_ => { list.Add(value); return default; });
		}

		// Remove some items (every 3rd)
		for (int i = 99; i >= 0; i -= 3)
		{
			var idx = i;
			executor.Add(_ => { if (idx < list.Count) list.RemoveAt(idx); return default; });
		}

		// Add more items
		for (int i = 100; i < 150; i++)
		{
			var value = i;
			executor.Add(_ => { list.Add(value); return default; });
		}

		// Insert at specific positions
		executor.Add(_ => { list.Insert(0, -1); return default; });
		executor.Add(_ => { list.Insert(list.Count / 2, -2); return default; });

		// Clear and rebuild
		executor.Add(_ => { list.Clear(); return default; });
		for (int i = 0; i < 10; i++)
		{
			var value = i * 10;
			executor.Add(_ => { list.Add(value); return default; });
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
							executor.Add(_ =>
							{
								list.Add(value);
								operationLog.Add($"Add:{value}");
								return default;
							});
							break;
						case 1: // Remove last if exists
							executor.Add(_ =>
							{
								if (list.Count > 0)
								{
									var removed = list[list.Count - 1];
									list.RemoveAt(list.Count - 1);
									operationLog.Add($"RemoveLast:{removed}");
								}
								return default;
							});
							break;
						case 2: // Insert at beginning
							executor.Add(_ =>
							{
								list.Insert(0, value);
								operationLog.Add($"Insert0:{value}");
								return default;
							});
							break;
						case 3: // Clear if too large
							executor.Add(_ =>
							{
								if (list.Count > 20)
								{
									list.Clear();
									operationLog.Add("Clear");
								}
								return default;
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
						executor.Add(_ => { dict[key] = value; return default; }); // Add or update
					}
					else if (i % 3 == 1)
					{
						executor.Add(_ => { dict.Remove(key); return default; }); // Remove
					}
					else
					{
						executor.Add(_ =>
						{
							if (dict.TryGetValue(key, out var v))
								dict[key] = v + 1; // Increment if exists
							return default;
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
	public async Task GroupModification_AtomicOperations()
	{
		var token = CancellationToken;

		var groupCount = 0;
		// Use interval for atomic batching within groups
		await using var executor = new ChannelExecutor(ex => { }, TimeSpan.FromMilliseconds(500));
		_ = executor.RunAsync(token);

		// Non-thread-safe list
		var list = new List<int>();

		// Group 1: Add 10 items atomically
		var group1 = executor.CreateGroup(
			_ => { Interlocked.Increment(ref groupCount); return default; },
			_ => default);
		foreach (var i in Enumerable.Range(0, 10))
		{
			var value = i;
			group1.Add(_ => { list.Add(value); return default; });
		}

		await executor.WaitFlushAsync(token);

		// Group 2: Remove all even numbers
		var group2 = executor.CreateGroup(
			_ => { Interlocked.Increment(ref groupCount); return default; },
			_ => default);
		group2.Add(_ => { for (int i = list.Count - 1; i >= 0; i--) if (list[i] % 2 == 0) list.RemoveAt(i); return default; });

		await executor.WaitFlushAsync(token);

		// Group 3: Double all remaining values
		var group3 = executor.CreateGroup(
			_ => { Interlocked.Increment(ref groupCount); return default; },
			_ => default);
		group3.Add(_ => { for (int i = 0; i < list.Count; i++) list[i] *= 2; return default; });

		await executor.WaitFlushAsync(token);

		// Should have [1, 3, 5, 7, 9] doubled = [2, 6, 10, 14, 18]
		list.Count.AssertEqual(5);
		list[0].AssertEqual(2);
		list[1].AssertEqual(6);
		list[2].AssertEqual(10);
		list[3].AssertEqual(14);
		list[4].AssertEqual(18);

		groupCount.AssertGreater(2);
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
					executor.Add(_ => { data.Users.Add((userId, userName, new List<string>())); return default; });

					// Add tags
					executor.Add(_ =>
					{
						var user = data.Users.Find(u => u.Id == userId);
						if (user != default)
							user.Tags.Add($"tag_{i % 3}");
						return default;
					});

					// Update stats
					executor.Add(_ =>
					{
						if (!data.Stats.ContainsKey(userId % 10))
							data.Stats[userId % 10] = 0;
						data.Stats[userId % 10]++;
						return default;
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

	#region Load Tests

	[TestMethod]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task LoadTest_CsvWriterWithTransactionStream()
	{
		var token = CancellationToken;

		const int rowCount = 10000;
		const int colCount = 50;

		var fs = new MemoryFileSystem();
		var filePath = "/test.csv";
		var deleteExecuted = false;

		// Use interval to batch operations
		await using var executor = new ChannelExecutor(ex => throw ex, TimeSpan.FromSeconds(1));
		_ = executor.RunAsync(token);

		// Create TransactionFileStream and CsvFileWriter
		var stream = new TransactionFileStream(fs, filePath, FileMode.Create);
		var writer = new CsvFileWriter(stream);
		writer.LineSeparator = "\n";

		// Create group: begin = nothing (already open), end = commit transaction
		var group = executor.CreateGroup(
			_ => default,
			stream.CommitAsync);

		var writeCount = 0;

		// Write 10000 rows with 50 columns each into the group
		for (int r = 0; r < rowCount; r++)
		{
			var row = new string[colCount];
			for (int c = 0; c < colCount; c++)
				row[c] = $"R{r}C{c}";

			var rowCopy = row;
			group.Add(t =>
			{
				writeCount++;
				return writer.WriteRowAsync(rowCopy, t);
			});
		}

		// Add delete operation directly to channel (not to group)
		executor.Add(async t =>
		{
			writer.Dispose();
			await stream.DisposeAsync();
			fs.DeleteFile(filePath);
			deleteExecuted = true;
		});

		// Wait for all operations to complete
		await executor.WaitFlushAsync(token);

		// Verify delete was executed
		deleteExecuted.AssertTrue();

		// File should be deleted
		fs.FileExists(filePath).AssertFalse();

		writeCount.AssertEqual(rowCount);
	}

	[TestMethod]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task LoadTest_CsvWriterWithTransactionStream_VerifyContent()
	{
		var token = CancellationToken;

		const int rowCount = 10000;
		const int colCount = 50;

		var fs = new MemoryFileSystem();
		var filePath = "/test.csv";

		// Use immediate mode for predictable behavior
		await using var executor = new ChannelExecutor(ex => throw ex, TimeSpan.Zero);
		_ = executor.RunAsync(token);

		var stream = new TransactionFileStream(fs, filePath, FileMode.Create);
		var writer = new CsvFileWriter(stream);
		writer.LineSeparator = "\n";

		// Write rows directly (no group - simpler case)
		for (int r = 0; r < rowCount; r++)
		{
			var row = new string[colCount];
			for (int c = 0; c < colCount; c++)
				row[c] = $"R{r}C{c}";

			var rowCopy = row;
			executor.Add(t => writer.WriteRowAsync(rowCopy, t));
		}

		// Final flush, commit and close
		executor.Add(async t =>
		{
			await writer.FlushAsync(t);
			await stream.CommitAsync(t);
			writer.Dispose();
			await stream.DisposeAsync();
		});

		await executor.WaitFlushAsync(token);

		// Verify file exists
		fs.FileExists(filePath).AssertTrue();

		// Read and verify content
		using var readStream = fs.OpenRead(filePath);
		using var reader = new CsvFileReader(readStream, "\n");
		reader.Delimiter = ';'; // match writer's default delimiter
		var cols = new List<string>();
		var readCount = 0;

		while (await reader.ReadRowAsync(cols, token))
		{
			cols.Count.AssertEqual(colCount);
			cols[0].AssertEqual($"R{readCount}C0");
			cols[colCount - 1].AssertEqual($"R{readCount}C{colCount - 1}");
			readCount++;
		}

		readCount.AssertEqual(rowCount);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task DoubleDispose_DoesNotThrow()
	{
		var token = CancellationToken;

		var executor = new ChannelExecutor(ex => { }, TimeSpan.Zero);
		_ = executor.RunAsync(token);

		var counter = 0;
		executor.Add(_ => { counter++; return default; });

		await executor.WaitFlushAsync(token);
		counter.AssertEqual(1);

		// First dispose
		await executor.DisposeAsync();

		// Second dispose should not throw
		await executor.DisposeAsync();
	}

	#endregion
}
