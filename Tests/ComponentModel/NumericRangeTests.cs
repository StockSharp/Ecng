#if NET7_0_OR_GREATER || NET8_0_OR_GREATER || NET9_0_OR_GREATER || NET10_0_OR_GREATER
namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class NumericRangeTests : BaseTestClass
{
	[TestMethod]
	public void Basic_Int()
	{
		var r = new NumericRange<int>(1, 4);
		var rg = (IRange<int>)r;
		rg.HasMinValue.AssertTrue();
		rg.HasMaxValue.AssertTrue();
		rg.Min.AssertEqual(1);
		rg.Max.AssertEqual(4);
		r.Length.AssertEqual(3);
		r.Contains(2).AssertTrue();
		r.Contains(5).AssertFalse();
		var ix = r.Intersect(new NumericRange<int>(3, 10));
		ix.AssertNotNull();
		ix.Value.Min.AssertEqual(3);
		ix.Value.Max.AssertEqual(4);
	}

	[TestMethod]
	public void Basic_Decimal()
	{
		var r = new NumericRange<decimal>(1.5m, 2.0m);
		r.Length.AssertEqual(0.5m);
		r.Contains(1.75m).AssertTrue();
		r.Contains(2.1m).AssertFalse();
	}
}
#endif
