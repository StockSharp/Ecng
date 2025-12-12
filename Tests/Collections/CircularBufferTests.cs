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

	#region CircularBufferEx Factories

	private static IEnumerable<ICircularBufferExFactory> GetFactories()
	{
		yield return new CircularBufferExFactory();
#if NET7_0_OR_GREATER
		yield return new NumericCircularBufferExFactory();
#endif
	}

#if NET7_0_OR_GREATER
	private static readonly ICircularBufferExFactory _operatorFactory = new CircularBufferExFactory();
	private static readonly ICircularBufferExFactory _numericFactory = new NumericCircularBufferExFactory();

	/// <summary>
	/// Helper to run action on both implementations and compare results.
	/// </summary>
	private static void RunAndCompare<T>(
		Func<ICircularBufferExFactory, T> action,
		string description = null)
	{
		var result1 = action(_operatorFactory);
		var result2 = action(_numericFactory);

		if (!EqualityComparer<T>.Default.Equals(result1, result2))
		{
			throw new AssertFailedException(
				$"Results differ between implementations{(description != null ? $" ({description})" : "")}. " +
				$"{_operatorFactory.Name}: {result1}, {_numericFactory.Name}: {result2}");
		}
	}

	/// <summary>
	/// Helper to run action on both implementations and compare complex results.
	/// </summary>
	private static void RunAndCompareBuffers(
		Action<ICircularBufferExFactory, ICircularBufferEx<int>, CircularBuffer<int>> setup,
		Action<ICircularBufferEx<int>, CircularBuffer<int>, ICircularBufferEx<int>, CircularBuffer<int>> compare)
	{
		var buf1 = _operatorFactory.CreateInt(10);
		var cb1 = _operatorFactory.AsCircularBufferInt(buf1);

		var buf2 = _numericFactory.CreateInt(10);
		var cb2 = _numericFactory.AsCircularBufferInt(buf2);

		setup(_operatorFactory, buf1, cb1);
		setup(_numericFactory, buf2, cb2);

		compare(buf1, cb1, buf2, cb2);
	}
#endif

	#endregion

	#region CircularBufferEx Tests - All Implementations

	[TestMethod]
	public void CircularBufferEx_PushBack_Sum()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum;

			var cb = factory.AsCircularBufferInt(buf);

			cb.PushBack(1);
			buf.Sum.AssertEqual(1, $"{factory.Name}: Sum after first push");

			cb.PushBack(2);
			buf.Sum.AssertEqual(3, $"{factory.Name}: Sum after second push");

			cb.PushBack(3);
			buf.Sum.AssertEqual(6, $"{factory.Name}: Sum after third push");

			// Overflow - first element (1) should be removed
			cb.PushBack(4);
			buf.Sum.AssertEqual(9, $"{factory.Name}: Sum after overflow"); // 2 + 3 + 4
			cb.ToArray().AssertEqual([2, 3, 4], $"{factory.Name}: Array after overflow");
		}

#if NET7_0_OR_GREATER
		// Compare implementations directly
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushBack(1);
			cb.PushBack(2);
			cb.PushBack(3);
			cb.PushBack(4);
			return buf.Sum;
		}, "PushBack_Sum final value");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_PushFront_Sum()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushFront(1);
			buf.Sum.AssertEqual(1, $"{factory.Name}");

			cb.PushFront(2);
			buf.Sum.AssertEqual(3, $"{factory.Name}");

			cb.PushFront(3);
			buf.Sum.AssertEqual(6, $"{factory.Name}");
			cb.ToArray().AssertEqual([3, 2, 1], $"{factory.Name}");

			// Overflow - LAST element (1) should be removed, not first
			cb.PushFront(4);
			buf.Sum.AssertEqual(9, $"{factory.Name}"); // 4 + 3 + 2
			cb.ToArray().AssertEqual([4, 3, 2], $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushFront(1);
			cb.PushFront(2);
			cb.PushFront(3);
			cb.PushFront(4);
			return (buf.Sum, string.Join(",", cb.ToArray()));
		}, "PushFront_Sum");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_Max()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.Max;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushBack(5);
			buf.Max.Value.AssertEqual(5, $"{factory.Name}");

			cb.PushBack(10);
			buf.Max.Value.AssertEqual(10, $"{factory.Name}");

			cb.PushBack(3);
			buf.Max.Value.AssertEqual(10, $"{factory.Name}");

			// Remove 5, max still 10
			cb.PushBack(1);
			buf.Max.Value.AssertEqual(10, $"{factory.Name}");

			// Remove 10, should recalc max = 3
			cb.PushBack(2);
			buf.Max.Value.AssertEqual(3, $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.Max;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushBack(5);
			cb.PushBack(10);
			cb.PushBack(3);
			cb.PushBack(1);
			cb.PushBack(2);
			return buf.Max.Value;
		}, "Max value");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_PushFront_Max()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.Max;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushFront(5);
			buf.Max.Value.AssertEqual(5, $"{factory.Name}");

			cb.PushFront(10);
			buf.Max.Value.AssertEqual(10, $"{factory.Name}");

			cb.PushFront(3);
			buf.Max.Value.AssertEqual(10, $"{factory.Name}");
			cb.ToArray().AssertEqual([3, 10, 5], $"{factory.Name}");

			// PushFront removes LAST element (5)
			cb.PushFront(1);
			buf.Max.Value.AssertEqual(10, $"{factory.Name}");
			cb.ToArray().AssertEqual([1, 3, 10], $"{factory.Name}");

			// PushFront removes LAST element (10) - should recalc
			cb.PushFront(2);
			buf.Max.Value.AssertEqual(3, $"{factory.Name}");
			cb.ToArray().AssertEqual([2, 1, 3], $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.Max;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushFront(5);
			cb.PushFront(10);
			cb.PushFront(3);
			cb.PushFront(1);
			cb.PushFront(2);
			return (buf.Max.Value, string.Join(",", cb.ToArray()));
		}, "PushFront_Max");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_PushBack_Min()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.Min;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushBack(5);
			buf.Min.Value.AssertEqual(5, $"{factory.Name}");

			cb.PushBack(2);
			buf.Min.Value.AssertEqual(2, $"{factory.Name}");

			cb.PushBack(8);
			buf.Min.Value.AssertEqual(2, $"{factory.Name}");

			// Remove 5, min still 2
			cb.PushBack(10);
			buf.Min.Value.AssertEqual(2, $"{factory.Name}");

			// Remove 2, recalc min = 8
			cb.PushBack(15);
			buf.Min.Value.AssertEqual(8, $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.Min;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushBack(5);
			cb.PushBack(2);
			cb.PushBack(8);
			cb.PushBack(10);
			cb.PushBack(15);
			return buf.Min.Value;
		}, "PushBack_Min");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_PushFront_Min()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.Min;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushFront(5);
			buf.Min.Value.AssertEqual(5, $"{factory.Name}");

			cb.PushFront(2);
			buf.Min.Value.AssertEqual(2, $"{factory.Name}");

			cb.PushFront(8);
			buf.Min.Value.AssertEqual(2, $"{factory.Name}");
			cb.ToArray().AssertEqual([8, 2, 5], $"{factory.Name}");

			// PushFront removes LAST (5), min still 2
			cb.PushFront(10);
			buf.Min.Value.AssertEqual(2, $"{factory.Name}");
			cb.ToArray().AssertEqual([10, 8, 2], $"{factory.Name}");

			// PushFront removes LAST (2), recalc min = 8
			cb.PushFront(15);
			buf.Min.Value.AssertEqual(8, $"{factory.Name}");
			cb.ToArray().AssertEqual([15, 10, 8], $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.Min;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushFront(5);
			cb.PushFront(2);
			cb.PushFront(8);
			cb.PushFront(10);
			cb.PushFront(15);
			return (buf.Min.Value, string.Join(",", cb.ToArray()));
		}, "PushFront_Min");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_SumNoFirst()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushBack(1);
			cb.PushBack(2);
			cb.PushBack(3);

			buf.Sum.AssertEqual(6, $"{factory.Name}");
			buf.SumNoFirst.AssertEqual(5, $"{factory.Name}"); // 2 + 3
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushBack(1);
			cb.PushBack(2);
			cb.PushBack(3);
			return (buf.Sum, buf.SumNoFirst);
		}, "SumNoFirst");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_SumNoFirst_Empty()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum;
			buf.SumNoFirst.AssertEqual(0, $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum;
			return buf.SumNoFirst;
		}, "SumNoFirst_Empty");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_Clear()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.All;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushBack(1);
			cb.PushBack(2);
			cb.PushBack(3);

			cb.Clear();

			buf.Sum.AssertEqual(0, $"{factory.Name}");
			buf.Max.HasValue.AssertFalse($"{factory.Name}");
			buf.Min.HasValue.AssertFalse($"{factory.Name}");
			cb.Count.AssertEqual(0, $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.All;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushBack(1);
			cb.PushBack(2);
			cb.PushBack(3);
			cb.Clear();
			return (buf.Sum, buf.Max.HasValue, buf.Min.HasValue, cb.Count);
		}, "Clear");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_PopBack()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum | CircularBufferStats.Max;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushBack(1);
			cb.PushBack(5);
			cb.PushBack(3);

			cb.PopBack();

			buf.Sum.AssertEqual(6, $"{factory.Name}"); // 1 + 5
			buf.Max.Value.AssertEqual(5, $"{factory.Name}");
			cb.ToArray().AssertEqual([1, 5], $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum | CircularBufferStats.Max;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushBack(1);
			cb.PushBack(5);
			cb.PushBack(3);
			cb.PopBack();
			return (buf.Sum, buf.Max.Value, string.Join(",", cb.ToArray()));
		}, "PopBack");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_PopFront()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum | CircularBufferStats.Max;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushBack(1);
			cb.PushBack(5);
			cb.PushBack(3);
			cb.PopFront();

			buf.Sum.AssertEqual(8, $"{factory.Name}"); // 5 + 3
			buf.Max.Value.AssertEqual(5, $"{factory.Name}");
			cb.ToArray().AssertEqual([5, 3], $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum | CircularBufferStats.Max;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushBack(1);
			cb.PushBack(5);
			cb.PushBack(3);
			cb.PopFront();
			return (buf.Sum, buf.Max.Value, string.Join(",", cb.ToArray()));
		}, "PopFront");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_IndexerSet()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum | CircularBufferStats.Max;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushBack(1);
			cb.PushBack(2);
			cb.PushBack(3);

			cb[1] = 10;

			buf.Sum.AssertEqual(14, $"{factory.Name}"); // 1 + 10 + 3
			buf.Max.Value.AssertEqual(10, $"{factory.Name}");
			cb.ToArray().AssertEqual([1, 10, 3], $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum | CircularBufferStats.Max;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushBack(1);
			cb.PushBack(2);
			cb.PushBack(3);
			cb[1] = 10;
			return (buf.Sum, buf.Max.Value, string.Join(",", cb.ToArray()));
		}, "IndexerSet");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_CapacityChange()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(5);
			buf.Stats = CircularBufferStats.Sum | CircularBufferStats.Max;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushBack(1);
			cb.PushBack(2);
			cb.PushBack(3);

			cb.Capacity = 3;

			// After capacity change, buffer is cleared
			buf.Sum.AssertEqual(0, $"{factory.Name}");
			buf.Max.HasValue.AssertFalse($"{factory.Name}");
			buf.Min.HasValue.AssertFalse($"{factory.Name}");
			cb.Count.AssertEqual(0, $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(5);
			buf.Stats = CircularBufferStats.Sum | CircularBufferStats.Max;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushBack(1);
			cb.PushBack(2);
			cb.PushBack(3);
			cb.Capacity = 3;
			return (buf.Sum, buf.Max.HasValue, buf.Min.HasValue, cb.Count);
		}, "CapacityChange");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_WithDecimal()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateDecimal(3);
			buf.Stats = CircularBufferStats.All;
			var cb = factory.AsCircularBufferDecimal(buf);

			cb.PushBack(1.5m);
			cb.PushBack(2.5m);
			cb.PushBack(3.0m);

			buf.Sum.AssertEqual(7.0m, $"{factory.Name}");
			buf.Max.Value.AssertEqual(3.0m, $"{factory.Name}");
			buf.Min.Value.AssertEqual(1.5m, $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateDecimal(3);
			buf.Stats = CircularBufferStats.All;
			var cb = f.AsCircularBufferDecimal(buf);
			cb.PushBack(1.5m);
			cb.PushBack(2.5m);
			cb.PushBack(3.0m);
			return (buf.Sum, buf.Max.Value, buf.Min.Value);
		}, "WithDecimal");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_DefaultStats_None()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			// Stats = None by default (or Sum for CircularBufferEx with Operator)
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushBack(1);
			cb.PushBack(2);

			buf.Max.HasValue.AssertFalse($"{factory.Name}");
			buf.Min.HasValue.AssertFalse($"{factory.Name}");
		}
	}

	[TestMethod]
	public void CircularBufferEx_Stats_SumOnly()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushBack(1);
			cb.PushBack(2);
			cb.PushBack(3);

			buf.Sum.AssertEqual(6, $"{factory.Name}");
			buf.Max.HasValue.AssertFalse($"{factory.Name}");
			buf.Min.HasValue.AssertFalse($"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.Sum;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushBack(1);
			cb.PushBack(2);
			cb.PushBack(3);
			return (buf.Sum, buf.Max.HasValue, buf.Min.HasValue);
		}, "Stats_SumOnly");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_Stats_MinMaxOnly()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.Min | CircularBufferStats.Max;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushBack(1);
			cb.PushBack(5);
			cb.PushBack(3);

			buf.Sum.AssertEqual(0, $"{factory.Name}"); // Not calculated
			buf.Max.Value.AssertEqual(5, $"{factory.Name}");
			buf.Min.Value.AssertEqual(1, $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.Min | CircularBufferStats.Max;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushBack(1);
			cb.PushBack(5);
			cb.PushBack(3);
			return (buf.Sum, buf.Max.Value, buf.Min.Value);
		}, "Stats_MinMaxOnly");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_Stats_None()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.None;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushBack(1);
			cb.PushBack(5);
			cb.PushBack(3);

			buf.Sum.AssertEqual(0, $"{factory.Name}");
			buf.Max.HasValue.AssertFalse($"{factory.Name}");
			buf.Min.HasValue.AssertFalse($"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.None;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushBack(1);
			cb.PushBack(5);
			cb.PushBack(3);
			return (buf.Sum, buf.Max.HasValue, buf.Min.HasValue);
		}, "Stats_None");
#endif
	}

	[TestMethod]
	public void CircularBufferEx_Stats_All()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(3);
			buf.Stats = CircularBufferStats.All;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushBack(1);
			cb.PushBack(5);
			cb.PushBack(3);

			buf.Sum.AssertEqual(9, $"{factory.Name}");
			buf.Max.Value.AssertEqual(5, $"{factory.Name}");
			buf.Min.Value.AssertEqual(1, $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(3);
			buf.Stats = CircularBufferStats.All;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushBack(1);
			cb.PushBack(5);
			cb.PushBack(3);
			return (buf.Sum, buf.Max.Value, buf.Min.Value);
		}, "Stats_All");
#endif
	}

	[TestMethod]
	public void StatsComputation()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateDecimal(3);
			buf.Stats = CircularBufferStats.All;
			var cb = factory.AsCircularBufferDecimal(buf);

			cb.PushBack(1m);
			cb.PushBack(2m);
			cb.PushBack(3m);
			buf.Sum.AssertEqual(6m, $"{factory.Name}");
			buf.Max.Value.AssertEqual(3m, $"{factory.Name}");
			buf.Min.Value.AssertEqual(1m, $"{factory.Name}");
			buf.SumNoFirst.AssertEqual(5m, $"{factory.Name}");
			cb.PushBack(4m);
			buf.Sum.AssertEqual(9m, $"{factory.Name}");
			buf.Max.Value.AssertEqual(4m, $"{factory.Name}");
			buf.Min.Value.AssertEqual(2m, $"{factory.Name}");
			cb.Clear();
			buf.Sum.AssertEqual(0m, $"{factory.Name}");
			buf.Max.HasValue.AssertFalse($"{factory.Name}");
			buf.Min.HasValue.AssertFalse($"{factory.Name}");

			cb.PushFront(4m);
			cb.PushFront(2m);
			buf.Sum.AssertEqual(6m, $"{factory.Name}");
			buf.Max.Value.AssertEqual(4m, $"{factory.Name}");
			buf.Min.Value.AssertEqual(2m, $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateDecimal(3);
			buf.Stats = CircularBufferStats.All;
			var cb = f.AsCircularBufferDecimal(buf);
			cb.PushBack(1m);
			cb.PushBack(2m);
			cb.PushBack(3m);
			var r1 = (buf.Sum, buf.Max.Value, buf.Min.Value, buf.SumNoFirst);
			cb.PushBack(4m);
			var r2 = (buf.Sum, buf.Max.Value, buf.Min.Value);
			cb.Clear();
			var r3 = (buf.Sum, buf.Max.HasValue, buf.Min.HasValue);
			cb.PushFront(4m);
			cb.PushFront(2m);
			var r4 = (buf.Sum, buf.Max.Value, buf.Min.Value);
			return (r1, r2, r3, r4);
		}, "StatsComputation");
#endif
	}

	[TestMethod]
	public void CapacityReset()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateInt(2);
			buf.Stats = CircularBufferStats.All;
			var cb = factory.AsCircularBufferInt(buf);

			cb.PushBack(1);
			cb.PushBack(2);
			cb.Capacity = 3;
			cb.PushBack(3);
			buf.Sum.AssertEqual(3, $"{factory.Name}");
			buf.Max.Value.AssertEqual(3, $"{factory.Name}");
			buf.Min.Value.AssertEqual(3, $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateInt(2);
			buf.Stats = CircularBufferStats.All;
			var cb = f.AsCircularBufferInt(buf);
			cb.PushBack(1);
			cb.PushBack(2);
			cb.Capacity = 3;
			cb.PushBack(3);
			return (buf.Sum, buf.Max.Value, buf.Min.Value);
		}, "CapacityReset");
#endif
	}

	[TestMethod]
	public void CollectionCompatibilityEx()
	{
		foreach (var factory in GetFactories())
		{
			var buf = factory.CreateDecimal(4);
			buf.Stats = CircularBufferStats.All;
			var cb = factory.AsCircularBufferDecimal(buf);

			var icol = (ICollection<decimal>)cb;
			var ilist = (IList<decimal>)cb;

			icol.Add(10);
			icol.Add(20);
			icol.Add(30);
			buf.Sum.AssertEqual(60, $"{factory.Name}");
			buf.Max.Value.AssertEqual(30, $"{factory.Name}");
			buf.Min.Value.AssertEqual(10, $"{factory.Name}");
			buf.SumNoFirst.AssertEqual(50, $"{factory.Name}");

			// Remove
			icol.Remove(20).AssertTrue($"{factory.Name}");
			buf.Sum.AssertEqual(40, $"{factory.Name}");
			buf.Max.Value.AssertEqual(30, $"{factory.Name}");
			buf.Min.Value.AssertEqual(10, $"{factory.Name}");
			buf.SumNoFirst.AssertEqual(30, $"{factory.Name}");

			// Insert
			ilist.Insert(1, 25);
			buf.Sum.AssertEqual(65, $"{factory.Name}");
			buf.Max.Value.AssertEqual(30, $"{factory.Name}");
			buf.Min.Value.AssertEqual(10, $"{factory.Name}");
			buf.SumNoFirst.AssertEqual(55, $"{factory.Name}");

			// RemoveAt
			ilist.RemoveAt(0);
			buf.Sum.AssertEqual(55, $"{factory.Name}");
			buf.Max.Value.AssertEqual(30, $"{factory.Name}");
			buf.Min.Value.AssertEqual(25, $"{factory.Name}");
			buf.SumNoFirst.AssertEqual(30, $"{factory.Name}");

			// Set by index
			ilist[0] = 100;
			buf.Sum.AssertEqual(130, $"{factory.Name}");
			buf.Max.Value.AssertEqual(100, $"{factory.Name}");
			buf.Min.Value.AssertEqual(30, $"{factory.Name}");
			buf.SumNoFirst.AssertEqual(30, $"{factory.Name}");
		}

#if NET7_0_OR_GREATER
		RunAndCompare(f =>
		{
			var buf = f.CreateDecimal(4);
			buf.Stats = CircularBufferStats.All;
			var cb = f.AsCircularBufferDecimal(buf);
			var icol = (ICollection<decimal>)cb;
			var ilist = (IList<decimal>)cb;
			icol.Add(10);
			icol.Add(20);
			icol.Add(30);
			icol.Remove(20);
			ilist.Insert(1, 25);
			ilist.RemoveAt(0);
			ilist[0] = 100;
			return (buf.Sum, buf.Max.Value, buf.Min.Value, buf.SumNoFirst);
		}, "CollectionCompatibilityEx");
#endif
	}

	#endregion

	#region Stress Test - Compare Implementations

#if NET7_0_OR_GREATER
	[TestMethod]
	public void RandomOperations_CompareImplementations()
	{
		const int capacity = 17;
		const int iterations = 5_000;

		var buf1 = _operatorFactory.CreateInt(capacity);
		buf1.Stats = CircularBufferStats.All;
		var cb1 = _operatorFactory.AsCircularBufferInt(buf1);

		var buf2 = _numericFactory.CreateInt(capacity);
		buf2.Stats = CircularBufferStats.All;
		var cb2 = _numericFactory.AsCircularBufferInt(buf2);

		for (int i = 0; i < iterations; i++)
		{
			var operation = RandomGen.GetInt() % 6;
			var val = RandomGen.GetInt() % 1000;

			switch (operation)
			{
				case 0: // PushBack
					cb1.PushBack(val);
					cb2.PushBack(val);
					break;
				case 1: // PushFront
					cb1.PushFront(val);
					cb2.PushFront(val);
					break;
				case 2: // PopBack (if not empty)
					if (cb1.Count > 0)
					{
						cb1.PopBack();
						cb2.PopBack();
					}
					break;
				case 3: // PopFront (if not empty)
					if (cb1.Count > 0)
					{
						cb1.PopFront();
						cb2.PopFront();
					}
					break;
				case 4: // Set by index (if not empty)
					if (cb1.Count > 0)
					{
						var idx = RandomGen.GetInt() % cb1.Count;
						cb1[idx] = val;
						cb2[idx] = val;
					}
					break;
				case 5: // Clear (occasionally)
					if (RandomGen.GetInt() % 100 == 0)
					{
						cb1.Clear();
						cb2.Clear();
					}
					break;
			}

			// Compare results
			cb1.Count.AssertEqual(cb2.Count, $"Iteration {i}: Count mismatch");
			buf1.Sum.AssertEqual(buf2.Sum, $"Iteration {i}: Sum mismatch");
			buf1.Max.HasValue.AssertEqual(buf2.Max.HasValue, $"Iteration {i}: Max.HasValue mismatch");
			buf1.Min.HasValue.AssertEqual(buf2.Min.HasValue, $"Iteration {i}: Min.HasValue mismatch");

			if (buf1.Max.HasValue)
				buf1.Max.Value.AssertEqual(buf2.Max.Value, $"Iteration {i}: Max.Value mismatch");

			if (buf1.Min.HasValue)
				buf1.Min.Value.AssertEqual(buf2.Min.Value, $"Iteration {i}: Min.Value mismatch");

			if (cb1.Count > 0)
				buf1.SumNoFirst.AssertEqual(buf2.SumNoFirst, $"Iteration {i}: SumNoFirst mismatch");

			// Compare content
			cb1.ToArray().AssertEqual(cb2.ToArray(), $"Iteration {i}: Content mismatch");
		}
	}

	[TestMethod]
	public void PushBackStress_CompareImplementations()
	{
		const int capacity = 17;
		const int iterations = 10_000;

		var buf1 = _operatorFactory.CreateInt(capacity);
		buf1.Stats = CircularBufferStats.All;
		var cb1 = _operatorFactory.AsCircularBufferInt(buf1);

		var buf2 = _numericFactory.CreateInt(capacity);
		buf2.Stats = CircularBufferStats.All;
		var cb2 = _numericFactory.AsCircularBufferInt(buf2);

		for (int i = 0; i < iterations; i++)
		{
			var val = RandomGen.GetInt();

			cb1.PushBack(val);
			cb2.PushBack(val);

			// Compare stats
			buf1.Sum.AssertEqual(buf2.Sum, $"Iteration {i}: Sum");
			buf1.Max.Value.AssertEqual(buf2.Max.Value, $"Iteration {i}: Max");
			buf1.Min.Value.AssertEqual(buf2.Min.Value, $"Iteration {i}: Min");

			// Compare content
			cb1.ToArray().AssertEqual(cb2.ToArray(), $"Iteration {i}: Content");
		}
	}
#endif

	#endregion

	#region CircularBufferEx-specific Legacy Tests (Only for CircularBufferEx)

#if !NET7_0_OR_GREATER
	[TestMethod]
	public void CircularBufferEx_BackwardCompatibility_OperatorOnly()
	{
		// Old code that sets only Operator should calculate Sum
		var buf = new CircularBufferEx<int>(3) { Operator = new IntOperator() };

		buf.PushBack(1);
		buf.PushBack(2);
		buf.PushBack(3);

		buf.Sum.AssertEqual(6);
		buf.Max.HasValue.AssertFalse(); // No MaxComparer set
		buf.Min.HasValue.AssertFalse(); // No MinComparer set
	}

	[TestMethod]
	public void CircularBufferEx_BackwardCompatibility_AllComparers()
	{
		// Old code that sets all comparers
		var buf = new CircularBufferEx<int>(3)
		{
			Operator = new IntOperator(),
#pragma warning disable CS0618 // Type or member is obsolete
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
        // Old code that uses Operator as comparer fallback
        var buf = new CircularBufferEx<int>(3)
        {
            Operator = new IntOperator(),
#pragma warning disable CS0618 // Type or member is obsolete
			MaxComparer = null, // Explicitly null, should use Operator
            MinComparer = null, // Explicitly null, should use Operator
#pragma warning restore CS0618

            // Stats not set - should infer from properties
            Stats = CircularBufferStats.All
        };

        buf.PushBack(1);
		buf.PushBack(5);
		buf.PushBack(3);

		buf.Sum.AssertEqual(9);
		buf.Max.Value.AssertEqual(5); // Uses Operator as comparer
		buf.Min.Value.AssertEqual(1); // Uses Operator as comparer
	}

	[TestMethod]
	public void CircularBufferEx_StatsOverridesProperties()
	{
        // When Stats is explicitly set, it overrides property-based inference
        var buf = new CircularBufferEx<int>(3)
        {
            Operator = new IntOperator(),
#pragma warning disable CS0618 // Type or member is obsolete
			MaxComparer = Comparer<int>.Default,
            MinComparer = Comparer<int>.Default,
#pragma warning restore CS0618

            Stats = CircularBufferStats.Sum // Only Sum, ignore Max/Min
        };

        buf.PushBack(1);
		buf.PushBack(5);
		buf.PushBack(3);

		buf.Sum.AssertEqual(9);
		buf.Max.HasValue.AssertFalse(); // Not calculated due to Stats
		buf.Min.HasValue.AssertFalse(); // Not calculated due to Stats
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

		// Explicitly set Stats - now setters should not change it
		buf.Stats = CircularBufferStats.Max;
		buf.Stats.AssertEqual(CircularBufferStats.Max);

#pragma warning disable CS0618
		buf.MinComparer = Comparer<int>.Default; // Should NOT add Min to Stats
		buf.Stats.AssertEqual(CircularBufferStats.Max); // Still Max only
#pragma warning restore CS0618

		buf.Operator = null; // Should NOT remove Sum from Stats
		buf.Stats.AssertEqual(CircularBufferStats.Max); // Still Max only
	}
#endif

	#endregion
}
