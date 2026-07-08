namespace Ecng.Tests.Common;

using System.ComponentModel;

#pragma warning disable CS0612 // Type or member is obsolete

[TestClass]
public class AttributeHelperTests : BaseTestClass
{
	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	private class CustomAttribute : Attribute
	{
	}

	[Obsolete]
	[Browsable(false)]
	private class AttrClass { }

	[Custom]
	private class BaseClass { }

	private class DerivedClass : BaseClass { }

	[TestMethod]
	[DoNotParallelize]
	public void GetAttributeCaching()
	{
		var previous = AttributeHelper.CacheEnabled;
		AttributeHelper.ClearCache();

		try
		{
			AttributeHelper.CacheEnabled = true;
			var type = typeof(AttrClass);
			var a1 = type.GetAttribute<ObsoleteAttribute>();
			var a2 = type.GetAttribute<ObsoleteAttribute>();
			a1.AssertNotNull();
			a1.AssertSame(a2);
			AttributeHelper.CacheEnabled = false;
			AttributeHelper.ClearCache();
			a1 = type.GetAttribute<ObsoleteAttribute>();
			a2 = type.GetAttribute<ObsoleteAttribute>();
			a1.AssertNotSame(a2);
		}
		finally
		{
			AttributeHelper.CacheEnabled = previous;
			AttributeHelper.ClearCache();
		}
	}

	[TestMethod]
	public void AttributeQueries()
	{
		var type = typeof(AttrClass);
		type.GetAttribute<ObsoleteAttribute>().AssertNotNull();
		type.GetAttributes<Attribute>().Count().AssertEqual(2);
		type.GetAttributes().Count().AssertEqual(2);
		type.IsObsolete().AssertTrue();
		type.IsBrowsable().AssertFalse();

		typeof(DerivedClass).IsObsolete().AssertFalse();
		typeof(DerivedClass).IsBrowsable().AssertTrue();
	}

	[TestMethod]
	public void InheritSearch()
	{
		typeof(DerivedClass).GetAttribute<CustomAttribute>(false).AssertNull();
		typeof(DerivedClass).GetAttribute<CustomAttribute>(true).AssertNotNull();
	}

	[TestMethod]
	public void NullProvider()
	{
		ThrowsExactly<ArgumentNullException>(() => AttributeHelper.GetAttribute<ObsoleteAttribute>(null));
		ThrowsExactly<ArgumentNullException>(() => AttributeHelper.GetAttributes<ObsoleteAttribute>(null).ToArray());
		ThrowsExactly<ArgumentNullException>(() => AttributeHelper.GetAttributes(null).ToArray());
	}
}
