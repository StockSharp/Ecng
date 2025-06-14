namespace Ecng.Tests.Common;

using System.ComponentModel;

#pragma warning disable CS0612 // Type or member is obsolete

[TestClass]
public class AttributeHelperTests
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
	public void GetAttributeCaching()
	{
		AttributeHelper.ClearCache();
		AttributeHelper.CacheEnabled = true;
		var type = typeof(AttrClass);
		var a1 = type.GetAttribute<ObsoleteAttribute>();
		var a2 = type.GetAttribute<ObsoleteAttribute>();
		a1.AssertNotNull();
		a1.AssertSame(a2);
		AttributeHelper.CacheEnabled = false;
		a1 = type.GetAttribute<ObsoleteAttribute>();
		a2 = type.GetAttribute<ObsoleteAttribute>();
		a1.AssertNotSame(a2);
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
		Assert.ThrowsExactly<ArgumentNullException>(() => AttributeHelper.GetAttribute<ObsoleteAttribute>(null));
		Assert.ThrowsExactly<ArgumentNullException>(() => AttributeHelper.GetAttributes<ObsoleteAttribute>(null).ToArray());
		Assert.ThrowsExactly<ArgumentNullException>(() => AttributeHelper.GetAttributes(null).ToArray());
	}
}