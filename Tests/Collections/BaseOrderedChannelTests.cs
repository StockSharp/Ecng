namespace Ecng.Tests.Collections;

using System.Threading.Channels;

/// <summary>
/// Tests for <see cref="BaseOrderedChannel{TSort, TValue, TCollection}"/>.
/// </summary>
[TestClass]
public class BaseOrderedChannelTests : BaseTestClass
{
	/// <summary>
	/// Test implementation of BaseOrderedChannel for testing purposes.
	/// Uses PriorityQueue for sorting by integer priority.
	/// </summary>
	private class TestOrderedChannel(int maxSize) : BaseOrderedChannel<int, string, Ecng.Collections.PriorityQueue<int, string>>(new Ecng.Collections.PriorityQueue<int, string>((a, b) => Math.Abs(a - b), Comparer<int>.Default), maxSize)
	{
        public ValueTask Add(int priority, string value, CancellationToken cancellationToken)
			=> Enqueue(priority, value, cancellationToken);
	}

	[TestMethod]
	public void Open_InitializesQueue()
	{
		// Arrange
		var queue = CreateQueue();

		// Act
		queue.Open();

		// Assert
		queue.IsClosed.AssertFalse();
		queue.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Close_ClosesQueue()
	{
		// Arrange
		var queue = CreateQueue();
		queue.Open();

		// Act
		queue.Close();

		// Assert
		queue.IsClosed.AssertTrue();
	}

	[TestMethod]
	public void MaxSize_DefaultValue_IsMinusOne()
	{
		// Arrange
		var queue = CreateQueue();

		// Assert
		queue.MaxSize.AssertEqual(-1);
	}

	[TestMethod]
	public void MaxSize_SetValidValue_Succeeds()
	{
		// Arrange & Act
		var queue = CreateQueue(maxSize: 100);

		// Assert
		queue.MaxSize.AssertEqual(100);
	}

	[TestMethod]
	public async Task Enqueue_WhenClosed_DropsValue()
	{
		// Arrange
		var queue = CreateQueue();

		// Act - enqueue without opening
		await queue.Add(1, "test", CancellationToken);

		// Assert - value should be dropped
		queue.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task EnqueueDequeue_SingleItem_WorksCorrectly()
	{
		var token = CancellationToken;

		// Arrange
		var queue = CreateQueue();
		queue.Open();

		// Act
		await queue.Add(1, "first", token);
		var result = await queue.DequeueAsync(token);

		// Assert
		result.AssertEqual("first");
		queue.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task EnqueueDequeue_MultipleItems_MaintainsOrder()
	{
		var token = CancellationToken;

		// Arrange
		var queue = CreateQueue();
		queue.Open();

		// Act - enqueue in non-sorted order
		await queue.Add(3, "third", token);
		await queue.Add(1, "first", token);
		await queue.Add(2, "second", token);

		await Task.Delay(100, token); // Allow time for sorting

		// Assert - should dequeue in sorted order by priority
		var first = await queue.DequeueAsync(token);
		var second = await queue.DequeueAsync(token);
		var third = await queue.DequeueAsync(token);

		first.AssertEqual("first");
		second.AssertEqual("second");
		third.AssertEqual("third");
	}

	[TestMethod]
	public async Task ReadAllAsync_ReturnsAllItems()
	{
		var token = CancellationToken;

		// Arrange
		var queue = CreateQueue();
		queue.Open();

		await queue.Add(1, "first", token);
		await queue.Add(2, "second", token);
		await queue.Add(3, "third", token);

		await Task.Delay(100, token); // Allow time for items to be queued

		// Act
		var items = new List<string>();
		var cts = CancellationTokenSource.CreateLinkedTokenSource(token);

		var readTask = Task.Run(async () =>
		{
			await foreach (var item in queue.ReadAllAsync(cts.Token))
			{
				items.Add(item);
				if (items.Count == 3)
					cts.Cancel();
			}
		}, token);

		await Task.WhenAny(readTask, Task.Delay(2000, token));

		// Assert
		items.Count.AssertEqual(3);
		items[0].AssertEqual("first");
		items[1].AssertEqual("second");
		items[2].AssertEqual("third");
	}

	[TestMethod]
	public async Task Clear_RemovesAllItems()
	{
		var token = CancellationToken;

		// Arrange
		var queue = CreateQueue();
		queue.Open();

		await queue.Add(1, "first", token);
		await queue.Add(2, "second", token);
		await queue.Add(3, "third", token);

		await Task.Delay(100, token); // Allow time for items to be queued

		// Act
		queue.Clear();

		// Assert
		queue.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task Reopen_AfterClose_WorksCorrectly()
	{
		var token = CancellationToken;

		// Arrange
		var queue = CreateQueue();
		queue.Open();
		await queue.Add(1, "first", token);
		queue.Close();

		// Act
		queue.Open();
		await queue.Add(2, "second", token);
		var result = await queue.DequeueAsync(token);

		// Assert
		result.AssertEqual("second");
		// Old items should be cleared when reopening
	}

	[TestMethod]
	public async Task BoundedQueue_RespectsMaxSize()
	{
		var token = CancellationToken;

		// Arrange
		var queue = CreateQueue(maxSize: 2);
		queue.Open();

		// Act - try to add 3 items to queue with max size 2
		await queue.Add(1, "first", token);
		await queue.Add(2, "second", token);

		var writeTask = Task.Run(async () => await queue.Add(3, "third", token), token); // This should block

		// Wait a bit to see if it completes (it shouldn't)
		var completed = await Task.WhenAny(writeTask, Task.Delay(500, token)) == writeTask;

		// Dequeue one item to make space
		await queue.DequeueAsync(token);

		// Now the blocked write should complete
		await writeTask.WaitAsync(TimeSpan.FromSeconds(2), token);

		// Assert
		completed.AssertFalse(); // Should have blocked initially
	}

	[TestMethod]
	public async Task Enqueue_WhenClosedWhileWriteIsPending_DropsValue()
	{
		var token = CancellationToken;
		var queue = CreateQueue(maxSize: 1);
		queue.Open();

		await queue.Add(1, "first", token);
		var writeTask = queue.Add(2, "second", token).AsTask();

		await Task.Delay(100, token);
		queue.Close();

		await writeTask.WaitAsync(TimeSpan.FromSeconds(2), token);
	}

	[TestMethod]
	public async Task Count_ReflectsQueueSize()
	{
		var token = CancellationToken;

		// Arrange
		var queue = CreateQueue();
		queue.Open();

		// Act & Assert
		queue.Count.AssertEqual(0);

		await queue.Add(1, "first", token);
		await Task.Delay(50, token);

		await queue.Add(2, "second", token);
		await queue.Add(3, "third", token);
		await Task.Delay(100, token);

		// Dequeue all
		await queue.DequeueAsync(token);
		await queue.DequeueAsync(token);
		await queue.DequeueAsync(token);

		queue.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task Count_IncludesPendingEnqueuedItems()
	{
		var token = CancellationToken;

		var queue = CreateQueue();
		queue.Open();

		queue.Count.AssertEqual(0);

		// An enqueued item that has not been dequeued yet must still be counted, even though it is
		// buffered in the underlying channel and not yet pulled into the sorted collection.
		await queue.Add(1, "first", token);
		queue.Count.AssertEqual(1);

		await queue.Add(2, "second", token);
		queue.Count.AssertEqual(2);

		// Consuming one item moves the rest into the sorted collection and leaves the remaining pending.
		await queue.DequeueAsync(token);
		queue.Count.AssertEqual(1);
	}

	/// <summary>
	/// Regression test for DequeueAsync on a closed-but-never-opened queue: ensures it throws
	/// ChannelClosedException instead of busy-polling. (Was: when the channel was null because the
	/// queue was closed before ever being opened, _isClosed was ignored and DequeueAsync spun on a
	/// 1ms timer indefinitely; Collections\BaseOrderedChannel.cs:209.)
	/// </summary>
	[TestMethod]
	public async Task DequeueAsync_ClosedBeforeOpened_Throws()
	{
		var queue = CreateQueue();

		// Close without ever opening: _channel stays null and _isClosed becomes true.
		queue.Close();

		await ThrowsAsync<ChannelClosedException>(()
			=> queue.DequeueAsync(CancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(5), CancellationToken));
	}

	private static TestOrderedChannel CreateQueue(int maxSize = -1) => new(maxSize);
}
