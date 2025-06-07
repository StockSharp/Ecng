namespace Ecng.Tests.Common;

[TestClass]
public class AllocationArrayTest
{
	[TestMethod]
	public void Test()
	{
		var array = new AllocationArray<int>
		{
			10
		};
		array.Count.AssertEqual(1);

		array.Count = 10;
		array.Count.AssertEqual(10);

		array.Count = 0;
		array.Count.AssertEqual(0);

		array.Add([1, 2, 3, 4], 2, 2);
		array.Count.AssertEqual(2);

		array.RemoveAt(0);
		array.Count.AssertEqual(1);

		array.RemoveAt(0);
		array.Count.AssertEqual(0);

		array.Add([1, 2, 3, 4], 0, 4);
		array.Count.AssertEqual(4);

		array.RemoveRange(1, 2);
		array.Count.AssertEqual(2);
		(array[0] + array[1]).AssertEqual(5);

		array.RemoveRange(0, 2);
		array.Count.AssertEqual(0);
	}

	[TestMethod]
	public void AddAndIndex()
	{
		var arr = new AllocationArray<int>();
		for (int i = 0; i < 1000; i++)
			arr.Add(i);
		arr.Count.AssertEqual(1000);
		for (int i = 0; i < 1000; i++)
			arr[i].AssertEqual(i);
	}

	[TestMethod]
	public void AddRangeAndRemove()
	{
		var arr = new AllocationArray<int>
		{
			{ Enumerable.Range(0, 1000).ToArray(), 0, 1000 }
		};
		arr.Count.AssertEqual(1000);
		arr.RemoveAt(0);
		arr.Count.AssertEqual(999);
		arr.RemoveRange(0, 998);
		arr.Count.AssertEqual(1);
		arr[0].AssertEqual(999);
	}

	[TestMethod]
	public void IndexerSetGrow()
	{
		var arr = new AllocationArray<int>
		{
			[10] = 42
		};
		arr.Count.AssertEqual(11);
		arr[10].AssertEqual(42);
	}

	[TestMethod]
	public void ResetAndCapacity()
	{
		var arr = new AllocationArray<int>(10);
		for (int i = 0; i < 10; i++) arr.Add(i);
		arr.Reset(20);
		arr.Count.AssertEqual(0);
		(arr.Buffer.Length >= 20).AssertTrue();
	}

	[TestMethod]
	public void MaxCountThrows()
	{
		var arr = new AllocationArray<int>
		{
			MaxCount = 5
		};
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => arr.Count = 10);
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => arr[10] = 1);
	}

	[TestMethod]
	public void RemoveRangeThrows()
	{
		var arr = new AllocationArray<int>
		{
			1
		};
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => arr.RemoveRange(-1, 1));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => arr.RemoveRange(0, 0));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => arr.RemoveRange(0, 2));
	}

	[TestMethod]
	public void Enumerator()
	{
		var arr = new AllocationArray<int>();
		for (int i = 0; i < 100; i++) arr.Add(i);
		int sum = 0;
		foreach (var x in arr) sum += x;
		sum.AssertEqual(Enumerable.Range(0, 100).Sum());
	}

	[TestMethod]
	public void BufferReflectsChanges()
	{
		var arr = new AllocationArray<int>
		{
			1
		};
		arr.Buffer[0] = 42;
		arr[0].AssertEqual(42);
	}

	[TestMethod]
	public void DifferentTypes()
	{
		var arrStr = new AllocationArray<string>
		{
			"abc"
		};
		arrStr[0].AssertEqual("abc");
		var arrObj = new AllocationArray<object>
		{
			new()
		};
		arrObj.Count.AssertEqual(1);
	}
}