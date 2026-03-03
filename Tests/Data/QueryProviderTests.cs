#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.Serialization;
using Ecng.UnitTesting;

[TestClass]
public class QueryProviderTests : BaseTestClass
{
	private static readonly ISqlDialect _dialect = SqlServerDialect.Instance;
	private static readonly QueryProvider _provider = new();

	private static Schema CreateTestSchema(
		string tableName,
		SchemaColumn identity,
		IReadOnlyList<SchemaColumn> columns)
	{
		return new()
		{
			TableName = tableName,
			EntityType = typeof(object),
			Identity = identity,
			Columns = columns,
		};
	}

	private static string Norm(string sql) => sql.Replace("\r\n", "\n");

	#region Count

	[TestMethod]
	public void Count_GeneratesSelectCountStar()
	{
		var schema = CreateTestSchema("Orders",
			identity: new() { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			columns: [new() { Name = "Name", ClrType = typeof(string) }]);

		var query = _provider.Create(schema, SqlCommandTypes.Count, schema.AllColumns, schema.AllColumns);
		var sql = Norm(query.Render(_dialect));

		sql.Contains("select").AssertTrue($"Expected 'select', got: {sql}");
		sql.Contains("count").AssertTrue($"Expected 'count', got: {sql}");
		sql.Contains("(*)").AssertTrue($"Expected '(*)', got: {sql}");
		sql.Contains("[Orders]").AssertTrue($"Expected '[Orders]', got: {sql}");
		sql.Contains("from").AssertTrue($"Expected 'from', got: {sql}");
	}

	#endregion

	#region Create (INSERT)

	[TestMethod]
	public void Create_GeneratesInsertInto_ExcludesReadOnlyColumns()
	{
		var identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true };
		var columns = new SchemaColumn[]
		{
			new() { Name = "Name", ClrType = typeof(string) },
			new() { Name = "Price", ClrType = typeof(decimal) },
		};

		var schema = CreateTestSchema("Products", identity, columns);
		var allCols = schema.AllColumns;

		var query = _provider.Create(schema, SqlCommandTypes.Create, [identity], allCols);
		var sql = Norm(query.Render(_dialect));

		// INSERT INTO with non-read-only columns
		sql.Contains("insert").AssertTrue($"Expected 'insert', got: {sql}");
		sql.Contains("[Products]").AssertTrue($"Expected '[Products]', got: {sql}");
		sql.Contains("[Name]").AssertTrue($"Expected '[Name]', got: {sql}");
		sql.Contains("[Price]").AssertTrue($"Expected '[Price]', got: {sql}");

		// Parameters should use @ prefix
		sql.Contains("@Name").AssertTrue($"Expected '@Name', got: {sql}");
		sql.Contains("@Price").AssertTrue($"Expected '@Price', got: {sql}");

		// Read-only column should not appear in INSERT column list
		// (Id is read-only identity -- it appears in the subsequent SELECT but not in INSERT columns)
		sql.Contains("values").AssertTrue($"Expected 'values', got: {sql}");
	}

	[TestMethod]
	public void Create_WithReadOnlyIdentity_GeneratesIdentitySelect()
	{
		var identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true };
		var columns = new SchemaColumn[]
		{
			new() { Name = "Title", ClrType = typeof(string) },
		};

		var schema = CreateTestSchema("Articles", identity, columns);

		var query = _provider.Create(schema, SqlCommandTypes.Create, [identity], schema.AllColumns);
		var sql = Norm(query.Render(_dialect));

		// Should be a BatchQuery with identity select
		sql.Contains("scope_identity()").AssertTrue($"Expected 'scope_identity()', got: {sql}");
	}

	[TestMethod]
	public void Create_ParameterNamesMatchColumnNames()
	{
		var identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true };
		var columns = new SchemaColumn[]
		{
			new() { Name = "FirstName", ClrType = typeof(string) },
			new() { Name = "LastName", ClrType = typeof(string) },
			new() { Name = "EmailAddress", ClrType = typeof(string) },
		};

		var schema = CreateTestSchema("Contacts", identity, columns);

		var query = _provider.Create(schema, SqlCommandTypes.Create, [identity], schema.AllColumns);
		var sql = Norm(query.Render(_dialect));

		sql.Contains("@FirstName").AssertTrue($"Expected '@FirstName' param, got: {sql}");
		sql.Contains("@LastName").AssertTrue($"Expected '@LastName' param, got: {sql}");
		sql.Contains("@EmailAddress").AssertTrue($"Expected '@EmailAddress' param, got: {sql}");
	}

	#endregion

	#region ReadBy

	[TestMethod]
	public void ReadBy_GeneratesSelectWithWhereOnKeyColumns()
	{
		var identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true };
		var columns = new SchemaColumn[]
		{
			new() { Name = "Name", ClrType = typeof(string) },
			new() { Name = "Value", ClrType = typeof(int) },
		};

		var schema = CreateTestSchema("Settings", identity, columns);

		var query = _provider.Create(schema, SqlCommandTypes.ReadBy, [identity], schema.AllColumns);
		var sql = Norm(query.Render(_dialect));

		sql.Contains("select").AssertTrue($"Expected 'select', got: {sql}");
		sql.Contains("[e].*").AssertTrue($"Expected '[e].*', got: {sql}");
		sql.Contains("from").AssertTrue($"Expected 'from', got: {sql}");
		sql.Contains("[Settings]").AssertTrue($"Expected '[Settings]', got: {sql}");
		sql.Contains("where").AssertTrue($"Expected 'where', got: {sql}");
		sql.Contains("e.[Id] = @Id").AssertTrue($"Expected 'e.[Id] = @Id', got: {sql}");
	}

	[TestMethod]
	public void ReadBy_CompositeKey_GeneratesAndConditions()
	{
		var col1 = new SchemaColumn { Name = "TenantId", ClrType = typeof(int) };
		var col2 = new SchemaColumn { Name = "UserId", ClrType = typeof(int) };
		var columns = new SchemaColumn[]
		{
			new() { Name = "Role", ClrType = typeof(string) },
		};

		var schema = CreateTestSchema("TenantUsers", null, [col1, col2, .. columns]);

		var query = _provider.Create(schema, SqlCommandTypes.ReadBy, [col1, col2], schema.AllColumns);
		var sql = Norm(query.Render(_dialect));

		sql.Contains("e.[TenantId] = @TenantId").AssertTrue($"Expected TenantId equality, got: {sql}");
		sql.Contains("e.[UserId] = @UserId").AssertTrue($"Expected UserId equality, got: {sql}");
		sql.Contains(" and ").AssertTrue($"Expected ' and ' join, got: {sql}");
	}

	#endregion

	#region ReadRange

	[TestMethod]
	public void ReadRange_GeneratesSelectWithInClause()
	{
		var identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true };
		var columns = new SchemaColumn[]
		{
			new() { Name = "Name", ClrType = typeof(string) },
		};

		var schema = CreateTestSchema("Items", identity, columns);

		// Value columns represent the parameter placeholders in IN clause
		var rangeParams = new SchemaColumn[]
		{
			new() { Name = "p0", ClrType = typeof(long) },
			new() { Name = "p1", ClrType = typeof(long) },
			new() { Name = "p2", ClrType = typeof(long) },
		};

		var query = _provider.Create(schema, SqlCommandTypes.ReadRange, [identity], rangeParams);
		var sql = Norm(query.Render(_dialect));

		sql.Contains("select").AssertTrue($"Expected 'select', got: {sql}");
		sql.Contains("[e].*").AssertTrue($"Expected '[e].*', got: {sql}");
		sql.Contains("[Items]").AssertTrue($"Expected '[Items]', got: {sql}");
		sql.Contains("where").AssertTrue($"Expected 'where', got: {sql}");
		sql.Contains(" in ").AssertTrue($"Expected ' in ', got: {sql}");
		sql.Contains("[e].[Id]").AssertTrue($"Expected '[e].[Id]', got: {sql}");
		sql.Contains("@p0").AssertTrue($"Expected '@p0', got: {sql}");
		sql.Contains("@p1").AssertTrue($"Expected '@p1', got: {sql}");
		sql.Contains("@p2").AssertTrue($"Expected '@p2', got: {sql}");
	}

	#endregion

	#region ReadAll

	[TestMethod]
	public void ReadAll_GeneratesSelectAllFromTable()
	{
		var identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true };
		var columns = new SchemaColumn[]
		{
			new() { Name = "Name", ClrType = typeof(string) },
			new() { Name = "Description", ClrType = typeof(string) },
		};

		var schema = CreateTestSchema("Categories", identity, columns);

		var query = _provider.Create(schema, SqlCommandTypes.ReadAll, [identity], schema.AllColumns);
		var sql = Norm(query.Render(_dialect));

		sql.Contains("select").AssertTrue($"Expected 'select', got: {sql}");
		sql.Contains("[e].*").AssertTrue($"Expected '[e].*', got: {sql}");
		sql.Contains("from").AssertTrue($"Expected 'from', got: {sql}");
		sql.Contains("[Categories]").AssertTrue($"Expected '[Categories]', got: {sql}");

		// ReadAll should NOT have a WHERE clause
		sql.Contains("where").AssertFalse($"ReadAll should not have 'where', got: {sql}");
	}

	#endregion

	#region UpdateBy

	[TestMethod]
	public void UpdateBy_GeneratesUpdateSetWhere()
	{
		var identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true };
		var columns = new SchemaColumn[]
		{
			new() { Name = "Name", ClrType = typeof(string) },
			new() { Name = "Price", ClrType = typeof(decimal) },
		};

		var schema = CreateTestSchema("Products", identity, columns);

		var query = _provider.Create(schema, SqlCommandTypes.UpdateBy, [identity], schema.AllColumns);
		var sql = Norm(query.Render(_dialect));

		sql.Contains("update").AssertTrue($"Expected 'update', got: {sql}");
		sql.Contains("set").AssertTrue($"Expected 'set', got: {sql}");
		sql.Contains("from").AssertTrue($"Expected 'from', got: {sql}");
		sql.Contains("[Products]").AssertTrue($"Expected '[Products]', got: {sql}");
		sql.Contains("where").AssertTrue($"Expected 'where', got: {sql}");
		sql.Contains("e.[Id] = @Id").AssertTrue($"Expected 'e.[Id] = @Id', got: {sql}");

		// SET clause should contain column assignments
		sql.Contains("[Name] = @Name").AssertTrue($"Expected '[Name] = @Name', got: {sql}");
		sql.Contains("[Price] = @Price").AssertTrue($"Expected '[Price] = @Price', got: {sql}");
	}

	[TestMethod]
	public void UpdateBy_LongColumnNames_AreNotTruncated()
	{
		var identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true };
		var columns = new SchemaColumn[]
		{
			new() { Name = "VeryLongColumnNameForTestingPurposes", ClrType = typeof(string) },
			new() { Name = "AnotherExtremelyLongColumnNameThatShouldNotBeTruncated", ClrType = typeof(string) },
			new() { Name = "ShortDescription", ClrType = typeof(string) },
		};

		var schema = CreateTestSchema("LongNameTable", identity, columns);

		var query = _provider.Create(schema, SqlCommandTypes.UpdateBy, [identity], schema.AllColumns);
		var sql = Norm(query.Render(_dialect));

		// CRITICAL: Verify all column names in SET are complete (not truncated)
		sql.Contains("[VeryLongColumnNameForTestingPurposes] = @VeryLongColumnNameForTestingPurposes")
			.AssertTrue($"Long column name was truncated in SET clause, got: {sql}");
		sql.Contains("[AnotherExtremelyLongColumnNameThatShouldNotBeTruncated] = @AnotherExtremelyLongColumnNameThatShouldNotBeTruncated")
			.AssertTrue($"Long column name was truncated in SET clause, got: {sql}");
		sql.Contains("[ShortDescription] = @ShortDescription")
			.AssertTrue($"Expected '[ShortDescription] = @ShortDescription', got: {sql}");
	}

	[TestMethod]
	public void UpdateBy_ExcludesReadOnlyColumnsFromSet()
	{
		var identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true };
		var columns = new SchemaColumn[]
		{
			new() { Name = "Name", ClrType = typeof(string) },
			new() { Name = "ComputedField", ClrType = typeof(string), IsReadOnly = true },
		};

		var schema = CreateTestSchema("Mixed", identity, columns);

		var query = _provider.Create(schema, SqlCommandTypes.UpdateBy, [identity], schema.AllColumns);
		var sql = Norm(query.Render(_dialect));

		// Name should be in SET
		sql.Contains("[Name] = @Name").AssertTrue($"Expected '[Name] = @Name' in SET, got: {sql}");

		// ComputedField is read-only and should appear in the SELECT part of the batch, not in SET
		// The SET clause should not assign ComputedField
		var setIdx = sql.IndexOf("set", StringComparison.OrdinalIgnoreCase);
		var whereIdx = sql.IndexOf("where", StringComparison.OrdinalIgnoreCase);
		if (setIdx >= 0 && whereIdx > setIdx)
		{
			var setClause = sql.Substring(setIdx, whereIdx - setIdx);
			setClause.Contains("ComputedField").AssertFalse(
				$"Read-only column 'ComputedField' should not appear in SET clause, got SET: {setClause}");
		}
	}

	#endregion

	#region DeleteBy

	[TestMethod]
	public void DeleteBy_GeneratesDeleteWithWhereOnKeyColumns()
	{
		var identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true };
		var columns = new SchemaColumn[]
		{
			new() { Name = "Name", ClrType = typeof(string) },
		};

		var schema = CreateTestSchema("Logs", identity, columns);

		var query = _provider.Create(schema, SqlCommandTypes.DeleteBy, [identity], schema.AllColumns);
		var sql = Norm(query.Render(_dialect));

		sql.Contains("delete").AssertTrue($"Expected 'delete', got: {sql}");
		sql.Contains("from").AssertTrue($"Expected 'from', got: {sql}");
		sql.Contains("[Logs]").AssertTrue($"Expected '[Logs]', got: {sql}");
		sql.Contains("where").AssertTrue($"Expected 'where', got: {sql}");
		sql.Contains("e.[Id] = @Id").AssertTrue($"Expected 'e.[Id] = @Id', got: {sql}");
	}

	[TestMethod]
	public void DeleteBy_CompositeKey()
	{
		var col1 = new SchemaColumn { Name = "OrderId", ClrType = typeof(long) };
		var col2 = new SchemaColumn { Name = "ProductId", ClrType = typeof(long) };
		var columns = new SchemaColumn[]
		{
			new() { Name = "Quantity", ClrType = typeof(int) },
		};

		var schema = CreateTestSchema("OrderItems", null, [col1, col2, .. columns]);

		var query = _provider.Create(schema, SqlCommandTypes.DeleteBy, [col1, col2], schema.AllColumns);
		var sql = Norm(query.Render(_dialect));

		sql.Contains("delete").AssertTrue($"Expected 'delete', got: {sql}");
		sql.Contains("e.[OrderId] = @OrderId").AssertTrue($"Expected OrderId equality, got: {sql}");
		sql.Contains("e.[ProductId] = @ProductId").AssertTrue($"Expected ProductId equality, got: {sql}");
		sql.Contains(" and ").AssertTrue($"Expected ' and ' join, got: {sql}");
	}

	#endregion

	#region DeleteAll

	[TestMethod]
	public void DeleteAll_GeneratesDeleteFromTable()
	{
		var identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true };
		var columns = new SchemaColumn[]
		{
			new() { Name = "Name", ClrType = typeof(string) },
		};

		var schema = CreateTestSchema("TempData", identity, columns);

		var query = _provider.Create(schema, SqlCommandTypes.DeleteAll, [identity], schema.AllColumns);
		var sql = Norm(query.Render(_dialect));

		sql.Contains("delete").AssertTrue($"Expected 'delete', got: {sql}");
		sql.Contains("from").AssertTrue($"Expected 'from', got: {sql}");
		sql.Contains("[TempData]").AssertTrue($"Expected '[TempData]', got: {sql}");

		// DeleteAll should NOT have a WHERE clause
		sql.Contains("where").AssertFalse($"DeleteAll should not have 'where', got: {sql}");
	}

	#endregion

	#region Caching

	[TestMethod]
	public void Create_ReturnsCachedQueryForSameInputs()
	{
		var provider = new QueryProvider();
		var identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true };
		var columns = new SchemaColumn[]
		{
			new() { Name = "Name", ClrType = typeof(string) },
		};

		var schema = CreateTestSchema("CacheTest", identity, columns);

		var query1 = provider.Create(schema, SqlCommandTypes.ReadAll, [identity], schema.AllColumns);
		var query2 = provider.Create(schema, SqlCommandTypes.ReadAll, [identity], schema.AllColumns);

		query1.AssertSame(query2);
	}

	#endregion

	#region SchemaRegistry Integration

	[TestMethod]
	public void ReadBy_WithSchemaRegistryEntity_GeneratesCorrectSql()
	{
		var schema = SchemaRegistry.Get(typeof(TestItem));
		var query = _provider.Create(schema, SqlCommandTypes.ReadBy, [schema.Identity], schema.AllColumns);
		var sql = Norm(query.Render(_dialect));

		sql.Contains("select").AssertTrue($"Expected 'select', got: {sql}");
		sql.Contains("[e].*").AssertTrue($"Expected '[e].*', got: {sql}");
		sql.Contains("[TestItem]").AssertTrue($"Expected '[TestItem]', got: {sql}");
		sql.Contains("where").AssertTrue($"Expected 'where', got: {sql}");
		sql.Contains("e.[Id] = @Id").AssertTrue($"Expected 'e.[Id] = @Id', got: {sql}");
	}

	[TestMethod]
	public void Count_WithSchemaRegistryEntity_GeneratesCorrectSql()
	{
		var schema = SchemaRegistry.Get(typeof(TestItem));
		var query = _provider.Create(schema, SqlCommandTypes.Count, [schema.Identity], schema.AllColumns);
		var sql = Norm(query.Render(_dialect));

		sql.Contains("select count(*)").AssertTrue($"Expected 'select count(*)', got: {sql}");
		sql.Contains("[TestItem]").AssertTrue($"Expected '[TestItem]', got: {sql}");
	}

	#endregion
}

#endif
