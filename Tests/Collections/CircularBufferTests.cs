namespace Ecng.Tests.Collections;

[TestClass]
public class CircularBufferTests
{
	[TestMethod]
	public void AppendOverCapacity()
	{
		var buf = new CircularBuffer<int>(3);
		buf.PushBack(1);
		buf.PushBack(2);
		buf.PushBack(3);
		buf.IsFull.AssertTrue();
		buf.PushBack(4);
		buf.ToArray().SequenceEqual([2, 3, 4]).AssertTrue();
	}

	[TestMethod]
	public void FrontBackPushPop()
	{
		var buf = new CircularBuffer<int>(2);
		buf.PushBack(1);
		buf.PushFront(0);
		buf.Back().AssertEqual(1);
		buf.Front().AssertEqual(0);
		buf.PopBack();
		buf.PopFront();
		buf.IsEmpty.AssertTrue();
	}

	[TestMethod]
	public void SegmentsEnumeration()
	{
		var buf = new CircularBuffer<int>(5);
		buf.PushBack(1);
		buf.PushBack(2);
		buf.PushBack(3);
		buf.PopFront();
		buf.PushBack(4);
		buf.PushBack(5);
		buf.ToArray().SequenceEqual([2, 3, 4, 5]).AssertTrue();
		var total = buf.ToArraySegments().Sum(s => s.Count);
		total.AssertEqual(4);
	}

	[TestMethod]
	public void ClearAndExceptions()
	{
		var buf = new CircularBuffer<int>(3);
		buf.PushBack(1);
		buf.PushBack(2);
		buf.Clear();
		buf.IsEmpty.AssertTrue();
		Assert.ThrowsExactly<InvalidOperationException>(() => buf.PopBack());
	}

	[TestMethod]
	public void CollectionCompatibility()
	{
		var icol = (ICollection<int>)new CircularBuffer<int>(5);
		icol.Add(10);
		icol.Add(20);
		icol.Add(30);

		 // ICollection<T>
		icol.IsReadOnly.AssertFalse();
		icol.Add(40);
		icol.ToArray().SequenceEqual([10, 20, 30, 40]).AssertTrue();
		// Remove, Contains, CopyTo
		icol.Count.AssertEqual(4);
		icol.Contains(10).AssertTrue();
		icol.Remove(10).AssertTrue();
		icol.Remove(10).AssertFalse();
		icol.Count.AssertEqual(3);
		icol.Contains(10).AssertFalse();
		icol.CopyTo(new int[5], 0);

		// IList<T>
		var ilist = (IList<int>)icol;
		ilist.IndexOf(20).AssertEqual(0);
		ilist.Insert(0, 99);
		ilist.IndexOf(20).AssertEqual(1);
		icol.Count.AssertEqual(4);
		ilist.RemoveAt(0);
		ilist.IndexOf(20).AssertEqual(0);
		icol.Count.AssertEqual(3);
		// get/set by index
		ilist[0].AssertEqual(20);
		ilist[0] = 111;
		ilist[0].AssertEqual(111);
	}
	
	[TestMethod]
	public void LinqCompatibility()
	{
		var buf = new CircularBuffer<int>(5);
		buf.PushBack(1);
		buf.PushBack(2);
		buf.PushBack(3);

		var enu = (IEnumerable<int>)buf;

		// ToArray
		var arr = enu.ToArray();
		arr.SequenceEqual([1, 2, 3]).AssertTrue();

		int[] concatArr = [4, 5];

		// Concat
		var concat = enu.Concat(concatArr);
		concat.SequenceEqual([1, 2, 3, 4, 5]).AssertTrue();

		// First, Last
		enu.First().AssertEqual(1);
		enu.Last().AssertEqual(3);

		// Count, Any
		enu.Count().AssertEqual(3);
		enu.Any().AssertTrue();

		enu.Skip(3).Count().AssertEqual(0);
		enu.Skip(3).Any().AssertFalse();

		enu.Skip(3).Concat(concatArr).Count().AssertEqual(2);
		enu.Skip(3).Concat(concatArr).Any().AssertTrue();

		enu.Skip(3).Concat(concatArr).ToArray().SequenceEqual(concatArr).AssertTrue();

		// Where, Select
		var even = enu.Where(x => x % 2 == 0).ToArray();
		even.SequenceEqual([2]).AssertTrue();
		var doubled = enu.Select(x => x * 2).ToArray();
		doubled.SequenceEqual([2, 4, 6]).AssertTrue();
	}

	[TestMethod]
	public void ShrinkCapacity()
	{
		var cb = new CircularBuffer<int>(10);

		for (var i = 1; i <= 15; i++)
			cb.PushBack(i);

		cb.Capacity = 4;

		cb.ToArray().AssertEqual([12, 13, 14, 15]);
		cb.Count.AreEqual(4);
		cb.Capacity.AreEqual(4);
		cb.IsFull.AssertTrue();
		cb.IsEmpty.AssertFalse();
	}

	[TestMethod]
	public void GrowCapacity()
	{
		var cb = new CircularBuffer<int>(3);
		cb.PushBack(1);
		cb.PushBack(2);
		cb.PushBack(3); // full: [1,2,3]

		cb.Capacity = 5; // grow

		cb.ToArray().AssertEqual([1, 2, 3]);
		cb.Count.AreEqual(3);
		cb.Capacity.AreEqual(5);
		cb.IsFull.AssertFalse();
	}
}
