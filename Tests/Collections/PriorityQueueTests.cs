namespace Ecng.Tests.Collections;

[TestClass]
public class PriorityQueueTests : BaseTestClass
{
	private static Ecng.Collections.PriorityQueue<long, int> CreateQueue()
		=> new((p1, p2) => (p1 - p2).Abs());

	#region Basic Operations

	[TestMethod]
	public void SameOrder()
	{
		var pq = CreateQueue();
		pq.Enqueue(0, 0);
		pq.Enqueue(1, 1);
		pq.Enqueue(0, 0);
		pq.Enqueue(1, 2);
		pq.Enqueue(2, 30);
		pq.Enqueue(3, 40);
		pq.Enqueue(1, 3);
		pq.Enqueue(0, 0);
		pq.Count.AssertEqual(8);

		pq.Dequeue();
		pq.Count.AssertEqual(7);

		var emptyCnt = 2;

		for (var i = 0; i < emptyCnt; i++)
			pq.Dequeue();

		var left = 7 - emptyCnt;
		pq.Count.AssertEqual(left);

		var prev = 0;

		while (pq.Count > 0)
		{
			var curr = pq.Dequeue().element;
			(curr > prev).AssertTrue();
			prev = curr;
			pq.Count.AssertEqual(--left);
		}
	}

	[TestMethod]
	public void Random()
	{
		var pq = CreateQueue();

		var count = 100000;

		var prev = 0;

		for (var i = 0; i < count; i++)
			pq.Enqueue(RandomGen.GetInt(0, 100), ++prev);

		pq.Count.AssertEqual(count);

		var prevPriority = 0L;
		var left = count;

		while (pq.Count > 0)
		{
			var (priority, _) = pq.Dequeue();

			(prevPriority <= priority).AssertTrue();
			prevPriority = priority;

			pq.Count.AssertEqual(--left);
		}
	}

	[TestMethod]
	public void Seq()
	{
		var pq = CreateQueue();

		var count = 100000;

		var prev = 0;

		for (var i = 0; i < count; i++)
			pq.Enqueue(i, ++prev);

		pq.Count.AssertEqual(count);

		var prevPriority = 0L;
		var left = count;

		while (pq.Count > 0)
		{
			var (priority, _) = pq.Dequeue();

			(prevPriority <= priority).AssertTrue();
			prevPriority = priority;

			pq.Count.AssertEqual(--left);
		}
	}

	[TestMethod]
	public void Enumeration()
	{
		var pq = CreateQueue();
		pq.Enqueue(1, 1);
		pq.Enqueue(2, 2);

		var counter = 0;

		foreach (var item in pq)
			counter++;

		counter.AssertEqual(2);

		pq.Enqueue(1, 1);
		pq.Enqueue(2, 2);

		counter = 0;

		foreach (var item in pq)
			counter++;

		counter.AssertEqual(4);
	}

	[TestMethod]
	public void Dequeue_EmptyQueue_Throws()
	{
		var pq = CreateQueue();
		ThrowsExactly<InvalidOperationException>(() => pq.Dequeue());
	}

	[TestMethod]
	public void Peek_EmptyQueue_Throws()
	{
		var pq = CreateQueue();
		ThrowsExactly<InvalidOperationException>(() => pq.Peek());
	}

	#endregion

	#region Peek

	[TestMethod]
	public void Peek_ReturnsMinElement()
	{
		var pq = CreateQueue();
		pq.Enqueue(3, 30);
		pq.Enqueue(1, 10);
		pq.Enqueue(2, 20);

		var (priority, element) = pq.Peek();

		priority.AssertEqual(1);
		element.AssertEqual(10);
		pq.Count.AssertEqual(3); // does not remove element
	}

	#endregion

	#region TryDequeue / TryPeek

	[TestMethod]
	public void TryDequeue_EmptyQueue_ReturnsFalse()
	{
		var pq = CreateQueue();

		var result = pq.TryDequeue(out var element, out var priority);

		result.AssertFalse();
		element.AssertEqual(default);
		priority.AssertEqual(default);
	}

	[TestMethod]
	public void TryDequeue_NonEmptyQueue_ReturnsTrue()
	{
		var pq = CreateQueue();
		pq.Enqueue(5, 50);

		var result = pq.TryDequeue(out var element, out var priority);

		result.AssertTrue();
		priority.AssertEqual(5);
		element.AssertEqual(50);
		pq.Count.AssertEqual(0);
	}

	[TestMethod]
	public void TryPeek_EmptyQueue_ReturnsFalse()
	{
		var pq = CreateQueue();

		var result = pq.TryPeek(out var element, out var priority);

		result.AssertFalse();
		element.AssertEqual(default);
		priority.AssertEqual(default);
	}

	[TestMethod]
	public void TryPeek_NonEmptyQueue_ReturnsTrue()
	{
		var pq = CreateQueue();
		pq.Enqueue(5, 50);

		var result = pq.TryPeek(out var element, out var priority);

		result.AssertTrue();
		priority.AssertEqual(5);
		element.AssertEqual(50);
		pq.Count.AssertEqual(1); // does not remove
	}

	#endregion

	#region Clear

	[TestMethod]
	public void Clear_EmptiesQueue()
	{
		var pq = CreateQueue();
		pq.Enqueue(1, 10);
		pq.Enqueue(2, 20);
		pq.Enqueue(3, 30);
		pq.Count.AssertEqual(3);

		pq.Clear();

		pq.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Clear_EmptyQueue_NoError()
	{
		var pq = CreateQueue();
		pq.Clear(); // should not throw
		pq.Count.AssertEqual(0);
	}

	#endregion

	#region CopyTo / ToArray

	[TestMethod]
	public void CopyTo_CopiesAllElements()
	{
		var pq = CreateQueue();
		pq.Enqueue(1, 10);
		pq.Enqueue(2, 20);
		pq.Enqueue(1, 11); // same priority

		var array = new (long, int)[3];
		((ICollection<(long, int)>)pq).CopyTo(array, 0);

		array.Length.AssertEqual(3);
		// elements should be in priority order
		array[0].AssertEqual((1L, 10));
		array[1].AssertEqual((1L, 11));
		array[2].AssertEqual((2L, 20));
	}

	[TestMethod]
	public void CopyTo_WithOffset()
	{
		var pq = CreateQueue();
		pq.Enqueue(1, 10);
		pq.Enqueue(2, 20);

		var array = new (long, int)[5];
		((ICollection<(long, int)>)pq).CopyTo(array, 2);

		array[0].AssertEqual(default);
		array[1].AssertEqual(default);
		array[2].AssertEqual((1L, 10));
		array[3].AssertEqual((2L, 20));
		array[4].AssertEqual(default);
	}

	[TestMethod]
	public void CopyTo_NullArray_Throws()
	{
		var pq = CreateQueue();
		pq.Enqueue(1, 10);

		ThrowsExactly<ArgumentNullException>(() =>
			((ICollection<(long, int)>)pq).CopyTo(null, 0));
	}

	[TestMethod]
	public void CopyTo_NegativeIndex_Throws()
	{
		var pq = CreateQueue();
		pq.Enqueue(1, 10);
		var array = new (long, int)[5];

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			((ICollection<(long, int)>)pq).CopyTo(array, -1));
	}

	[TestMethod]
	public void CopyTo_IndexTooLarge_Throws()
	{
		var pq = CreateQueue();
		pq.Enqueue(1, 10);
		var array = new (long, int)[5];

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			((ICollection<(long, int)>)pq).CopyTo(array, 10));
	}

	[TestMethod]
	public void CopyTo_NotEnoughSpace_Throws()
	{
		var pq = CreateQueue();
		pq.Enqueue(1, 10);
		pq.Enqueue(2, 20);
		pq.Enqueue(3, 30);
		var array = new (long, int)[2];

		ThrowsExactly<ArgumentException>(() =>
			((ICollection<(long, int)>)pq).CopyTo(array, 0));
	}

	[TestMethod]
	public void ToArray_ReturnsAllElements()
	{
		var pq = CreateQueue();
		pq.Enqueue(3, 30);
		pq.Enqueue(1, 10);
		pq.Enqueue(2, 20);

		var array = pq.ToArray();

		array.Length.AssertEqual(3);
		array[0].AssertEqual((1L, 10));
		array[1].AssertEqual((2L, 20));
		array[2].AssertEqual((3L, 30));
	}

	[TestMethod]
	public void ToArray_EmptyQueue_ReturnsEmptyArray()
	{
		var pq = CreateQueue();

		var array = pq.ToArray();

		array.Length.AssertEqual(0);
	}

	[TestMethod]
	public void CopyAndClear_WorksCorrectly()
	{
		var pq = CreateQueue();
		pq.Enqueue(1, 10);
		pq.Enqueue(2, 20);
		pq.Enqueue(3, 30);

		var array = pq.CopyAndClear();

		array.Length.AssertEqual(3);
		pq.Count.AssertEqual(0);
	}

	#endregion

	#region DequeueEnqueue / EnqueueDequeue

	[TestMethod]
	public void DequeueEnqueue_RemovesMinAndAddsNew()
	{
		var pq = CreateQueue();
		pq.Enqueue(1, 10);
		pq.Enqueue(3, 30);

		var removed = pq.DequeueEnqueue(2, 20);

		removed.AssertEqual(10);
		pq.Count.AssertEqual(2);

		var (p1, e1) = pq.Dequeue();
		p1.AssertEqual(2);
		e1.AssertEqual(20);

		var (p2, e2) = pq.Dequeue();
		p2.AssertEqual(3);
		e2.AssertEqual(30);
	}

	[TestMethod]
	public void DequeueEnqueue_EmptyQueue_Throws()
	{
		var pq = CreateQueue();
		ThrowsExactly<InvalidOperationException>(() => pq.DequeueEnqueue(1, 10));
	}

	[TestMethod]
	public void EnqueueDequeue_EmptyQueue_ReturnsSameElement()
	{
		var pq = CreateQueue();

		var result = pq.EnqueueDequeue(5, 50);

		result.AssertEqual(50);
		pq.Count.AssertEqual(0);
	}

	[TestMethod]
	public void EnqueueDequeue_ReturnsMin()
	{
		var pq = CreateQueue();
		pq.Enqueue(2, 20);
		pq.Enqueue(3, 30);

		var result = pq.EnqueueDequeue(1, 10);

		result.AssertEqual(10); // new element has minimum priority
		pq.Count.AssertEqual(2);
	}

	#endregion

	#region EnqueueRange

	[TestMethod]
	public void EnqueueRange_WithPriority_AddsAllElements()
	{
		var pq = CreateQueue();

		pq.EnqueueRange(5, new[] { 50, 51, 52 });

		pq.Count.AssertEqual(3);

		var (p1, e1) = pq.Dequeue();
		p1.AssertEqual(5);
		e1.AssertEqual(50);
	}

	[TestMethod]
	public void EnqueueRange_WithTuples_AddsAllElements()
	{
		var pq = CreateQueue();

		pq.EnqueueRange(new[] { (1L, 10), (3L, 30), (2L, 20) });

		pq.Count.AssertEqual(3);

		var (p1, _) = pq.Dequeue();
		p1.AssertEqual(1);

		var (p2, _) = pq.Dequeue();
		p2.AssertEqual(2);

		var (p3, _) = pq.Dequeue();
		p3.AssertEqual(3);
	}

	[TestMethod]
	public void EnqueueRange_NullItems_Throws()
	{
		var pq = CreateQueue();
		ThrowsExactly<ArgumentNullException>(() => pq.EnqueueRange(null));
	}

	/// <summary>
	/// Verifies that EnqueueRange with empty sequence does not create garbage nodes.
	/// </summary>
	[TestMethod]
	public void EnqueueRange_EmptySequence_ShouldNotCreateNodes()
	{
		var pq = CreateQueue();

		// Enqueue empty range - should not create any nodes
		pq.EnqueueRange(1, Array.Empty<int>());

		// Queue should remain empty and functional
		pq.Count.AssertEqual(0, "Queue should be empty after enqueueing empty range");

		// Peek should throw InvalidOperationException for empty queue
		ThrowsExactly<InvalidOperationException>(() => pq.Peek());

		// TryPeek should return false
		pq.TryPeek(out _, out _).AssertFalse("TryPeek on empty queue should return false");

		// Should still be able to use the queue
		pq.Enqueue(1, 10);
		pq.Count.AssertEqual(1);
		pq.Peek().AssertEqual((1L, 10));
	}

	/// <summary>
	/// Verifies that multiple empty EnqueueRange calls don't break enumeration.
	/// Scenario: empty nodes at the front of the linked list after dequeue.
	/// </summary>
	[TestMethod]
	public void EnqueueRange_MultipleEmptySequences_EnumerationWorks()
	{
		var pq = CreateQueue();

		// Add a real element first (priority 1 = best)
		pq.Enqueue(1, 10);

		// Add empty ranges with worse priorities (go to end of list)
		pq.EnqueueRange(2, Array.Empty<int>());
		pq.EnqueueRange(3, Array.Empty<int>());

		// Add another real element with worst priority (goes last)
		pq.EnqueueRange(4, new[] { 40 });

		// Count should be 2
		pq.Count.AssertEqual(2);

		// Dequeue the first element - now empty nodes are at front!
		// List becomes: [EmptyNode(2), EmptyNode(3), Node(4, 40)]
		var first = pq.Dequeue();
		first.AssertEqual((1L, 10));

		pq.Count.AssertEqual(1);

		// Direct enumeration test
		var count = 0;
		foreach (var item in pq)
		{
			count++;
			item.AssertEqual((4L, 40));
		}
		count.AssertEqual(1, "Enumerator should find exactly 1 element after skipping empty nodes");
	}

	#endregion

	#region ICollection interface

	[TestMethod]
	public void ICollection_Add_EnqueuesElement()
	{
		var pq = CreateQueue();
		var collection = (ICollection<(long, int)>)pq;

		collection.Add((5, 50));
		collection.Add((3, 30));

		pq.Count.AssertEqual(2);
		pq.Peek().AssertEqual((3, 30));
	}

	[TestMethod]
	public void ICollection_IsReadOnly_ReturnsFalse()
	{
		var pq = CreateQueue();
		var collection = (ICollection<(long, int)>)pq;

		collection.IsReadOnly.AssertFalse();
	}

	[TestMethod]
	public void ICollection_Contains_ThrowsNotSupported()
	{
		var pq = CreateQueue();
		pq.Enqueue(1, 10);
		var collection = (ICollection<(long, int)>)pq;

		ThrowsExactly<NotSupportedException>(() => collection.Contains((1, 10)));
	}

	[TestMethod]
	public void ICollection_Remove_ThrowsNotSupported()
	{
		var pq = CreateQueue();
		pq.Enqueue(1, 10);
		var collection = (ICollection<(long, int)>)pq;

		ThrowsExactly<NotSupportedException>(() => collection.Remove((1, 10)));
	}

	#endregion

	#region IQueue interface

	[TestMethod]
	public void IQueue_Enqueue_AddsElement()
	{
		var pq = CreateQueue();
		var queue = (IQueue<(long, int)>)pq;

		queue.Enqueue((5, 50));

		pq.Count.AssertEqual(1);
		pq.Peek().AssertEqual((5, 50));
	}

	[TestMethod]
	public void IQueue_TryDequeue_ReturnsElement()
	{
		var pq = CreateQueue();
		pq.Enqueue(5, 50);
		var queue = (IQueue<(long, int)>)pq;

		var result = queue.TryDequeue(out var item);

		result.AssertTrue();
		item.AssertEqual((5, 50));
	}

	[TestMethod]
	public void IQueue_TryDequeue_EmptyQueue_ReturnsFalse()
	{
		var pq = CreateQueue();
		var queue = (IQueue<(long, int)>)pq;

		var result = queue.TryDequeue(out var item);

		result.AssertFalse();
		item.AssertEqual(default);
	}

	#endregion

	#region Enumeration modification detection

	[TestMethod]
	public void Enumeration_ModificationDuringIteration_Throws()
	{
		var pq = CreateQueue();
		pq.Enqueue(1, 10);
		pq.Enqueue(2, 20);

		ThrowsExactly<InvalidOperationException>(() =>
		{
			foreach (var _ in pq)
			{
				pq.Enqueue(3, 30); // modification during iteration
			}
		});
	}

	#endregion

	#region Comparer

	[TestMethod]
	public void Comparer_ReturnsConfiguredComparer()
	{
		var customComparer = Comparer<long>.Create((a, b) => b.CompareTo(a)); // reverse
		var pq = new Ecng.Collections.PriorityQueue<long, int>((p1, p2) => (p1 - p2).Abs(), customComparer);

		pq.Comparer.AssertEqual(customComparer);
	}

	#endregion
}
