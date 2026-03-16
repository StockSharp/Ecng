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

	[TestMethod]
	public void EntitySchema_ViaInterface()
	{
		IDbPersistable entity = new GenTestOrderEntity();
		var schema = entity.Schema;

		schema.AssertNotNull();
		schema.TableName.AssertEqual("Ecng_Orders");
		schema.AssertSame(SchemaRegistry.Get(typeof(GenTestOrderEntity)));
	}

	[TestMethod]
	public void EntitySchema_ViaInterface_Plain()
	{
		IDbPersistable entity = new GenTestPlainEntity();
		var schema = entity.Schema;

		schema.AssertNotNull();
		schema.TableName.AssertEqual("GenTestPlainEntity");
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

	#region Enum properties

	[TestMethod]
	public void Generated_Enum_ClrTypeIsUnderlyingInt()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestEnumEntity));
		var col = schema.Columns.First(c => c.Name == "Status");

		col.ClrType.AssertEqual(typeof(int));
		col.IsNullable.AssertFalse();
	}

	[TestMethod]
	public void Generated_NullableEnum_ClrTypeIsUnderlyingInt()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestEnumEntity));
		var col = schema.Columns.First(c => c.Name == "NullableStatus");

		col.ClrType.AssertEqual(typeof(int));
		col.IsNullable.AssertTrue();
	}

	#endregion

	#region Identity type

	[TestMethod]
	public void Generated_Identity_IntType()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestIntIdEntity));

		schema.Identity.AssertNotNull();
		schema.Identity.Name.AssertEqual("Id");
		schema.Identity.ClrType.AssertEqual(typeof(int));
	}

	#endregion

	#region Nullable inner schema propagation

	[TestMethod]
	public void Generated_NullableInnerSchema_ColumnsAreNullable()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestNullableInnerEntity));

		var street = schema.Columns.First(c => c.Name == "AddressStreet");
		var city = schema.Columns.First(c => c.Name == "AddressCity");

		street.IsNullable.AssertTrue();
		city.IsNullable.AssertTrue();
	}

	[TestMethod]
	public void Generated_NullMid_NullBranchPropagates4Levels()
	{
		var schema = SchemaRegistry.Get(typeof(GenTestNullMidEntity));

		// L2.Top — outer is NOT NULL → NOT NULL
		schema.Columns.First(c => c.Name == "DataTop").IsNullable.AssertFalse();

		// NullBranch subtree — [Column(IsNullable = true)] at L2.NullBranch → all NULL
		schema.Columns.First(c => c.Name == "DataNullBranchMid").IsNullable.AssertTrue();
		schema.Columns.First(c => c.Name == "DataNullBranchL4Deep").IsNullable.AssertTrue();
		schema.Columns.First(c => c.Name == "DataNullBranchL4Count").IsNullable.AssertTrue();

		// SolidBranch subtree — no nullable marker → all NOT NULL
		schema.Columns.First(c => c.Name == "DataSolidBranchMid").IsNullable.AssertFalse();
		schema.Columns.First(c => c.Name == "DataSolidBranchL4Deep").IsNullable.AssertFalse();
		schema.Columns.First(c => c.Name == "DataSolidBranchL4Count").IsNullable.AssertFalse();
	}

	[TestMethod]
	public void Generated_NullCantCancel_OuterWins()
	{
		// Outer [Column(IsNullable = true)] + inner [Column(IsNullable = false)]
		// Outer wins — column must be nullable because container can be null.
		var schema = SchemaRegistry.Get(typeof(GenTestNullCantCancelEntity));

		schema.Columns.First(c => c.Name == "DataForced").IsNullable.AssertTrue();
		schema.Columns.First(c => c.Name == "DataNormal").IsNullable.AssertTrue();
	}

	[TestMethod]
	public void Generated_NullRoot_Entire4LevelTreeNullable()
	{
		// Root is nullable → everything is nullable, including SolidBranch
		var schema = SchemaRegistry.Get(typeof(GenTestNullRootEntity));

		schema.Columns.First(c => c.Name == "DataTop").IsNullable.AssertTrue();

		schema.Columns.First(c => c.Name == "DataNullBranchMid").IsNullable.AssertTrue();
		schema.Columns.First(c => c.Name == "DataNullBranchL4Deep").IsNullable.AssertTrue();
		schema.Columns.First(c => c.Name == "DataNullBranchL4Count").IsNullable.AssertTrue();

		schema.Columns.First(c => c.Name == "DataSolidBranchMid").IsNullable.AssertTrue();
		schema.Columns.First(c => c.Name == "DataSolidBranchL4Deep").IsNullable.AssertTrue();
		schema.Columns.First(c => c.Name == "DataSolidBranchL4Count").IsNullable.AssertTrue();
	}

	#endregion
}

#endif
