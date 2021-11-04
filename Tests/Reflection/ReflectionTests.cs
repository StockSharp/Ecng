namespace Ecng.Tests.Reflection
{
	using System.Collections.Generic;

	using Ecng.Reflection;
	using Ecng.UnitTesting;
	
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ReflectionTests
	{
		[TestMethod]
		public void ItemType()
		{
			var arr = new[] { 1, 2, 3 };
			var list = new List<int> { 1, 2, 3 };
			var enu = (IEnumerable<int>)arr;
			arr.GetType().GetItemType().AssertSame(typeof(int));
			list.GetType().GetItemType().AssertSame(typeof(int));
			enu.GetType().GetItemType().AssertSame(typeof(int));
		}
	}
}