namespace Ecng.Tests.Collections;

[TestClass]
public class CircularBufferTests : BaseTestClass
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
		buf.AssertEqual(new int[] { 2, 3, 4 });
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
		buf.AssertEqual(new int[] { 2, 3, 4, 5 });
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
		ThrowsExactly<InvalidOperationException>(() => buf.PopBack());
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
		icol.AssertEqual([10, 20, 30, 40]);
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
		arr.AssertEqual([1, 2, 3]);

		int[] concatArr = [4, 5];

		// Concat
		var concat = enu.Concat(concatArr);
		concat.AssertEqual([1, 2, 3, 4, 5]);

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

		enu.Skip(3).Concat(concatArr).AssertEqual(concatArr);

		// Where, Select
		var even = enu.Where(x => x % 2 == 0).ToArray();
		even.AssertEqual([2]);
		var doubled = enu.Select(x => x * 2).ToArray();
		doubled.AssertEqual([2, 4, 6]);
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

	[TestMethod]
	public void RandomPushBackStress()
	{
		const int capacity = 17;
		const int iterations = 10_000;

		var buf = new CircularBuffer<int>(capacity);
		var model = new List<int>(capacity);

		for (int i = 0; i < iterations; i++)
		{
			var val = RandomGen.GetInt();

			buf.PushBack(val);
			model.Add(val);

			if (model.Count > capacity)
				model.RemoveAt(0);

			// basic invariants
			(buf.Count <= buf.Capacity).AssertTrue();
			(buf.IsEmpty == (buf.Count == 0)).AssertTrue();
			(buf.IsFull == (buf.Count == buf.Capacity)).AssertTrue();

			// order check
			buf.AssertEqual(model);
		}
	}

	#region CircularBufferEx Tests

	[TestMethod]
	public void CircularBufferEx_PushBack_Sum()
	{
		var buf = new CircularBufferEx<int>(3) { Operator = new IntOperator() };

		buf.PushBack(1);
		buf.Sum.AssertEqual(1);

		buf.PushBack(2);
		buf.Sum.AssertEqual(3);

		buf.PushBack(3);
		buf.Sum.AssertEqual(6);

		// Overflow - first element (1) should be removed
		buf.PushBack(4);
		buf.Sum.AssertEqual(9); // 2 + 3 + 4
		buf.ToArray().AssertEqual([2, 3, 4]);
	}

	[TestMethod]
	public void CircularBufferEx_PushFront_Sum()
	{
		var buf = new CircularBufferEx<int>(3) { Operator = new IntOperator() };

		buf.PushFront(1);
		buf.Sum.AssertEqual(1);

		buf.PushFront(2);
		buf.Sum.AssertEqual(3);

		buf.PushFront(3);
		buf.Sum.AssertEqual(6);
		buf.ToArray().AssertEqual([3, 2, 1]);

		// Overflow - LAST element (1) should be removed, not first
		buf.PushFront(4);
		buf.Sum.AssertEqual(9); // 4 + 3 + 2
		buf.ToArray().AssertEqual([4, 3, 2]);
	}

	[TestMethod]
	public void CircularBufferEx_PushBack_Max()
	{
		var buf = new CircularBufferEx<int>(3)
		{
			MaxComparer = Comparer<int>.Default
		};

		buf.PushBack(5);
		buf.Max.Value.AssertEqual(5);

		buf.PushBack(10);
		buf.Max.Value.AssertEqual(10);

		buf.PushBack(3);
		buf.Max.Value.AssertEqual(10);

		// Remove 5, max still 10
		buf.PushBack(1);
		buf.Max.Value.AssertEqual(10);

		// Remove 10, should recalc max = 3
		buf.PushBack(2);
		buf.Max.Value.AssertEqual(3);
	}

	[TestMethod]
	public void CircularBufferEx_PushFront_Max()
	{
		var buf = new CircularBufferEx<int>(3)
		{
			MaxComparer = Comparer<int>.Default
		};

		buf.PushFront(5);
		buf.Max.Value.AssertEqual(5);

		buf.PushFront(10);
		buf.Max.Value.AssertEqual(10);

		buf.PushFront(3);
		buf.Max.Value.AssertEqual(10);
		buf.ToArray().AssertEqual([3, 10, 5]);

		// PushFront removes LAST element (5)
		buf.PushFront(1);
		buf.Max.Value.AssertEqual(10);
		buf.ToArray().AssertEqual([1, 3, 10]);

		// PushFront removes LAST element (10) - should recalc
		buf.PushFront(2);
		buf.Max.Value.AssertEqual(3);
		buf.ToArray().AssertEqual([2, 1, 3]);
	}

	[TestMethod]
	public void CircularBufferEx_PushBack_Min()
	{
		var buf = new CircularBufferEx<int>(3)
		{
			MinComparer = Comparer<int>.Default
		};

		buf.PushBack(5);
		buf.Min.Value.AssertEqual(5);

		buf.PushBack(2);
		buf.Min.Value.AssertEqual(2);

		buf.PushBack(8);
		buf.Min.Value.AssertEqual(2);

		// Remove 5, min still 2
		buf.PushBack(10);
		buf.Min.Value.AssertEqual(2);

		// Remove 2, recalc min = 8
		buf.PushBack(15);
		buf.Min.Value.AssertEqual(8);
	}

	[TestMethod]
	public void CircularBufferEx_PushFront_Min()
	{
		var buf = new CircularBufferEx<int>(3)
		{
			MinComparer = Comparer<int>.Default
		};

		buf.PushFront(5);
		buf.Min.Value.AssertEqual(5);

		buf.PushFront(2);
		buf.Min.Value.AssertEqual(2);

		buf.PushFront(8);
		buf.Min.Value.AssertEqual(2);
		buf.ToArray().AssertEqual([8, 2, 5]);

		// PushFront removes LAST (5), min still 2
		buf.PushFront(10);
		buf.Min.Value.AssertEqual(2);
		buf.ToArray().AssertEqual([10, 8, 2]);

		// PushFront removes LAST (2), recalc min = 8
		buf.PushFront(15);
		buf.Min.Value.AssertEqual(8);
		buf.ToArray().AssertEqual([15, 10, 8]);
	}

	[TestMethod]
	public void CircularBufferEx_SumNoFirst()
	{
		var buf = new CircularBufferEx<int>(3) { Operator = new IntOperator() };

		buf.PushBack(1);
		buf.PushBack(2);
		buf.PushBack(3);

		buf.Sum.AssertEqual(6);
		buf.SumNoFirst.AssertEqual(5); // 2 + 3
	}

	[TestMethod]
	public void CircularBufferEx_SumNoFirst_Empty()
	{
		var buf = new CircularBufferEx<int>(3) { Operator = new IntOperator() };
		buf.SumNoFirst.AssertEqual(0);
	}

	[TestMethod]
	public void CircularBufferEx_Clear()
	{
		var buf = new CircularBufferEx<int>(3)
		{
			Operator = new IntOperator(),
			MaxComparer = Comparer<int>.Default,
			MinComparer = Comparer<int>.Default
		};

		buf.PushBack(1);
		buf.PushBack(2);
		buf.PushBack(3);

		buf.Clear();

		buf.Sum.AssertEqual(0);
		buf.Max.HasValue.AssertFalse();
		buf.Min.HasValue.AssertFalse();
		buf.Count.AssertEqual(0);
	}

	[TestMethod]
	public void CircularBufferEx_PopBack()
	{
		var buf = new CircularBufferEx<int>(3)
		{
			Operator = new IntOperator(),
			MaxComparer = Comparer<int>.Default
		};

		buf.PushBack(1);
		buf.PushBack(5);
		buf.PushBack(3);

		buf.PopBack();

		buf.Sum.AssertEqual(6); // 1 + 5
		buf.Max.Value.AssertEqual(5);
		buf.ToArray().AssertEqual([1, 5]);
	}

	[TestMethod]
	public void CircularBufferEx_PopFront()
	{
		var buf = new CircularBufferEx<int>(3)
		{
			Operator = new IntOperator(),
			MaxComparer = Comparer<int>.Default
		};

		buf.PushBack(1);
		buf.PushBack(5);
		buf.PushBack(3);

		buf.PopFront();

		buf.Sum.AssertEqual(8); // 5 + 3
		buf.Max.Value.AssertEqual(5);
		buf.ToArray().AssertEqual([5, 3]);
	}

	[TestMethod]
	public void CircularBufferEx_IndexerSet()
	{
		var buf = new CircularBufferEx<int>(3)
		{
			Operator = new IntOperator(),
			MaxComparer = Comparer<int>.Default
		};

		buf.PushBack(1);
		buf.PushBack(2);
		buf.PushBack(3);

		buf[1] = 10;

		buf.Sum.AssertEqual(14); // 1 + 10 + 3
		buf.Max.Value.AssertEqual(10);
		buf.ToArray().AssertEqual([1, 10, 3]);
	}

	[TestMethod]
	public void CircularBufferEx_CapacityChange()
	{
		var buf = new CircularBufferEx<int>(5)
		{
			Operator = new IntOperator(),
			MaxComparer = Comparer<int>.Default
		};

		buf.PushBack(1);
		buf.PushBack(2);
		buf.PushBack(3);

		buf.Capacity = 3;

		// After capacity change, buffer is cleared
		buf.Sum.AssertEqual(0);
		buf.Max.HasValue.AssertFalse();
		buf.Count.AssertEqual(0);
	}

	[TestMethod]
	public void CircularBufferEx_NoOperator_SumIsDefault()
	{
		var buf = new CircularBufferEx<int>(3);

		buf.PushBack(1);
		buf.PushBack(2);

		buf.Sum.AssertEqual(0);
	}

	[TestMethod]
	public void CircularBufferEx_NoComparer_MaxMinHasNoValue()
	{
		var buf = new CircularBufferEx<int>(3);

		buf.PushBack(1);
		buf.PushBack(2);

		buf.Max.HasValue.AssertFalse();
		buf.Min.HasValue.AssertFalse();
	}

	#endregion
}
