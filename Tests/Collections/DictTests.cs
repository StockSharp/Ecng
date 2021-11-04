namespace Ecng.Tests.Collections
{
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class DictTests
	{
		[TestMethod]
		public void Tuples()
		{
			var dict = new Dictionary<string, int>();
			(string name, int value) t = ("123", 123);
			dict.Add(t);

			foreach (var (name, value) in dict)
			{
				name.AssertEqual(t.name);
				value.AssertEqual(t.value);
			}
		}
	}
}