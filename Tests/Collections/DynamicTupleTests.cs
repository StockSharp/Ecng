namespace Ecng.Tests.Collections;

[TestClass]
public class DynamicTupleTests
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

		// CompareTo should return -1 for non-equal
		((IComparable)a).CompareTo(b).AssertEqual(-1);
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