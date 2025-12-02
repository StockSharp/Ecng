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
        public void Add(int priority, string value) => Enqueue(priority, value);
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
	public void Enqueue_WhenClosed_DropsValue()
	{
		// Arrange
		var queue = CreateQueue();

		// Act - enqueue without opening
		queue.Add(1, "test");

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
		queue.Add(1, "first");
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
		queue.Add(3, "third");
		queue.Add(1, "first");
		queue.Add(2, "second");

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

		queue.Add(1, "first");
		queue.Add(2, "second");
		queue.Add(3, "third");

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

		queue.Add(1, "first");
		queue.Add(2, "second");
		queue.Add(3, "third");

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
		queue.Add(1, "first");
		queue.Close();

		// Act
		queue.Open();
		queue.Add(2, "second");
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
		queue.Add(1, "first");
		queue.Add(2, "second");

		var writeTask = Task.Run(() => queue.Add(3, "third"), token); // This should block

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

		queue.Add(1, "first");
		await Task.Delay(50, token);

		queue.Add(2, "second");
		queue.Add(3, "third");
		await Task.Delay(100, token);

		// Dequeue all
		await queue.DequeueAsync(token);
		await queue.DequeueAsync(token);
		await queue.DequeueAsync(token);

		queue.Count.AssertEqual(0);
	}

	private static TestOrderedChannel CreateQueue(int maxSize = -1) => new(maxSize);
}
