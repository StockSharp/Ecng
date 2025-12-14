namespace Ecng.Tests.Collections;

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

	private static TestOrderedChannel CreateQueue(int maxSize = -1) => new(maxSize);
}
