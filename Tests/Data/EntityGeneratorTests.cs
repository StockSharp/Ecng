#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using Ecng.Serialization;

[TestClass]
public class EntityGeneratorTests : BaseTestClass
{
	[TestMethod]
	public void EntityAttribute_Name()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestOrderEntity));

		schema.TableName.AssertEqual("Ecng_Orders");
	}

	[TestMethod]
	public void EntityAttribute_NoCache()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestOrderEntity));

		schema.NoCache.AssertTrue();
	}

	[TestMethod]
	public void EntityAttribute_NoCacheFalse()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestProductEntity));

		schema.NoCache.AssertFalse();
	}

	[TestMethod]
	public void NoEntityAttribute_FallsBackToClassName()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestPlainEntity));

		schema.TableName.AssertEqual("GenTestPlainEntity");
	}

	[TestMethod]
	public void GeneratedSchema_HasCorrectColumns()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestOrderEntity));

		schema.Identity.AssertNotNull();
		schema.Identity.Name.AssertEqual("Id");

		var colNames = schema.Columns.Select(c => c.Name).ToArray();
		colNames.AssertContains("Symbol");
		colNames.AssertContains("Price");
	}

	#region ColumnAttribute in generated schema

	[TestMethod]
	public void Generated_ColumnAttr_MaxLength()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestColumnAttrEntity));
		var col = schema.Columns.First(c => c.Name == "Name");

		col.MaxLength.AssertEqual(128);
	}

	[TestMethod]
	public void Generated_ColumnAttr_IsNullable()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestColumnAttrEntity));
		var col = schema.Columns.First(c => c.Name == "Description");

		col.IsNullable.AssertTrue();
	}

	[TestMethod]
	public void Generated_ColumnAttr_Both()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestColumnAttrEntity));
		var col = schema.Columns.First(c => c.Name == "Tag");

		col.IsNullable.AssertTrue();
		col.MaxLength.AssertEqual(64);
	}

	[TestMethod]
	public void Generated_NoAttr_String_NotNullable()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestColumnAttrEntity));
		var col = schema.Columns.First(c => c.Name == "Plain");

		col.IsNullable.AssertFalse();
		col.MaxLength.AssertEqual(0);
	}

	[TestMethod]
	public void Generated_NullableValueType_IsNullable()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestColumnAttrEntity));
		var col = schema.Columns.First(c => c.Name == "NullableInt");

		col.IsNullable.AssertTrue();
	}

	[TestMethod]
	public void Generated_ValueType_NotNullable()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestColumnAttrEntity));
		var col = schema.Columns.First(c => c.Name == "RequiredInt");

		col.IsNullable.AssertFalse();
	}

	#endregion
}

#endif
