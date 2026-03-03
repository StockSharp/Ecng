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

	#region IAttributesEntity tests

	private class TestAttributesEntity : IAttributesEntity
	{
		public IList<Attribute> Attributes { get; } = [];
	}

	[TestMethod]
	public void SetDisplay_AddsDisplayAttribute()
	{
		var entity = new TestAttributesEntity();

		entity.SetDisplay("Group", "Name", "Desc", 10);

		entity.Attributes.Count.AssertEqual(1);
		var attr = entity.Attributes[0] as DisplayAttribute;
		attr.AssertNotNull();
		attr.GroupName.AssertEqual("Group");
		attr.Name.AssertEqual("Name");
		attr.Description.AssertEqual("Desc");
		attr.Order.AssertEqual(10);
	}

	[TestMethod]
	public void SetDisplay_ReplacesExistingDisplayAttribute()
	{
		var entity = new TestAttributesEntity();

		entity.SetDisplay("Group1", "Name1", "Desc1", 1);
		entity.SetDisplay("Group2", "Name2", "Desc2", 2);

		entity.Attributes.Count.AssertEqual(1);
		var attr = entity.Attributes[0] as DisplayAttribute;
		attr.Name.AssertEqual("Name2");
	}

	[TestMethod]
	public void SetReadOnly_AddsReadOnlyAttribute()
	{
		var entity = new TestAttributesEntity();

		entity.SetReadOnly();

		entity.Attributes.Count.AssertEqual(1);
		entity.IsReadOnly().AssertTrue();
	}

	[TestMethod]
	public void SetReadOnly_False_RemovesAttribute()
	{
		var entity = new TestAttributesEntity();

		entity.SetReadOnly(true);
		entity.IsReadOnly().AssertTrue();

		entity.SetReadOnly(false);
		entity.IsReadOnly().AssertFalse();
		entity.Attributes.Count.AssertEqual(0);
	}

	[TestMethod]
	public void SetBasic_AddsBasicSettingAttribute()
	{
		var entity = new TestAttributesEntity();

		entity.SetBasic();

		entity.Attributes.Count.AssertEqual(1);
		entity.IsBasic().AssertTrue();
	}

	[TestMethod]
	public void SetBasic_False_RemovesAttribute()
	{
		var entity = new TestAttributesEntity();

		entity.SetBasic(true);
		entity.IsBasic().AssertTrue();

		entity.SetBasic(false);
		entity.IsBasic().AssertFalse();
	}

	[TestMethod]
	public void SetNonBrowsable_AddsBrowsableAttribute()
	{
		var entity = new TestAttributesEntity();

		entity.SetNonBrowsable();

		entity.IsBrowsable().AssertFalse();
	}

	[TestMethod]
	public void SetNonBrowsable_False_RemovesAttribute()
	{
		var entity = new TestAttributesEntity();

		entity.SetNonBrowsable(true);
		entity.IsBrowsable().AssertFalse();

		entity.SetNonBrowsable(false);
		entity.IsBrowsable().AssertTrue();
	}

	[TestMethod]
	public void SetExpandable_AddsTypeConverterAttribute()
	{
		var entity = new TestAttributesEntity();

		entity.SetExpandable(true);

		entity.Attributes.Count.AssertEqual(1);
		(entity.Attributes[0] is TypeConverterAttribute).AssertTrue();
	}

	[TestMethod]
	public void GetDisplayName_FromEntity()
	{
		var entity = new TestAttributesEntity();
		entity.SetDisplay("Group", "EntityName", "Desc", 0);

		entity.GetDisplayName().AssertEqual("EntityName");
	}

	[TestMethod]
	public void GetDescription_FromEntity()
	{
		var entity = new TestAttributesEntity();
		entity.SetDisplay("Group", "Name", "EntityDesc", 0);

		entity.GetDescription().AssertEqual("EntityDesc");
	}

	[TestMethod]
	public void GetGroupName_FromEntity()
	{
		var entity = new TestAttributesEntity();
		entity.SetDisplay("EntityGroup", "Name", "Desc", 0);

		entity.GetGroupName().AssertEqual("EntityGroup");
	}

	[TestMethod]
	public void GetDisplay_ReturnsDisplayAttribute()
	{
		var entity = new TestAttributesEntity();
		entity.SetDisplay("Group", "Name", "Desc", 5);

		var display = entity.GetDisplay();
		display.AssertNotNull();
		display.Order.AssertEqual(5);
	}

	[TestMethod]
	public void GetDisplay_NoAttribute_ReturnsNull()
	{
		var entity = new TestAttributesEntity();

		entity.GetDisplay().AssertNull();
	}

	#endregion

	#region Validators tests

	[TestMethod]
	public void SetRequired_AddsRequiredAttribute()
	{
		var entity = new TestAttributesEntity();

		entity.SetRequired();

		entity.Attributes.Count.AssertEqual(1);
		(entity.Attributes[0] is RequiredAttribute).AssertTrue();
	}

	[TestMethod]
	public void IsValid_WithRequiredValidator_ValidValue()
	{
		var entity = new TestAttributesEntity();
		entity.SetRequired();

		entity.IsValid("some value").AssertTrue();
	}

	[TestMethod]
	public void IsValid_WithRequiredValidator_NullValue()
	{
		var entity = new TestAttributesEntity();
		entity.SetRequired();

		entity.IsValid(null).AssertFalse();
	}

	[TestMethod]
	public void IsValid_WithRequiredValidator_EmptyString()
	{
		var entity = new TestAttributesEntity();
		entity.SetRequired();

		entity.IsValid(string.Empty).AssertFalse();
	}

	[TestMethod]
	public void IsValid_NoValidators_ReturnsTrue()
	{
		var entity = new TestAttributesEntity();

		entity.IsValid(null).AssertTrue();
		entity.IsValid("any").AssertTrue();
	}

	[TestMethod]
	public void IsValid_WithRangeValidator()
	{
		var entity = new TestAttributesEntity();
		entity.ModifyAttributes(true, () => new RangeAttribute(1, 100));

		entity.IsValid(50).AssertTrue();
		entity.IsValid(1).AssertTrue();
		entity.IsValid(100).AssertTrue();
		entity.IsValid(0).AssertFalse();
		entity.IsValid(101).AssertFalse();
	}

	[TestMethod]
	public void IsValid_MultipleValidators_AllMustPass()
	{
		var entity = new TestAttributesEntity();
		entity.SetRequired();
		entity.ModifyAttributes(true, () => new StringLengthAttribute(10));

		entity.IsValid("short").AssertTrue();
		entity.IsValid("this is too long").AssertFalse();
		entity.IsValid(null).AssertFalse();
		entity.IsValid(string.Empty).AssertFalse();
	}

	#endregion

	#region ModifyAttributes tests

	[TestMethod]
	public void ModifyAttributes_Add_AddsAttribute()
	{
		var entity = new TestAttributesEntity();

		entity.ModifyAttributes(true, () => new ReadOnlyAttribute(true));

		entity.Attributes.Count.AssertEqual(1);
	}

	[TestMethod]
	public void ModifyAttributes_Remove_RemovesAttribute()
	{
		var entity = new TestAttributesEntity();
		entity.ModifyAttributes(true, () => new ReadOnlyAttribute(true));

		entity.ModifyAttributes(false, () => new ReadOnlyAttribute(true));

		entity.Attributes.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ModifyAttributes_Add_ReplacesExisting()
	{
		var entity = new TestAttributesEntity();
		entity.ModifyAttributes(true, () => new RangeAttribute(1, 10));
		entity.ModifyAttributes(true, () => new RangeAttribute(5, 50));

		entity.Attributes.Count.AssertEqual(1);
		var range = entity.Attributes[0] as RangeAttribute;
		range.Minimum.AssertEqual(5);
		range.Maximum.AssertEqual(50);
	}

	[TestMethod]
	public void ModifyAttributes_ByInstance()
	{
		var entity = new TestAttributesEntity();
		var attr = new DescriptionAttribute("Test");

		entity.ModifyAttributes(true, attr);

		entity.Attributes.Count.AssertEqual(1);
		(entity.Attributes[0] as DescriptionAttribute).Description.AssertEqual("Test");
	}

	[TestMethod]
	public void ModifyAttributes_NullEntity_Throws()
	{
		TestAttributesEntity entity = null;

		ThrowsExactly<ArgumentNullException>(() => entity.ModifyAttributes(true, () => new ReadOnlyAttribute(true)));
	}

	[TestMethod]
	public void ModifyAttributes_NullAttrType_Throws()
	{
		var entity = new TestAttributesEntity();

		ThrowsExactly<ArgumentNullException>(() => entity.ModifyAttributes(true, null, () => new ReadOnlyAttribute(true)));
	}

	[TestMethod]
	public void ModifyAttributes_NullCreate_Throws()
	{
		var entity = new TestAttributesEntity();

		ThrowsExactly<ArgumentNullException>(() => entity.ModifyAttributes(true, typeof(ReadOnlyAttribute), null));
	}

	#endregion

	#region SetEditor tests

	[TestMethod]
	public void SetEditor_AddsEditorAttribute()
	{
		var entity = new TestAttributesEntity();
		var editorAttr = new EditorAttribute(typeof(string), typeof(object));

		entity.SetEditor(editorAttr);

		entity.Attributes.Count.AssertEqual(1);
		entity.Attributes[0].AssertSame(editorAttr);
	}

	[TestMethod]
	public void SetEditor_NullEditor_Throws()
	{
		var entity = new TestAttributesEntity();

		ThrowsExactly<ArgumentNullException>(() => entity.SetEditor(null));
	}

	#endregion

	#region Multiple attributes tests

	[TestMethod]
	public void MultipleAttributes_IndependentOperations()
	{
		var entity = new TestAttributesEntity();

		entity.SetDisplay("Group", "Name", "Desc", 1);
		entity.SetReadOnly();
		entity.SetBasic();
		entity.SetRequired();

		entity.Attributes.Count.AssertEqual(4);
		entity.IsReadOnly().AssertTrue();
		entity.IsBasic().AssertTrue();
		entity.GetDisplayName().AssertEqual("Name");
		entity.IsValid(null).AssertFalse();
	}

	#endregion
}
