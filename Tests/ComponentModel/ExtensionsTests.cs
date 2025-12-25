namespace Ecng.Tests.ComponentModel;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Ecng.ComponentModel;

[TestClass]
public class ExtensionsTests : BaseTestClass
{
	#region Test classes

	[DisplayName("Test Class Name")]
	[Description("Test Class Description")]
	[Category("Test Category")]
	private class TestClassWithAttributes { }

	[Display(Name = "Display Class", Description = "Display Description", GroupName = "Display Group")]
	private class TestClassWithDisplayAttribute { }

	private class TestClassNoAttributes { }

	private class TestClassWithProperties
	{
		[DisplayName("Property Name")]
		[Description("Property Description")]
		public string PropertyWithDisplayName { get; set; }

		[Display(Name = "Display Prop", Description = "Display Prop Desc", GroupName = "Prop Group")]
		public int PropertyWithDisplay { get; set; }

		public bool PropertyNoAttributes { get; set; }
	}

	private enum TestEnumWithDescription
	{
		[Display(Name = "Value One", Description = "First value")]
		Value1,

		[Display(Name = "Value Two", Description = "Second value")]
		Value2
	}

	#endregion

	#region GetDisplayName tests

	[TestMethod]
	public void GetDisplayName_TypeWithDisplayNameAttribute_ReturnsDisplayName()
	{
		typeof(TestClassWithAttributes).GetDisplayName().AssertEqual("Test Class Name");
	}

	[TestMethod]
	public void GetDisplayName_TypeWithDisplayAttribute_ReturnsName()
	{
		typeof(TestClassWithDisplayAttribute).GetDisplayName().AssertEqual("Display Class");
	}

	[TestMethod]
	public void GetDisplayName_TypeNoAttributes_ReturnsTypeName()
	{
		typeof(TestClassNoAttributes).GetDisplayName().AssertEqual("TestClassNoAttributes");
	}

	[TestMethod]
	public void GetDisplayName_TypeNoAttributes_WithDefault_ReturnsDefault()
	{
		typeof(TestClassNoAttributes).GetDisplayName("Default").AssertEqual("Default");
	}

	[TestMethod]
	public void GetDisplayName_Property_WithDisplayNameAttribute()
	{
		var prop = typeof(TestClassWithProperties).GetProperty(nameof(TestClassWithProperties.PropertyWithDisplayName));
		prop.GetDisplayName().AssertEqual("Property Name");
	}

	[TestMethod]
	public void GetDisplayName_Property_WithDisplayAttribute()
	{
		var prop = typeof(TestClassWithProperties).GetProperty(nameof(TestClassWithProperties.PropertyWithDisplay));
		prop.GetDisplayName().AssertEqual("Display Prop");
	}

	#endregion

	#region GetDescription tests

	[TestMethod]
	public void GetDescription_TypeWithDescriptionAttribute_ReturnsDescription()
	{
		typeof(TestClassWithAttributes).GetDescription().AssertEqual("Test Class Description");
	}

	[TestMethod]
	public void GetDescription_TypeWithDisplayAttribute_ReturnsDescription()
	{
		typeof(TestClassWithDisplayAttribute).GetDescription().AssertEqual("Display Description");
	}

	[TestMethod]
	public void GetDescription_TypeNoAttributes_ReturnsTypeName()
	{
		typeof(TestClassNoAttributes).GetDescription().AssertEqual("TestClassNoAttributes");
	}

	[TestMethod]
	public void GetDescription_TypeNoAttributes_WithDefault_ReturnsDefault()
	{
		typeof(TestClassNoAttributes).GetDescription("Default Desc").AssertEqual("Default Desc");
	}

	#endregion

	#region GetCategory tests

	[TestMethod]
	public void GetCategory_TypeWithCategoryAttribute_ReturnsCategory()
	{
		typeof(TestClassWithAttributes).GetCategory().AssertEqual("Test Category");
	}

	[TestMethod]
	public void GetCategory_TypeWithDisplayAttribute_ReturnsGroupName()
	{
		typeof(TestClassWithDisplayAttribute).GetCategory().AssertEqual("Display Group");
	}

	[TestMethod]
	public void GetCategory_TypeNoAttributes_ReturnsTypeName()
	{
		typeof(TestClassNoAttributes).GetCategory().AssertEqual("TestClassNoAttributes");
	}

	[TestMethod]
	public void GetCategory_TypeNoAttributes_WithDefault_ReturnsDefault()
	{
		typeof(TestClassNoAttributes).GetCategory("Default Cat").AssertEqual("Default Cat");
	}

	#endregion

	#region Enum field tests

	[TestMethod]
	public void GetFieldDisplayName_EnumWithDisplayAttribute()
	{
		TestEnumWithDescription.Value1.GetFieldDisplayName().AssertEqual("Value One");
	}

	[TestMethod]
	public void GetFieldDescription_EnumWithDisplayAttribute()
	{
		TestEnumWithDescription.Value1.GetFieldDescription().AssertEqual("First value");
	}

	[TestMethod]
	public void GetDisplayName_Object_Enum()
	{
		object value = TestEnumWithDescription.Value2;
		value.GetDisplayName().AssertEqual("Value Two");
	}

	[TestMethod]
	public void GetDisplayName_Object_Null_Throws()
	{
		object value = null;
		ThrowsExactly<ArgumentNullException>(() => value.GetDisplayName());
	}

	#endregion

	#region Guid tests

	[TestMethod]
	public void ToN_Guid_ReturnsNFormat()
	{
		var guid = new Guid("12345678-1234-1234-1234-123456789abc");
		guid.ToN().AssertEqual("12345678123412341234123456789abc");
	}

	#endregion
}
