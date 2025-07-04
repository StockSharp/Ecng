namespace Ecng.Tests.Collections;

[TestClass]
public class CircularBufferExTests
{
	[TestMethod]
	public void StatsComputation()
	{
		var buf = new CircularBufferEx<decimal>(3)
		{
			Operator = new DecimalOperator(),
			MaxComparer = Comparer<decimal>.Default,
			MinComparer = Comparer<decimal>.Default
		};
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
		var buf = new CircularBufferEx<int>(2)
		{
			Operator = new IntOperator(),
			MaxComparer = Comparer<int>.Default,
			MinComparer = Comparer<int>.Default
		};
		buf.PushBack(1);
		buf.PushBack(2);
		buf.Capacity = 3;
		buf.PushBack(3);
		buf.Sum.AssertEqual(3);
		buf.Max.Value.AssertEqual(3);
		buf.Min.Value.AssertEqual(3);
	}

	[TestMethod]
	public void CollectionCompatibility()
	{
		var buf = new CircularBufferEx<decimal>(4)
		{
			Operator = new DecimalOperator(),
			MaxComparer = Comparer<decimal>.Default,
			MinComparer = Comparer<decimal>.Default
		};
		var icol = (ICollection<decimal>)buf;
		var ilist = (IList<decimal>)buf;

		icol.Add(10);
		icol.Add(20);
		icol.Add(30);
		buf.Sum.AssertEqual(60);
		buf.Max.Value.AssertEqual(30);
		buf.Min.Value.AssertEqual(10);
		buf.SumNoFirst.AssertEqual(50);

		// Remove
		icol.Remove(20).AssertTrue();
		buf.Sum.AssertEqual(40);
		buf.Max.Value.AssertEqual(30);
		buf.Min.Value.AssertEqual(10);
		buf.SumNoFirst.AssertEqual(30);

		// Insert
		ilist.Insert(1, 25);
		buf.Sum.AssertEqual(65);
		buf.Max.Value.AssertEqual(30);
		buf.Min.Value.AssertEqual(10);
		buf.SumNoFirst.AssertEqual(55);

		// RemoveAt
		ilist.RemoveAt(0);
		buf.Sum.AssertEqual(55);
		buf.Max.Value.AssertEqual(30);
		buf.Min.Value.AssertEqual(25);
		buf.SumNoFirst.AssertEqual(30);

		// Set by index
		ilist[0] = 100;
		buf.Sum.AssertEqual(130);
		buf.Max.Value.AssertEqual(100);
		buf.Min.Value.AssertEqual(30);
		buf.SumNoFirst.AssertEqual(30);
	}
}