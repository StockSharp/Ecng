namespace Ecng.Tests.Collections;

#region ICircularBufferEx Factory Abstraction

/// <summary>
/// Factory interface for creating ICircularBufferEx instances.
/// </summary>
public interface ICircularBufferExFactory
{
	string Name { get; }
	ICircularBufferEx<int> CreateInt(int capacity);
	ICircularBufferEx<decimal> CreateDecimal(int capacity);
	CircularBuffer<int> AsCircularBufferInt(ICircularBufferEx<int> buf);
	CircularBuffer<decimal> AsCircularBufferDecimal(ICircularBufferEx<decimal> buf);
}

/// <summary>
/// Factory for CircularBufferEx (IOperator-based).
/// </summary>
public class CircularBufferExFactory : ICircularBufferExFactory
{
	public string Name => "CircularBufferEx";

	public ICircularBufferEx<int> CreateInt(int capacity)
	{
		var buf = new CircularBufferEx<int>(capacity);
		buf.Operator = new IntOperator();
		return buf;
	}

	public ICircularBufferEx<decimal> CreateDecimal(int capacity)
	{
		var buf = new CircularBufferEx<decimal>(capacity);
		buf.Operator = new DecimalOperator();
		return buf;
	}

	public CircularBuffer<int> AsCircularBufferInt(ICircularBufferEx<int> buf) => (CircularBuffer<int>)buf;
	public CircularBuffer<decimal> AsCircularBufferDecimal(ICircularBufferEx<decimal> buf) => (CircularBuffer<decimal>)buf;
}

#if NET7_0_OR_GREATER
/// <summary>
/// Factory for NumericCircularBufferEx (INumber-based).
/// </summary>
public class NumericCircularBufferExFactory : ICircularBufferExFactory
{
	public string Name => "NumericCircularBufferEx";

	public ICircularBufferEx<int> CreateInt(int capacity) => new NumericCircularBufferEx<int>(capacity);
	public ICircularBufferEx<decimal> CreateDecimal(int capacity) => new NumericCircularBufferEx<decimal>(capacity);

	public CircularBuffer<int> AsCircularBufferInt(ICircularBufferEx<int> buf) => (CircularBuffer<int>)buf;
	public CircularBuffer<decimal> AsCircularBufferDecimal(ICircularBufferEx<decimal> buf) => (CircularBuffer<decimal>)buf;
}

/// <summary>
/// Helper that runs operations on multiple buffer implementations and compares results.
/// </summary>
public class MultiBufferInt
{
	private static readonly CircularBufferExFactory _operatorFactory = new();
	private static readonly NumericCircularBufferExFactory _numericFactory = new();

	public ICircularBufferEx<int> Buf1 { get; }
	public ICircularBufferEx<int> Buf2 { get; }
	public CircularBuffer<int> Cb1 { get; }
	public CircularBuffer<int> Cb2 { get; }

	public MultiBufferInt(int capacity, CircularBufferStats stats = CircularBufferStats.All)
	{
		Buf1 = _operatorFactory.CreateInt(capacity);
		Buf1.Stats = stats;
		Cb1 = _operatorFactory.AsCircularBufferInt(Buf1);

		Buf2 = _numericFactory.CreateInt(capacity);
		Buf2.Stats = stats;
		Cb2 = _numericFactory.AsCircularBufferInt(Buf2);
	}

	public void PushBack(int value)
	{
		Cb1.PushBack(value);
		Cb2.PushBack(value);
		AssertEqual();
	}

	public void PushFront(int value)
	{
		Cb1.PushFront(value);
		Cb2.PushFront(value);
		AssertEqual();
	}

	public void PopBack()
	{
		Cb1.PopBack();
		Cb2.PopBack();
		AssertEqual();
	}

	public void PopFront()
	{
		Cb1.PopFront();
		Cb2.PopFront();
		AssertEqual();
	}

	public void Clear()
	{
		Cb1.Clear();
		Cb2.Clear();
		AssertEqual();
	}

	public void SetAt(int index, int value)
	{
		Cb1[index] = value;
		Cb2[index] = value;
		AssertEqual();
	}

	public int Capacity
	{
		get => Cb1.Capacity;
		set
		{
			Cb1.Capacity = value;
			Cb2.Capacity = value;
			AssertEqual();
		}
	}

	public void AssertEqual(string context = null)
	{
		var ctx = context != null ? $" ({context})" : "";

		Cb1.Count.AssertEqual(Cb2.Count, $"Count mismatch{ctx}");
		Buf1.Sum.AssertEqual(Buf2.Sum, $"Sum mismatch{ctx}");
		Buf1.Max.HasValue.AssertEqual(Buf2.Max.HasValue, $"Max.HasValue mismatch{ctx}");
		Buf1.Min.HasValue.AssertEqual(Buf2.Min.HasValue, $"Min.HasValue mismatch{ctx}");

		if (Buf1.Max.HasValue)
			Buf1.Max.Value.AssertEqual(Buf2.Max.Value, $"Max.Value mismatch{ctx}");

		if (Buf1.Min.HasValue)
			Buf1.Min.Value.AssertEqual(Buf2.Min.Value, $"Min.Value mismatch{ctx}");

		if (Cb1.Count > 0)
			Buf1.SumNoFirst.AssertEqual(Buf2.SumNoFirst, $"SumNoFirst mismatch{ctx}");

		Cb1.ToArray().AssertEqual(Cb2.ToArray(), $"Content mismatch{ctx}");
	}

	// Assert expected values (checks both implementations against expected)
	public void AssertSum(int expected) => Buf1.Sum.AssertEqual(expected);
	public void AssertMax(int expected) => Buf1.Max.Value.AssertEqual(expected);
	public void AssertMin(int expected) => Buf1.Min.Value.AssertEqual(expected);
	public void AssertSumNoFirst(int expected) => Buf1.SumNoFirst.AssertEqual(expected);
	public void AssertMaxHasValue(bool expected) => Buf1.Max.HasValue.AssertEqual(expected);
	public void AssertMinHasValue(bool expected) => Buf1.Min.HasValue.AssertEqual(expected);
	public void AssertCount(int expected) => Cb1.Count.AssertEqual(expected);
	public void AssertContent(int[] expected) => Cb1.ToArray().AssertEqual(expected);
}

/// <summary>
/// Helper that runs operations on multiple decimal buffer implementations and compares results.
/// </summary>
public class MultiBufferDecimal
{
	private static readonly CircularBufferExFactory _operatorFactory = new();
	private static readonly NumericCircularBufferExFactory _numericFactory = new();

	public ICircularBufferEx<decimal> Buf1 { get; }
	public ICircularBufferEx<decimal> Buf2 { get; }
	public CircularBuffer<decimal> Cb1 { get; }
	public CircularBuffer<decimal> Cb2 { get; }

	public MultiBufferDecimal(int capacity, CircularBufferStats stats = CircularBufferStats.All)
	{
		Buf1 = _operatorFactory.CreateDecimal(capacity);
		Buf1.Stats = stats;
		Cb1 = _operatorFactory.AsCircularBufferDecimal(Buf1);

		Buf2 = _numericFactory.CreateDecimal(capacity);
		Buf2.Stats = stats;
		Cb2 = _numericFactory.AsCircularBufferDecimal(Buf2);
	}

	public void PushBack(decimal value)
	{
		Cb1.PushBack(value);
		Cb2.PushBack(value);
		AssertEqual();
	}

	public void PushFront(decimal value)
	{
		Cb1.PushFront(value);
		Cb2.PushFront(value);
		AssertEqual();
	}

	public void Clear()
	{
		Cb1.Clear();
		Cb2.Clear();
		AssertEqual();
	}

	public int Capacity
	{
		get => Cb1.Capacity;
		set
		{
			Cb1.Capacity = value;
			Cb2.Capacity = value;
			AssertEqual();
		}
	}

	public void AssertEqual(string context = null)
	{
		var ctx = context != null ? $" ({context})" : "";

		Cb1.Count.AssertEqual(Cb2.Count, $"Count mismatch{ctx}");
		Buf1.Sum.AssertEqual(Buf2.Sum, $"Sum mismatch{ctx}");
		Buf1.Max.HasValue.AssertEqual(Buf2.Max.HasValue, $"Max.HasValue mismatch{ctx}");
		Buf1.Min.HasValue.AssertEqual(Buf2.Min.HasValue, $"Min.HasValue mismatch{ctx}");

		if (Buf1.Max.HasValue)
			Buf1.Max.Value.AssertEqual(Buf2.Max.Value, $"Max.Value mismatch{ctx}");

		if (Buf1.Min.HasValue)
			Buf1.Min.Value.AssertEqual(Buf2.Min.Value, $"Min.Value mismatch{ctx}");

		if (Cb1.Count > 0)
			Buf1.SumNoFirst.AssertEqual(Buf2.SumNoFirst, $"SumNoFirst mismatch{ctx}");

		Cb1.ToArray().AssertEqual(Cb2.ToArray(), $"Content mismatch{ctx}");
	}

	public void AssertSum(decimal expected) => Buf1.Sum.AssertEqual(expected);
	public void AssertMax(decimal expected) => Buf1.Max.Value.AssertEqual(expected);
	public void AssertMin(decimal expected) => Buf1.Min.Value.AssertEqual(expected);
	public void AssertSumNoFirst(decimal expected) => Buf1.SumNoFirst.AssertEqual(expected);
	public void AssertMaxHasValue(bool expected) => Buf1.Max.HasValue.AssertEqual(expected);
	public void AssertMinHasValue(bool expected) => Buf1.Min.HasValue.AssertEqual(expected);
	public void AssertCount(int expected) => Cb1.Count.AssertEqual(expected);
}
#endif

#endregion

[TestClass]
public class CircularBufferTests : BaseTestClass
{
	#region Basic CircularBuffer Tests

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

	#endregion

	#region CircularBufferEx Tests

#if NET7_0_OR_GREATER
	// .NET 7+: test both implementations and compare results

	[TestMethod]
	public void CircularBufferEx_PushBack_Sum()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.Sum);

		mb.PushBack(1);
		mb.AssertSum(1);

		mb.PushBack(2);
		mb.AssertSum(3);

		mb.PushBack(3);
		mb.AssertSum(6);

		// Overflow - first element (1) should be removed
		mb.PushBack(4);
		mb.AssertSum(9); // 2 + 3 + 4
		mb.AssertContent([2, 3, 4]);
	}

	[TestMethod]
	public void CircularBufferEx_PushFront_Sum()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.Sum);

		mb.PushFront(1);
		mb.AssertSum(1);

		mb.PushFront(2);
		mb.AssertSum(3);

		mb.PushFront(3);
		mb.AssertSum(6);
		mb.AssertContent([3, 2, 1]);

		// Overflow - LAST element (1) should be removed
		mb.PushFront(4);
		mb.AssertSum(9); // 4 + 3 + 2
		mb.AssertContent([4, 3, 2]);
	}

	[TestMethod]
	public void CircularBufferEx_Max()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.Max);

		mb.PushBack(5);
		mb.AssertMax(5);

		mb.PushBack(10);
		mb.AssertMax(10);

		mb.PushBack(3);
		mb.AssertMax(10);

		// Remove 5, max still 10
		mb.PushBack(1);
		mb.AssertMax(10);

		// Remove 10, should recalc max = 3
		mb.PushBack(2);
		mb.AssertMax(3);
	}

	[TestMethod]
	public void CircularBufferEx_PushFront_Max()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.Max);

		mb.PushFront(5);
		mb.AssertMax(5);

		mb.PushFront(10);
		mb.AssertMax(10);

		mb.PushFront(3);
		mb.AssertMax(10);
		mb.AssertContent([3, 10, 5]);

		// PushFront removes LAST element (5)
		mb.PushFront(1);
		mb.AssertMax(10);
		mb.AssertContent([1, 3, 10]);

		// PushFront removes LAST element (10) - should recalc
		mb.PushFront(2);
		mb.AssertMax(3);
		mb.AssertContent([2, 1, 3]);
	}

	[TestMethod]
	public void CircularBufferEx_PushBack_Min()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.Min);

		mb.PushBack(5);
		mb.AssertMin(5);

		mb.PushBack(2);
		mb.AssertMin(2);

		mb.PushBack(8);
		mb.AssertMin(2);

		// Remove 5, min still 2
		mb.PushBack(10);
		mb.AssertMin(2);

		// Remove 2, recalc min = 8
		mb.PushBack(15);
		mb.AssertMin(8);
	}

	[TestMethod]
	public void CircularBufferEx_PushFront_Min()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.Min);

		mb.PushFront(5);
		mb.AssertMin(5);

		mb.PushFront(2);
		mb.AssertMin(2);

		mb.PushFront(8);
		mb.AssertMin(2);
		mb.AssertContent([8, 2, 5]);

		// PushFront removes LAST (5), min still 2
		mb.PushFront(10);
		mb.AssertMin(2);
		mb.AssertContent([10, 8, 2]);

		// PushFront removes LAST (2), recalc min = 8
		mb.PushFront(15);
		mb.AssertMin(8);
		mb.AssertContent([15, 10, 8]);
	}

	[TestMethod]
	public void CircularBufferEx_SumNoFirst()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.Sum);

		mb.PushBack(1);
		mb.PushBack(2);
		mb.PushBack(3);

		mb.AssertSum(6);
		mb.AssertSumNoFirst(5); // 2 + 3
	}

	[TestMethod]
	public void CircularBufferEx_SumNoFirst_Empty()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.Sum);
		mb.AssertSumNoFirst(0);
	}

	[TestMethod]
	public void CircularBufferEx_Clear()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.All);

		mb.PushBack(1);
		mb.PushBack(2);
		mb.PushBack(3);

		mb.Clear();

		mb.AssertSum(0);
		mb.AssertMaxHasValue(false);
		mb.AssertMinHasValue(false);
		mb.AssertCount(0);
	}

	[TestMethod]
	public void CircularBufferEx_PopBack()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.Sum | CircularBufferStats.Max);

		mb.PushBack(1);
		mb.PushBack(5);
		mb.PushBack(3);

		mb.PopBack();

		mb.AssertSum(6); // 1 + 5
		mb.AssertMax(5);
		mb.AssertContent([1, 5]);
	}

	[TestMethod]
	public void CircularBufferEx_PopFront()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.Sum | CircularBufferStats.Max);

		mb.PushBack(1);
		mb.PushBack(5);
		mb.PushBack(3);
		mb.PopFront();

		mb.AssertSum(8); // 5 + 3
		mb.AssertMax(5);
		mb.AssertContent([5, 3]);
	}

	[TestMethod]
	public void CircularBufferEx_IndexerSet()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.Sum | CircularBufferStats.Max);

		mb.PushBack(1);
		mb.PushBack(2);
		mb.PushBack(3);

		mb.SetAt(1, 10);

		mb.AssertSum(14); // 1 + 10 + 3
		mb.AssertMax(10);
		mb.AssertContent([1, 10, 3]);
	}

	[TestMethod]
	public void CircularBufferEx_CapacityChange()
	{
		var mb = new MultiBufferInt(5, CircularBufferStats.Sum | CircularBufferStats.Max);

		mb.PushBack(1);
		mb.PushBack(2);
		mb.PushBack(3);

		mb.Capacity = 3;

		// After capacity change, buffer is cleared
		mb.AssertSum(0);
		mb.AssertMaxHasValue(false);
		mb.AssertMinHasValue(false);
		mb.AssertCount(0);
	}

	[TestMethod]
	public void CircularBufferEx_WithDecimal()
	{
		var mb = new MultiBufferDecimal(3, CircularBufferStats.All);

		mb.PushBack(1.5m);
		mb.PushBack(2.5m);
		mb.PushBack(3.0m);

		mb.AssertSum(7.0m);
		mb.AssertMax(3.0m);
		mb.AssertMin(1.5m);
	}

	[TestMethod]
	public void CircularBufferEx_Stats_SumOnly()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.Sum);

		mb.PushBack(1);
		mb.PushBack(2);
		mb.PushBack(3);

		mb.AssertSum(6);
		mb.AssertMaxHasValue(false);
		mb.AssertMinHasValue(false);
	}

	[TestMethod]
	public void CircularBufferEx_Stats_MinMaxOnly()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.Min | CircularBufferStats.Max);

		mb.PushBack(1);
		mb.PushBack(5);
		mb.PushBack(3);

		mb.AssertSum(0); // Not calculated
		mb.AssertMax(5);
		mb.AssertMin(1);
	}

	[TestMethod]
	public void CircularBufferEx_Stats_None()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.None);

		mb.PushBack(1);
		mb.PushBack(5);
		mb.PushBack(3);

		mb.AssertSum(0);
		mb.AssertMaxHasValue(false);
		mb.AssertMinHasValue(false);
	}

	[TestMethod]
	public void CircularBufferEx_Stats_All()
	{
		var mb = new MultiBufferInt(3, CircularBufferStats.All);

		mb.PushBack(1);
		mb.PushBack(5);
		mb.PushBack(3);

		mb.AssertSum(9);
		mb.AssertMax(5);
		mb.AssertMin(1);
	}

	[TestMethod]
	public void StatsComputation()
	{
		var mb = new MultiBufferDecimal(3, CircularBufferStats.All);

		mb.PushBack(1m);
		mb.PushBack(2m);
		mb.PushBack(3m);
		mb.AssertSum(6m);
		mb.AssertMax(3m);
		mb.AssertMin(1m);
		mb.AssertSumNoFirst(5m);

		mb.PushBack(4m);
		mb.AssertSum(9m);
		mb.AssertMax(4m);
		mb.AssertMin(2m);

		mb.Clear();
		mb.AssertSum(0m);
		mb.AssertMaxHasValue(false);
		mb.AssertMinHasValue(false);

		mb.PushFront(4m);
		mb.PushFront(2m);
		mb.AssertSum(6m);
		mb.AssertMax(4m);
		mb.AssertMin(2m);
	}

	[TestMethod]
	public void CapacityReset()
	{
		var mb = new MultiBufferInt(2, CircularBufferStats.All);

		mb.PushBack(1);
		mb.PushBack(2);
		mb.Capacity = 3;
		mb.PushBack(3);
		mb.AssertSum(3);
		mb.AssertMax(3);
		mb.AssertMin(3);
	}

	[TestMethod]
	public void CollectionCompatibilityEx()
	{
		var mb = new MultiBufferDecimal(4, CircularBufferStats.All);

		var icol1 = (ICollection<decimal>)mb.Cb1;
		var ilist1 = (IList<decimal>)mb.Cb1;
		var icol2 = (ICollection<decimal>)mb.Cb2;
		var ilist2 = (IList<decimal>)mb.Cb2;

		icol1.Add(10); icol2.Add(10);
		icol1.Add(20); icol2.Add(20);
		icol1.Add(30); icol2.Add(30);
		mb.AssertEqual();
		mb.AssertSum(60);
		mb.AssertMax(30);
		mb.AssertMin(10);
		mb.AssertSumNoFirst(50);

		// Remove
		icol1.Remove(20); icol2.Remove(20);
		mb.AssertEqual();
		mb.AssertSum(40);

		// Insert
		ilist1.Insert(1, 25); ilist2.Insert(1, 25);
		mb.AssertEqual();
		mb.AssertSum(65);

		// RemoveAt
		ilist1.RemoveAt(0); ilist2.RemoveAt(0);
		mb.AssertEqual();
		mb.AssertSum(55);
		mb.AssertMin(25);

		// Set by index
		ilist1[0] = 100; ilist2[0] = 100;
		mb.AssertEqual();
		mb.AssertSum(130);
		mb.AssertMax(100);
	}

	[TestMethod]
	public void RandomOperations_CompareImplementations()
	{
		const int capacity = 17;
		const int iterations = 5_000;

		var mb = new MultiBufferInt(capacity, CircularBufferStats.All);

		for (int i = 0; i < iterations; i++)
		{
			var operation = RandomGen.GetInt() % 6;
			var val = RandomGen.GetInt() % 1000;

			switch (operation)
			{
				case 0:
					mb.PushBack(val);
					break;
				case 1:
					mb.PushFront(val);
					break;
				case 2:
					if (mb.Cb1.Count > 0)
						mb.PopBack();
					break;
				case 3:
					if (mb.Cb1.Count > 0)
						mb.PopFront();
					break;
				case 4:
					if (mb.Cb1.Count > 0)
						mb.SetAt(RandomGen.GetInt() % mb.Cb1.Count, val);
					break;
				case 5:
					if (RandomGen.GetInt() % 100 == 0)
						mb.Clear();
					break;
			}
		}
	}

	[TestMethod]
	public void PushBackStress_CompareImplementations()
	{
		const int capacity = 17;
		const int iterations = 10_000;

		var mb = new MultiBufferInt(capacity, CircularBufferStats.All);

		for (int i = 0; i < iterations; i++)
		{
			mb.PushBack(RandomGen.GetInt());
		}
	}

#else
	// .NET 6: test only CircularBufferEx

	[TestMethod]
	public void CircularBufferEx_PushBack_Sum()
	{
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.Sum, Operator = new IntOperator() };

		buf.PushBack(1);
		buf.Sum.AssertEqual(1);

		buf.PushBack(2);
		buf.Sum.AssertEqual(3);

		buf.PushBack(3);
		buf.Sum.AssertEqual(6);

		buf.PushBack(4);
		buf.Sum.AssertEqual(9);
		buf.ToArray().AssertEqual([2, 3, 4]);
	}

	[TestMethod]
	public void CircularBufferEx_PushFront_Sum()
	{
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.Sum, Operator = new IntOperator() };

		buf.PushFront(1);
		buf.Sum.AssertEqual(1);

		buf.PushFront(2);
		buf.Sum.AssertEqual(3);

		buf.PushFront(3);
		buf.Sum.AssertEqual(6);
		buf.ToArray().AssertEqual([3, 2, 1]);

		buf.PushFront(4);
		buf.Sum.AssertEqual(9);
		buf.ToArray().AssertEqual([4, 3, 2]);
	}

	[TestMethod]
	public void CircularBufferEx_Max()
	{
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.Max, Operator = new IntOperator() };

		buf.PushBack(5);
		buf.Max.Value.AssertEqual(5);

		buf.PushBack(10);
		buf.Max.Value.AssertEqual(10);

		buf.PushBack(3);
		buf.Max.Value.AssertEqual(10);

		buf.PushBack(1);
		buf.Max.Value.AssertEqual(10);

		buf.PushBack(2);
		buf.Max.Value.AssertEqual(3);
	}

	[TestMethod]
	public void CircularBufferEx_PushFront_Max()
	{
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.Max, Operator = new IntOperator() };

		buf.PushFront(5);
		buf.Max.Value.AssertEqual(5);

		buf.PushFront(10);
		buf.Max.Value.AssertEqual(10);

		buf.PushFront(3);
		buf.Max.Value.AssertEqual(10);
		buf.ToArray().AssertEqual([3, 10, 5]);

		buf.PushFront(1);
		buf.Max.Value.AssertEqual(10);
		buf.ToArray().AssertEqual([1, 3, 10]);

		buf.PushFront(2);
		buf.Max.Value.AssertEqual(3);
		buf.ToArray().AssertEqual([2, 1, 3]);
	}

	[TestMethod]
	public void CircularBufferEx_PushBack_Min()
	{
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.Min, Operator = new IntOperator() };

		buf.PushBack(5);
		buf.Min.Value.AssertEqual(5);

		buf.PushBack(2);
		buf.Min.Value.AssertEqual(2);

		buf.PushBack(8);
		buf.Min.Value.AssertEqual(2);

		buf.PushBack(10);
		buf.Min.Value.AssertEqual(2);

		buf.PushBack(15);
		buf.Min.Value.AssertEqual(8);
	}

	[TestMethod]
	public void CircularBufferEx_PushFront_Min()
	{
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.Min, Operator = new IntOperator() };

		buf.PushFront(5);
		buf.Min.Value.AssertEqual(5);

		buf.PushFront(2);
		buf.Min.Value.AssertEqual(2);

		buf.PushFront(8);
		buf.Min.Value.AssertEqual(2);
		buf.ToArray().AssertEqual([8, 2, 5]);

		buf.PushFront(10);
		buf.Min.Value.AssertEqual(2);
		buf.ToArray().AssertEqual([10, 8, 2]);

		buf.PushFront(15);
		buf.Min.Value.AssertEqual(8);
		buf.ToArray().AssertEqual([15, 10, 8]);
	}

	[TestMethod]
	public void CircularBufferEx_SumNoFirst()
	{
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.Sum, Operator = new IntOperator() };

		buf.PushBack(1);
		buf.PushBack(2);
		buf.PushBack(3);

		buf.Sum.AssertEqual(6);
		buf.SumNoFirst.AssertEqual(5);
	}

	[TestMethod]
	public void CircularBufferEx_SumNoFirst_Empty()
	{
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.Sum, Operator = new IntOperator() };
		buf.SumNoFirst.AssertEqual(0);
	}

	[TestMethod]
	public void CircularBufferEx_Clear()
	{
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.All, Operator = new IntOperator() };

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
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.Sum | CircularBufferStats.Max, Operator = new IntOperator() };

		buf.PushBack(1);
		buf.PushBack(5);
		buf.PushBack(3);

		buf.PopBack();

		buf.Sum.AssertEqual(6);
		buf.Max.Value.AssertEqual(5);
		buf.ToArray().AssertEqual([1, 5]);
	}

	[TestMethod]
	public void CircularBufferEx_PopFront()
	{
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.Sum | CircularBufferStats.Max, Operator = new IntOperator() };

		buf.PushBack(1);
		buf.PushBack(5);
		buf.PushBack(3);
		buf.PopFront();

		buf.Sum.AssertEqual(8);
		buf.Max.Value.AssertEqual(5);
		buf.ToArray().AssertEqual([5, 3]);
	}

	[TestMethod]
	public void CircularBufferEx_IndexerSet()
	{
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.Sum | CircularBufferStats.Max, Operator = new IntOperator() };

		buf.PushBack(1);
		buf.PushBack(2);
		buf.PushBack(3);

		buf[1] = 10;

		buf.Sum.AssertEqual(14);
		buf.Max.Value.AssertEqual(10);
		buf.ToArray().AssertEqual([1, 10, 3]);
	}

	[TestMethod]
	public void CircularBufferEx_CapacityChange()
	{
		var buf = new CircularBufferEx<int>(5) { Stats = CircularBufferStats.Sum | CircularBufferStats.Max, Operator = new IntOperator() };

		buf.PushBack(1);
		buf.PushBack(2);
		buf.PushBack(3);

		buf.Capacity = 3;

		buf.Sum.AssertEqual(0);
		buf.Max.HasValue.AssertFalse();
		buf.Min.HasValue.AssertFalse();
		buf.Count.AssertEqual(0);
	}

	[TestMethod]
	public void CircularBufferEx_WithDecimal()
	{
		var buf = new CircularBufferEx<decimal>(3) { Stats = CircularBufferStats.All, Operator = new DecimalOperator() };

		buf.PushBack(1.5m);
		buf.PushBack(2.5m);
		buf.PushBack(3.0m);

		buf.Sum.AssertEqual(7.0m);
		buf.Max.Value.AssertEqual(3.0m);
		buf.Min.Value.AssertEqual(1.5m);
	}

	[TestMethod]
	public void CircularBufferEx_Stats_SumOnly()
	{
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.Sum, Operator = new IntOperator() };

		buf.PushBack(1);
		buf.PushBack(2);
		buf.PushBack(3);

		buf.Sum.AssertEqual(6);
		buf.Max.HasValue.AssertFalse();
		buf.Min.HasValue.AssertFalse();
	}

	[TestMethod]
	public void CircularBufferEx_Stats_MinMaxOnly()
	{
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.Min | CircularBufferStats.Max, Operator = new IntOperator() };

		buf.PushBack(1);
		buf.PushBack(5);
		buf.PushBack(3);

		buf.Sum.AssertEqual(0);
		buf.Max.Value.AssertEqual(5);
		buf.Min.Value.AssertEqual(1);
	}

	[TestMethod]
	public void CircularBufferEx_Stats_None()
	{
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.None, Operator = new IntOperator() };

		buf.PushBack(1);
		buf.PushBack(5);
		buf.PushBack(3);

		buf.Sum.AssertEqual(0);
		buf.Max.HasValue.AssertFalse();
		buf.Min.HasValue.AssertFalse();
	}

	[TestMethod]
	public void CircularBufferEx_Stats_All()
	{
		var buf = new CircularBufferEx<int>(3) { Stats = CircularBufferStats.All, Operator = new IntOperator() };

		buf.PushBack(1);
		buf.PushBack(5);
		buf.PushBack(3);

		buf.Sum.AssertEqual(9);
		buf.Max.Value.AssertEqual(5);
		buf.Min.Value.AssertEqual(1);
	}

	[TestMethod]
	public void StatsComputation()
	{
		var buf = new CircularBufferEx<decimal>(3) { Stats = CircularBufferStats.All, Operator = new DecimalOperator() };

		buf.PushBack(1m);
		buf.PushBack(2m);
		buf.PushBack(3m);
		buf.Sum.AssertEqual(6m);
		buf.Max.Value.AssertEqual(3m);
		buf.Min.Value.AssertEqual(1m);
		buf.SumNoFirst.AssertEqual(5m);
		buf.PushBack(4m);
		buf.Sum.AssertEqual(9m);
		buf.Max.Value.AssertEqual(4m);
		buf.Min.Value.AssertEqual(2m);
		buf.Clear();
		buf.Sum.AssertEqual(0m);
		buf.Max.HasValue.AssertFalse();
		buf.Min.HasValue.AssertFalse();

		buf.PushFront(4m);
		buf.PushFront(2m);
		buf.Sum.AssertEqual(6m);
		buf.Max.Value.AssertEqual(4m);
		buf.Min.Value.AssertEqual(2m);
	}

	[TestMethod]
	public void CapacityReset()
	{
		var buf = new CircularBufferEx<int>(2) { Stats = CircularBufferStats.All, Operator = new IntOperator() };

		buf.PushBack(1);
		buf.PushBack(2);
		buf.Capacity = 3;
		buf.PushBack(3);
		buf.Sum.AssertEqual(3);
		buf.Max.Value.AssertEqual(3);
		buf.Min.Value.AssertEqual(3);
	}

	[TestMethod]
	public void CollectionCompatibilityEx()
	{
		var buf = new CircularBufferEx<decimal>(4) { Stats = CircularBufferStats.All, Operator = new DecimalOperator() };
		var icol = (ICollection<decimal>)buf;
		var ilist = (IList<decimal>)buf;

		icol.Add(10);
		icol.Add(20);
		icol.Add(30);
		buf.Sum.AssertEqual(60);
		buf.Max.Value.AssertEqual(30);
		buf.Min.Value.AssertEqual(10);
		buf.SumNoFirst.AssertEqual(50);

		icol.Remove(20).AssertTrue();
		buf.Sum.AssertEqual(40);
		buf.Max.Value.AssertEqual(30);
		buf.Min.Value.AssertEqual(10);
		buf.SumNoFirst.AssertEqual(30);

		ilist.Insert(1, 25);
		buf.Sum.AssertEqual(65);
		buf.Max.Value.AssertEqual(30);
		buf.Min.Value.AssertEqual(10);
		buf.SumNoFirst.AssertEqual(55);

		ilist.RemoveAt(0);
		buf.Sum.AssertEqual(55);
		buf.Max.Value.AssertEqual(30);
		buf.Min.Value.AssertEqual(25);
		buf.SumNoFirst.AssertEqual(30);

		ilist[0] = 100;
		buf.Sum.AssertEqual(130);
		buf.Max.Value.AssertEqual(100);
		buf.Min.Value.AssertEqual(30);
		buf.SumNoFirst.AssertEqual(30);
	}

	[TestMethod]
	public void CircularBufferEx_BackwardCompatibility_OperatorOnly()
	{
		var buf = new CircularBufferEx<int>(3) { Operator = new IntOperator() };

		buf.PushBack(1);
		buf.PushBack(2);
		buf.PushBack(3);

		buf.Sum.AssertEqual(6);
		buf.Max.HasValue.AssertFalse();
		buf.Min.HasValue.AssertFalse();
	}

	[TestMethod]
	public void CircularBufferEx_BackwardCompatibility_AllComparers()
	{
		var buf = new CircularBufferEx<int>(3)
		{
			Operator = new IntOperator(),
#pragma warning disable CS0618
			MaxComparer = Comparer<int>.Default,
			MinComparer = Comparer<int>.Default
#pragma warning restore CS0618
		};

		buf.PushBack(1);
		buf.PushBack(5);
		buf.PushBack(3);

		buf.Sum.AssertEqual(9);
		buf.Max.Value.AssertEqual(5);
		buf.Min.Value.AssertEqual(1);
	}

	[TestMethod]
	public void CircularBufferEx_BackwardCompatibility_OperatorAsComparer()
	{
		var buf = new CircularBufferEx<int>(3)
		{
			Operator = new IntOperator(),
#pragma warning disable CS0618
			MaxComparer = null,
			MinComparer = null,
#pragma warning restore CS0618
			Stats = CircularBufferStats.All
		};

		buf.PushBack(1);
		buf.PushBack(5);
		buf.PushBack(3);

		buf.Sum.AssertEqual(9);
		buf.Max.Value.AssertEqual(5);
		buf.Min.Value.AssertEqual(1);
	}

	[TestMethod]
	public void CircularBufferEx_StatsOverridesProperties()
	{
		var buf = new CircularBufferEx<int>(3)
		{
			Operator = new IntOperator(),
#pragma warning disable CS0618
			MaxComparer = Comparer<int>.Default,
			MinComparer = Comparer<int>.Default,
#pragma warning restore CS0618
			Stats = CircularBufferStats.Sum
		};

		buf.PushBack(1);
		buf.PushBack(5);
		buf.PushBack(3);

		buf.Sum.AssertEqual(9);
		buf.Max.HasValue.AssertFalse();
		buf.Min.HasValue.AssertFalse();
	}

	[TestMethod]
	public void CircularBufferEx_Stats_AutoSetByOperator()
	{
		var buf = new CircularBufferEx<int>(3);
		buf.Stats.AssertEqual(CircularBufferStats.None);

		buf.Operator = new IntOperator();
		buf.Stats.AssertEqual(CircularBufferStats.Sum);

		buf.Operator = null;
		buf.Stats.AssertEqual(CircularBufferStats.None);
	}

	[TestMethod]
	public void CircularBufferEx_Stats_AutoSetByMinComparer()
	{
		var buf = new CircularBufferEx<int>(3);
		buf.Stats.AssertEqual(CircularBufferStats.None);

#pragma warning disable CS0618
		buf.MinComparer = Comparer<int>.Default;
		buf.Stats.AssertEqual(CircularBufferStats.Min);

		buf.MinComparer = null;
		buf.Stats.AssertEqual(CircularBufferStats.None);
#pragma warning restore CS0618
	}

	[TestMethod]
	public void CircularBufferEx_Stats_AutoSetByMaxComparer()
	{
		var buf = new CircularBufferEx<int>(3);
		buf.Stats.AssertEqual(CircularBufferStats.None);

#pragma warning disable CS0618
		buf.MaxComparer = Comparer<int>.Default;
		buf.Stats.AssertEqual(CircularBufferStats.Max);

		buf.MaxComparer = null;
		buf.Stats.AssertEqual(CircularBufferStats.None);
#pragma warning restore CS0618
	}

	[TestMethod]
	public void CircularBufferEx_Stats_AutoSetCombination()
	{
		var buf = new CircularBufferEx<int>(3)
		{
			Operator = new IntOperator()
		};
		buf.Stats.AssertEqual(CircularBufferStats.Sum);

#pragma warning disable CS0618
		buf.MinComparer = Comparer<int>.Default;
		buf.Stats.AssertEqual(CircularBufferStats.Sum | CircularBufferStats.Min);

		buf.MaxComparer = Comparer<int>.Default;
		buf.Stats.AssertEqual(CircularBufferStats.All);
#pragma warning restore CS0618

		buf.Operator = null;
		buf.Stats.AssertEqual(CircularBufferStats.Min | CircularBufferStats.Max);
	}

	[TestMethod]
	public void CircularBufferEx_Stats_ExplicitOverridesAuto()
	{
		var buf = new CircularBufferEx<int>(3)
		{
			Operator = new IntOperator()
		};
		buf.Stats.AssertEqual(CircularBufferStats.Sum);

		buf.Stats = CircularBufferStats.Max;
		buf.Stats.AssertEqual(CircularBufferStats.Max);

#pragma warning disable CS0618
		buf.MinComparer = Comparer<int>.Default;
		buf.Stats.AssertEqual(CircularBufferStats.Max);
#pragma warning restore CS0618

		buf.Operator = null;
		buf.Stats.AssertEqual(CircularBufferStats.Max);
	}
#endif

	#endregion
}
