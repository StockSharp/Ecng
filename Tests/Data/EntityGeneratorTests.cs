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

		schema.TableName.AssertEqual("Orders");
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
}

#endif
