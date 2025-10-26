namespace Ecng.Tests.Collections;

[TestClass]
public class CollectionHelperTests
{
	[TestMethod]
	public void GetHashCodeEx_Distribution_NotCollapsed()
	{
		// Arrange: many different sequences should yield many different hashes
		var sequences = Enumerable.Range(0, 200).Select(i => new[] { i });

		// Act
		var hashes = sequences.Select(s => s.GetHashCodeEx()).ToArray();
		var distinct = new HashSet<int>(hashes).Count;

		// Assert: with a proper hash we expect substantially more distinct values
		distinct.AssertGreater(50);
	}

	[TestMethod]
	public void GetHashCodeEx_ConsidersAllPositions()
	{
		// Arrange: two arrays differing only at index31 must produce different hashes
		var a = Enumerable.Range(0, 40).ToArray();
		var b = a.ToArray();
		b[31] = a[31] + 1; // index31 is currently masked out by (31 ^ index) ==0

		// Act
		var ha = a.GetHashCodeEx();
		var hb = b.GetHashCodeEx();

		// Assert
		ha.AssertNotEqual(hb);
	}
}