namespace Ecng.Tests.Collections;

[TestClass]
public class SynchronizedListTests
{
	[TestMethod]
	public void RangeOperations()
	{
		var list = new SynchronizedList<int>();
		list.AddRange(Enumerable.Range(1, 5));
		list.Count.AssertEqual(5);
		list.GetRange(1, 3).SequenceEqual([2, 3, 4]).AssertTrue();
		list.RemoveRange([1, 2]);
		list.ToArray().SequenceEqual([3, 4, 5]).AssertTrue();
		list.RemoveRange(1, 2).AssertEqual(2);
		list.ToArray().SequenceEqual([3]).AssertTrue();
	}

	[TestMethod]
	public void Events()
	{
		var list = new SynchronizedList<int>();
		var added = new List<int>();
		var removed = new List<int>();
		list.AddedRange += items => added.AddRange(items);
		list.RemovedRange += items => removed.AddRange(items);
		list.AddRange([1, 2, 3]);
		added.SequenceEqual([1, 2, 3]).AssertTrue();
		list.RemoveRange([1, 3]);
		removed.SequenceEqual([1, 3]).AssertTrue();
	}

	[TestMethod]
	public void NullCheck()
	{
		var list = new SynchronizedList<string> { CheckNullableItems = true };
		Assert.ThrowsExactly<ArgumentNullException>(() => list.AddRange(["a", null]));
	}

	[TestMethod]
	public void InsertAndIndexer()
	{
		var list = new SynchronizedList<int>();
		list.AddRange([2, 3]);
		list.Insert(0, 1);
		list[0].AssertEqual(1);
		list[2].AssertEqual(3);
		list.IndexOf(3).AssertEqual(2);
		list.RemoveAt(1);
		list.ToArray().SequenceEqual([1, 3]).AssertTrue();
	}

	[TestMethod]
	public void RemoveRangeBounds()
	{
		var list = new SynchronizedList<int>();
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list.RemoveRange(-1, 1));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list.RemoveRange(0, 0));
	}
}
