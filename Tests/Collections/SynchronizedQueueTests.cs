namespace Ecng.Tests.Collections;

[TestClass]
public class SynchronizedQueueTests : BaseTestClass
{
	#region Basic Operations

	[TestMethod]
	public void BasicOperations()
	{
		var q = new SynchronizedQueue<int>();
		q.Enqueue(1);
		q.Enqueue(2);
		q.Peek().AssertEqual(1);
		q.Dequeue().AssertEqual(1);
		q.Dequeue().AssertEqual(2);
		q.Count.AssertEqual(0);
	}

	[TestMethod]
	public void EmptyOperations()
	{
		var q = new SynchronizedQueue<int>();
		ThrowsExactly<InvalidOperationException>(() => q.Dequeue());
		ThrowsExactly<InvalidOperationException>(() => q.Peek());
	}

	[TestMethod]
	public void UnsupportedMethods()
	{
		var q = new SynchronizedQueue<int>();
		q.Enqueue(1);
		var list = (IList<int>)q;
		ThrowsExactly<NotSupportedException>(() => _ = list[0]);
		ThrowsExactly<NotSupportedException>(() => list.Insert(0, 1));
		ThrowsExactly<NotSupportedException>(() => list.RemoveAt(0));
	}

	#endregion

	#region Count and Clear

	[TestMethod]
	public void Count_ReturnsCorrectValue()
	{
		var q = new SynchronizedQueue<int>();
		q.Count.AssertEqual(0);

		q.Enqueue(1);
		q.Count.AssertEqual(1);

		q.Enqueue(2);
		q.Enqueue(3);
		q.Count.AssertEqual(3);

		q.Dequeue();
		q.Count.AssertEqual(2);
	}

	[TestMethod]
	public void Clear_RemovesAllElements()
	{
		var q = new SynchronizedQueue<int>();
		q.Enqueue(1);
		q.Enqueue(2);
		q.Enqueue(3);
		q.Count.AssertEqual(3);

		q.Clear();

		q.Count.AssertEqual(0);
	}

	#endregion

	#region Contains

	[TestMethod]
	public void Contains_FindsExistingElement()
	{
		var q = new SynchronizedQueue<int>();
		q.Enqueue(10);
		q.Enqueue(20);
		q.Enqueue(30);

		q.Contains(20).AssertTrue();
		q.Contains(10).AssertTrue();
		q.Contains(30).AssertTrue();
	}

	[TestMethod]
	public void Contains_ReturnsFalseForMissingElement()
	{
		var q = new SynchronizedQueue<int>();
		q.Enqueue(10);
		q.Enqueue(20);

		q.Contains(99).AssertFalse();
	}

	#endregion

	#region ToArray / CopyTo / CopyAndClear

	[TestMethod]
	public void ToArray_ReturnsAllElements()
	{
		var q = new SynchronizedQueue<int>();
		q.Enqueue(1);
		q.Enqueue(2);
		q.Enqueue(3);

		var array = q.ToArray();

		array.Length.AssertEqual(3);
		// Queue returns elements in FIFO order
		array[0].AssertEqual(1);
		array[1].AssertEqual(2);
		array[2].AssertEqual(3);
	}

	[TestMethod]
	public void ToArray_EmptyQueue_ReturnsEmptyArray()
	{
		var q = new SynchronizedQueue<int>();

		var array = q.ToArray();

		array.Length.AssertEqual(0);
	}

	[TestMethod]
	public void CopyTo_CopiesAllElements()
	{
		var q = new SynchronizedQueue<int>();
		q.Enqueue(1);
		q.Enqueue(2);
		q.Enqueue(3);

		var array = new int[3];
		q.CopyTo(array, 0);

		array[0].AssertEqual(1);
		array[1].AssertEqual(2);
		array[2].AssertEqual(3);
	}

	[TestMethod]
	public void CopyTo_WithOffset()
	{
		var q = new SynchronizedQueue<int>();
		q.Enqueue(1);
		q.Enqueue(2);

		var array = new int[5];
		q.CopyTo(array, 2);

		array[0].AssertEqual(0);
		array[1].AssertEqual(0);
		array[2].AssertEqual(1);
		array[3].AssertEqual(2);
		array[4].AssertEqual(0);
	}

	[TestMethod]
	public void CopyAndClear_ReturnsAllElementsAndClears()
	{
		var q = new SynchronizedQueue<int>();
		q.Enqueue(1);
		q.Enqueue(2);
		q.Enqueue(3);

		var array = q.CopyAndClear();

		array.Length.AssertEqual(3);
		q.Count.AssertEqual(0);
	}

	#endregion

	#region ICollection.Add

	[TestMethod]
	public void ICollection_Add_EnqueuesElement()
	{
		var q = new SynchronizedQueue<int>();
		var collection = (ICollection<int>)q;

		collection.Add(10);
		collection.Add(20);

		q.Count.AssertEqual(2);
		q.Peek().AssertEqual(10); // FIFO - first added is first out
	}

	#endregion

	#region Enumeration

	[TestMethod]
	public void Enumeration_IteratesInFifoOrder()
	{
		var q = new SynchronizedQueue<int>();
		q.Enqueue(1);
		q.Enqueue(2);
		q.Enqueue(3);

		var result = new List<int>();
		foreach (var item in q)
			result.Add(item);

		result.Count.AssertEqual(3);
		result[0].AssertEqual(1);
		result[1].AssertEqual(2);
		result[2].AssertEqual(3);
	}

	#endregion

	#region TryDequeue / TryPeek extension methods

	[TestMethod]
	public void TryDequeue_ReturnsElementIfExists()
	{
		var q = new SynchronizedQueue<string>();
		q.Enqueue("first");
		q.Enqueue("second");

		var result = q.TryDequeue();

		result.AssertEqual("first");
		q.Count.AssertEqual(1);
	}

	[TestMethod]
	public void TryDequeue_ReturnsDefaultIfEmpty()
	{
		var q = new SynchronizedQueue<string>();

		var result = q.TryDequeue();

		result.AssertEqual(default);
	}

	[TestMethod]
	public void TryPeek_ReturnsElementIfExists()
	{
		var q = new SynchronizedQueue<string>();
		q.Enqueue("first");
		q.Enqueue("second");

		var result = q.TryPeek();

		result.AssertEqual("first");
		q.Count.AssertEqual(2); // does not remove
	}

	[TestMethod]
	public void TryPeek_ReturnsDefaultIfEmpty()
	{
		var q = new SynchronizedQueue<string>();

		var result = q.TryPeek();

		result.AssertEqual(default);
	}

	#endregion
}
