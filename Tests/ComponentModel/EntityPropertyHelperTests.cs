namespace Ecng.Tests.ComponentModel;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Ecng.ComponentModel;

[TestClass]
public class EntityPropertyHelperTests : BaseTestClass
{
	#region Test classes

	private class SimpleClass
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	private class NestedClass
	{
		public int Value { get; set; }

		[Display(Name = "Inner Object")]
		public SimpleClass Inner { get; set; }
	}

	private class DeepNestedClass
	{
		public NestedClass Level1 { get; set; }
	}

	private class ClassWithCollection
	{
		public List<int> Numbers { get; set; }
		public List<SimpleClass> Items { get; set; }
		public Dictionary<string, int> Dict { get; set; }
		public Dictionary<int, SimpleClass> TypedDict { get; set; }
	}

	private class NullableClass
	{
		public int? NullableInt { get; set; }
		public DateTime? NullableDate { get; set; }
	}

	#endregion

	#region GetEntityProperties tests

	[TestMethod]
	public void GetEntityProperties_SimpleClass_ReturnsProperties()
	{
		var props = typeof(SimpleClass).GetEntityProperties().ToList();

		props.Count.AssertEqual(2);
		props.Any(p => p.Name == "Id").AssertTrue();
		props.Any(p => p.Name == "Name").AssertTrue();
	}

	[TestMethod]
	public void GetEntityProperties_NestedClass_ReturnsNestedProperties()
	{
		var props = typeof(NestedClass).GetEntityProperties().ToList();

		props.Any(p => p.Name == "Value").AssertTrue();
		props.Any(p => p.Name == "Inner").AssertTrue();

		var innerProp = props.First(p => p.Name == "Inner");
		innerProp.Properties.AssertNotNull();
	}

	[TestMethod]
	public void GetEntityProperties_WithFilter_FiltersProperties()
	{
		var props = typeof(SimpleClass).GetEntityProperties(p => p.Name == "Id").ToList();

		props.Count.AssertEqual(1);
		props[0].Name.AssertEqual("Id");
	}

	#endregion

	#region GetPropType tests

	[TestMethod]
	public void GetPropType_SimpleProperty_ReturnsType()
	{
		var type = typeof(SimpleClass).GetPropType("Id");
		type.AssertEqual(typeof(int));
	}

	[TestMethod]
	public void GetPropType_StringProperty_ReturnsType()
	{
		var type = typeof(SimpleClass).GetPropType("Name");
		type.AssertEqual(typeof(string));
	}

	[TestMethod]
	public void GetPropType_NestedProperty_ReturnsType()
	{
		var type = typeof(NestedClass).GetPropType("Inner.Id");
		type.AssertEqual(typeof(int));
	}

	[TestMethod]
	public void GetPropType_DeepNestedProperty_ReturnsType()
	{
		var type = typeof(DeepNestedClass).GetPropType("Level1.Inner.Name");
		type.AssertEqual(typeof(string));
	}

	[TestMethod]
	public void GetPropType_NonExistentProperty_ReturnsNull()
	{
		var type = typeof(SimpleClass).GetPropType("NonExistent");
		type.AssertNull();
	}

	[TestMethod]
	public void GetPropType_ListIndexer_ReturnsItemType()
	{
		var type = typeof(ClassWithCollection).GetPropType("Numbers[0]");
		type.AssertEqual(typeof(int));
	}

	[TestMethod]
	public void GetPropType_ListOfObjects_ReturnsItemType()
	{
		var type = typeof(ClassWithCollection).GetPropType("Items[0]");
		type.AssertEqual(typeof(SimpleClass));
	}

	[TestMethod]
	public void GetPropType_ListItemProperty_ReturnsType()
	{
		var type = typeof(ClassWithCollection).GetPropType("Items[0].Id");
		type.AssertEqual(typeof(int));
	}

	[TestMethod]
	public void GetPropType_Dictionary_ReturnsObject()
	{
		var type = typeof(ClassWithCollection).GetPropType("Dict[key]");
		type.AssertEqual(typeof(object));
	}

	[TestMethod]
	public void GetPropType_TypedDictionary_ReturnsObject()
	{
		// Note: Dictionary<K,V> implements IDictionary, so returns object (not the value type)
		var type = typeof(ClassWithCollection).GetPropType("TypedDict[1]");
		type.AssertEqual(typeof(object));
	}

	[TestMethod]
	public void GetPropType_NullableInt_ReturnsUnderlyingType()
	{
		var type = typeof(NullableClass).GetPropType("NullableInt");
		type.AssertEqual(typeof(int));
	}

	[TestMethod]
	public void GetPropType_NullType_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() => ((Type)null).GetPropType("Id"));
	}

	[TestMethod]
	public void GetPropType_NullName_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() => typeof(SimpleClass).GetPropType(null));
	}

	[TestMethod]
	public void GetPropType_WithVirtualProp_UsesCallback()
	{
		var type = typeof(SimpleClass).GetPropType("Virtual", (t, n) => n == "Virtual" ? typeof(decimal) : null);
		type.AssertEqual(typeof(decimal));
	}

	#endregion

	#region GetPropValue tests

	[TestMethod]
	public void GetPropValue_SimpleProperty_ReturnsValue()
	{
		var obj = new SimpleClass { Id = 42, Name = "Test" };

		obj.GetPropValue("Id").AssertEqual(42);
		obj.GetPropValue("Name").AssertEqual("Test");
	}

	[TestMethod]
	public void GetPropValue_NestedProperty_ReturnsValue()
	{
		var obj = new NestedClass { Inner = new SimpleClass { Id = 123 } };

		obj.GetPropValue("Inner.Id").AssertEqual(123);
	}

	[TestMethod]
	public void GetPropValue_NullNested_ReturnsNull()
	{
		var obj = new NestedClass { Inner = null };

		obj.GetPropValue("Inner.Id").AssertNull();
	}

	[TestMethod]
	public void GetPropValue_NonExistent_ReturnsNull()
	{
		var obj = new SimpleClass();

		obj.GetPropValue("NonExistent").AssertNull();
	}

	[TestMethod]
	public void GetPropValue_ListIndex_ReturnsItem()
	{
		var obj = new ClassWithCollection { Numbers = [10, 20, 30] };

		obj.GetPropValue("Numbers[0]").AssertEqual(10);
		obj.GetPropValue("Numbers[1]").AssertEqual(20);
		obj.GetPropValue("Numbers[2]").AssertEqual(30);
	}

	[TestMethod]
	public void GetPropValue_ListOutOfRange_ReturnsNull()
	{
		var obj = new ClassWithCollection { Numbers = [10] };

		obj.GetPropValue("Numbers[5]").AssertNull();
	}

	[TestMethod]
	public void GetPropValue_ListNegativeIndex_ReturnsNull()
	{
		var obj = new ClassWithCollection { Numbers = [10] };

		obj.GetPropValue("Numbers[-1]").AssertNull();
	}

	[TestMethod]
	public void GetPropValue_ListItemProperty_ReturnsValue()
	{
		var obj = new ClassWithCollection
		{
			Items = [new SimpleClass { Id = 1 }, new SimpleClass { Id = 2 }]
		};

		obj.GetPropValue("Items[0].Id").AssertEqual(1);
		obj.GetPropValue("Items[1].Id").AssertEqual(2);
	}

	[TestMethod]
	public void GetPropValue_Dictionary_ReturnsValue()
	{
		var obj = new ClassWithCollection
		{
			Dict = new Dictionary<string, int> { ["key1"] = 100, ["key2"] = 200 }
		};

		obj.GetPropValue("Dict[key1]").AssertEqual(100);
		obj.GetPropValue("Dict[key2]").AssertEqual(200);
	}

	[TestMethod]
	public void GetPropValue_DictionaryMissingKey_ReturnsNull()
	{
		var obj = new ClassWithCollection
		{
			Dict = new Dictionary<string, int> { ["key1"] = 100 }
		};

		obj.GetPropValue("Dict[missing]").AssertNull();
	}

	[TestMethod]
	public void GetPropValue_TypedDictionary_ReturnsValue()
	{
		var obj = new ClassWithCollection
		{
			TypedDict = new Dictionary<int, SimpleClass>
			{
				[1] = new SimpleClass { Name = "First" },
				[2] = new SimpleClass { Name = "Second" }
			}
		};

		(obj.GetPropValue("TypedDict[1]") as SimpleClass).Name.AssertEqual("First");
	}

	[TestMethod]
	public void GetPropValue_WithVars_UsesVariable()
	{
		var obj = new ClassWithCollection { Numbers = [10, 20, 30] };
		var vars = new Dictionary<string, object> { ["idx"] = 2 };

		obj.GetPropValue("Numbers[idx]", vars: vars).AssertEqual(30);
	}

	[TestMethod]
	public void GetPropValue_WithVirtualProp_UsesCallback()
	{
		var obj = new SimpleClass { Id = 42 };

		var result = obj.GetPropValue("Virtual", (o, n) => n == "Virtual" ? 999 : null);
		result.AssertEqual(999);
	}

	#endregion

	#region GetVars tests

	[TestMethod]
	public void GetVars_NoIndexer_ReturnsEmpty()
	{
		var vars = typeof(SimpleClass).GetVars("Id").ToList();
		vars.Count.AssertEqual(0);
	}

	[TestMethod]
	public void GetVars_NumericIndex_ReturnsEmpty()
	{
		var vars = typeof(ClassWithCollection).GetVars("Numbers[0]").ToList();
		vars.Count.AssertEqual(0);
	}

	[TestMethod]
	public void GetVars_VariableIndex_ReturnsVariable()
	{
		var vars = typeof(ClassWithCollection).GetVars("Numbers[idx]").ToList();
		vars.Count.AssertEqual(1);
		vars[0].AssertEqual("idx");
	}

	[TestMethod]
	public void GetVars_MultipleVariables_ReturnsAll()
	{
		var vars = typeof(ClassWithCollection).GetVars("Items[i].Id").ToList();
		vars.Count.AssertEqual(1);
		vars[0].AssertEqual("i");
	}

	[TestMethod]
	public void GetVars_NullType_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() => ((Type)null).GetVars("Id").ToList());
	}

	[TestMethod]
	public void GetVars_NullName_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() => typeof(SimpleClass).GetVars(null).ToList());
	}

	#endregion
}
