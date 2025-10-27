namespace Ecng.Tests.Common;

[TestClass]
public class ArrayHelperTests
{
	[TestMethod]
	public void ClearArray()
	{
		var arr = new int[] { 1, 2, 3 };
		arr.Clear();
		arr.All(x => x == 0).AssertTrue();
	}

	[TestMethod]
	public void ClearArrayRange()
	{
		var arr = new int[] { 1, 2, 3 };
		arr.Clear(1, 2);
		arr[0].AssertEqual(1);
		arr[1].AssertEqual(0);
		arr[2].AssertEqual(0);
	}

	[TestMethod]
	public void CreateArrayType()
	{
		var arr = typeof(double).CreateArray(3);
		arr.Length.AssertEqual(3);
		arr.GetType().GetElementType().AssertEqual(typeof(double));
	}

	[TestMethod]
	public void IndexOfFound()
	{
		var arr = new[] { "a", "b", "c" };
		arr.IndexOf("b").AssertEqual(1);
	}

	[TestMethod]
	public void IndexOfNotFound()
	{
		var arr = new[] { "a", "b", "c" };
		arr.IndexOf("z").AssertEqual(-1);
	}

	[TestMethod]
	public void CloneArray()
	{
		var arr = new[] { 1, 2, 3 };
		var clone = arr.Clone();
		((int[])clone).AssertEqual(arr);
		ReferenceEquals(arr, clone).AssertFalse();
	}

	[TestMethod]
	public void ReverseArray()
	{
		var arr = new[] { 1, 2, 3 };
		var rev = arr.Reverse();
		rev.AssertEqual([3, 2, 1]);
	}

	[TestMethod]
	public void ConcatArrays()
	{
		var a = new[] { 1, 2 };
		var b = new[] { 3, 4 };
		var c = a.Concat(b);
		c.AssertEqual([1, 2, 3, 4]);
	}

	[TestMethod]
	public void ClearThrowsOnNull()
	{
		int[] arr = null;
		Assert.ThrowsExactly<ArgumentNullException>(arr.Clear);
	}

	[TestMethod]
	public void ConcatThrowsOnNull()
	{
		int[] a = null, b = null;
		Assert.ThrowsExactly<ArgumentNullException>(() => a.Concat([]));
		Assert.ThrowsExactly<ArgumentNullException>(() => Array.Empty<int>().Concat(b));
	}
}
