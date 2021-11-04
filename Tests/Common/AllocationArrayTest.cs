namespace Ecng.Tests.Common
{
	using Ecng.Common;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class AllocationArrayTest
	{
		[TestMethod]
		public void Test()
		{
			var array = new AllocationArray<int>();

			array.Add(10);
			array.Count.AssertEqual(1);

			array.Count = 10;
			array.Count.AssertEqual(10);

			array.Count = 0;
			array.Count.AssertEqual(0);

			array.Add(new [] { 1, 2, 3, 4 }, 2, 2);
			array.Count.AssertEqual(2);

			array.RemoveAt(0);
			array.Count.AssertEqual(1);

			array.RemoveAt(0);
			array.Count.AssertEqual(0);

			array.Add(new[] { 1, 2, 3, 4 }, 0, 4);
			array.Count.AssertEqual(4);

			array.RemoveRange(1, 2);
			array.Count.AssertEqual(2);
			(array[0] + array[1]).AssertEqual(5);

			array.RemoveRange(0, 2);
			array.Count.AssertEqual(0);
		}
	}
}