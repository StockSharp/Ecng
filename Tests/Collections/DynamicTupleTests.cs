namespace Ecng.Tests.Collections;

[TestClass]
public class DynamicTupleTests : BaseTestClass
{
	[TestMethod]
	public void EqualsAndHashCode()
	{
		var a = new DynamicTuple([1, "x", 3m]);
		var b = new DynamicTuple([1, "x", 3m]);

		// Equality
		a.Equals(b).AssertTrue();
		a.AssertEqual(b);

		// Hash codes should be equal for equal tuples
		a.GetHashCode().AssertEqual(b.GetHashCode());

		// CompareTo should indicate equality (0)
		((IComparable)a).CompareTo(b).AssertEqual(0);
	}

	[TestMethod]
	public void NotEquals()
	{
		var a = new DynamicTuple([1, "x"]);
		var b = new DynamicTuple(["1", "x"]);

		// Different types/values should not be equal
		a.Equals(b).AssertFalse();
		a.AssertNotEqual(b);

		var aToB = ((IComparable)a).CompareTo(b);
		var bToA = ((IComparable)b).CompareTo(a);

		aToB.AssertNotEqual(0);
		bToA.AssertNotEqual(0);
		Math.Sign(aToB).AssertEqual(-Math.Sign(bToA));
	}

	[TestMethod]
	public void CloneAndToString()
	{
		var a = new DynamicTuple([1, "a"]);

		// Clone creates an equal but different instance
		var c = a.Clone();
		c.AssertEqual(a);
		ReferenceEquals(a, c).AssertFalse();
		a.Values.AssertNotSame(c.Values);

		// ToString representation
		c.ToString().AssertEqual("1,a");
	}
}
