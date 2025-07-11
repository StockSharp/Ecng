﻿namespace Ecng.Tests.Collections;

[TestClass]
public class LinqTests
{
	[TestMethod]
	public void RemoveWhere2()
	{
		var list = new List<int>();
		var list2 = new List<int>();

		void test(int[] arr, Func<int, bool> filter)
		{
			list.Clear();
			list2.Clear();

			list.AddRange(arr);
			list2.AddRange(arr);

			list.RemoveWhere2(filter).AssertEqual(list2.RemoveAll(i => filter(i)));
			list.SequenceEqual(list2).AssertTrue();
		}

		test([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], i => i is 4 or 5 or 6);
		test([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], i => i > 7);
		test([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], i => i < 5);
		test([1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9], i => i < 5);
		test([5, 5, 5, 5, 5], i => i < 5);
		test([5, 5, 5, 5, 5], i => i == 5);
		test([], i => i < 5);
	}
}
