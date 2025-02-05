namespace Ecng.Tests.Common
{
	using System.Dynamic;

	[TestClass]
	public class TypeTests
	{
		[TestMethod]
		public void HasProperty()
		{
			var obj = new { MyProp = 123 };
			var propName = nameof(obj.MyProp);

			obj.HasProperty(propName).AssertTrue();

			var obj2 = new { };
			obj2.HasProperty(propName).AssertFalse();

			dynamic obj3 = new ExpandoObject();
			obj3.MyProp = 123;
			((object)obj3).HasProperty(propName).AssertTrue();

			dynamic obj4 = new ExpandoObject();
			TypeHelper.HasProperty((object)obj4, propName).AssertFalse();
		}
	}
}