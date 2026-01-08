namespace Ecng.Tests.Collections;

[TestClass]
public class SynchronizedStackTests : BaseTestClass
{
	#region Basic Operations

	[TestMethod]
	public void BasicOperations()
	{
		var st = new SynchronizedStack<int>();
		st.Push(1);
		st.Push(2);
		st.Peek().AssertEqual(2);
		st.Pop().AssertEqual(2);
		st.Pop().AssertEqual(1);
		st.Count.AssertEqual(0);
	}

	[TestMethod]
	public void EmptyOperations()
	{
		var st = new SynchronizedStack<int>();
		ThrowsExactly<InvalidOperationException>(() => st.Pop());
		ThrowsExactly<InvalidOperationException>(() => st.Peek());
	}

	[TestMethod]
	public void UnsupportedMethods()
	{
		var st = new SynchronizedStack<int>
		{
			1
		};
		var list = (IList<int>)st;
		ThrowsExactly<NotSupportedException>(() => _ = list[0]);
		ThrowsExactly<NotSupportedException>(() => list.Insert(0, 1));
		ThrowsExactly<NotSupportedException>(() => list.RemoveAt(0));
	}

	#endregion

	#region Count and Clear

	[TestMethod]
	public void Count_ReturnsCorrectValue()
	{
		var st = new SynchronizedStack<int>();
		st.Count.AssertEqual(0);

		st.Push(1);
		st.Count.AssertEqual(1);

		st.Push(2);
		st.Push(3);
		st.Count.AssertEqual(3);

		st.Pop();
		st.Count.AssertEqual(2);
	}

	[TestMethod]
	public void Clear_RemovesAllElements()
	{
		var st = new SynchronizedStack<int>();
		st.Push(1);
		st.Push(2);
		st.Push(3);
		st.Count.AssertEqual(3);

		st.Clear();

		st.Count.AssertEqual(0);
	}

	#endregion

	#region Contains

	[TestMethod]
	public void Contains_FindsExistingElement()
	{
		var st = new SynchronizedStack<int>();
		st.Push(10);
		st.Push(20);
		st.Push(30);

		st.Contains(20).AssertTrue();
		st.Contains(10).AssertTrue();
		st.Contains(30).AssertTrue();
	}

	[TestMethod]
	public void Contains_ReturnsFalseForMissingElement()
	{
		var st = new SynchronizedStack<int>();
		st.Push(10);
		st.Push(20);

		st.Contains(99).AssertFalse();
	}

	#endregion

	#region ToArray / CopyTo / CopyAndClear

	[TestMethod]
	public void ToArray_ReturnsAllElements()
	{
		var st = new SynchronizedStack<int>();
		st.Push(1);
		st.Push(2);
		st.Push(3);

		var array = st.ToArray();

		array.Length.AssertEqual(3);
		// Stack returns elements in LIFO order
		array[0].AssertEqual(3);
		array[1].AssertEqual(2);
		array[2].AssertEqual(1);
	}

	[TestMethod]
	public void ToArray_EmptyStack_ReturnsEmptyArray()
	{
		var st = new SynchronizedStack<int>();

		var array = st.ToArray();

		array.Length.AssertEqual(0);
	}

	[TestMethod]
	public void CopyTo_CopiesAllElements()
	{
		var st = new SynchronizedStack<int>();
		st.Push(1);
		st.Push(2);
		st.Push(3);

		var array = new int[3];
		st.CopyTo(array, 0);

		array[0].AssertEqual(3);
		array[1].AssertEqual(2);
		array[2].AssertEqual(1);
	}

	[TestMethod]
	public void CopyTo_WithOffset()
	{
		var st = new SynchronizedStack<int>();
		st.Push(1);
		st.Push(2);

		var array = new int[5];
		st.CopyTo(array, 2);

		array[0].AssertEqual(0);
		array[1].AssertEqual(0);
		array[2].AssertEqual(2);
		array[3].AssertEqual(1);
		array[4].AssertEqual(0);
	}

	[TestMethod]
	public void CopyAndClear_ReturnsAllElementsAndClears()
	{
		var st = new SynchronizedStack<int>();
		st.Push(1);
		st.Push(2);
		st.Push(3);

		var array = st.CopyAndClear();

		array.Length.AssertEqual(3);
		st.Count.AssertEqual(0);
	}

	#endregion

	#region ICollection.Add

	[TestMethod]
	public void ICollection_Add_PushesElement()
	{
		var st = new SynchronizedStack<int>();
		var collection = (ICollection<int>)st;

		collection.Add(10);
		collection.Add(20);

		st.Count.AssertEqual(2);
		st.Peek().AssertEqual(20);
	}

	#endregion

	#region Enumeration

	[TestMethod]
	public void Enumeration_IteratesInLifoOrder()
	{
		var st = new SynchronizedStack<int>();
		st.Push(1);
		st.Push(2);
		st.Push(3);

		var result = new List<int>();
		foreach (var item in st)
			result.Add(item);

		result.Count.AssertEqual(3);
		result[0].AssertEqual(3);
		result[1].AssertEqual(2);
		result[2].AssertEqual(1);
	}

	#endregion
}
