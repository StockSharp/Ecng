namespace Ecng.Tests.Collections;

[TestClass]
public class CircularBufferExTests
{
	[TestMethod]
	public void StatsComputation()
	{
		var buf = new CircularBufferEx<decimal>(3)
		{
			Operator = new Ecng.Common.DecimalOperator(),
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
	}

	[TestMethod]
	public void CapacityReset()
	{
		var buf = new CircularBufferEx<int>(2)
		{
			Operator = new Ecng.Common.IntOperator(),
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
}
