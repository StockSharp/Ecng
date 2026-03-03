namespace Ecng.Tests.Data;

using Ecng.Data;
using Ecng.Data.Sql;

[TestClass]
public class SqlDialectTests : BaseTestClass
{
	private static ISqlDialect GetDialect(string dialectName) => dialectName switch
	{
		"SqlServer" => SqlServerDialect.Instance,
		"SQLite" => SQLiteDialect.Instance,
		_ => throw new ArgumentException($"Unknown dialect: {dialectName}")
	};

	private static string Quote(string dialectName, string identifier) => dialectName switch
	{
		"SqlServer" => $"[{identifier}]",
		"SQLite" => $"\"{identifier}\"",
		_ => identifier
	};

	#region CreateSelect Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void CreateSelect_Basic(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var q = Quote(dialectName, "Users");

		var sql = Query.CreateSelect("Users", null, null, null, null).Render(dialect);

		sql.AssertEqual($"SELECT * FROM {q}");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void CreateSelect_WithWhere(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qTable = Quote(dialectName, "Users");
		var qCol = Quote(dialectName, "Status");

		var sql = Query.CreateSelect("Users", $"{qCol} = @p0", null, null, null).Render(dialect);

		sql.AssertEqual($"SELECT * FROM {qTable} WHERE {qCol} = @p0");
	}

	[TestMethod]
	[DataRow("SqlServer", "OFFSET 10 ROWS", "FETCH NEXT 20 ROWS ONLY")]
	[DataRow("SQLite", "OFFSET 10", "LIMIT 20")]
	public void CreateSelect_WithPagination(string dialectName, string expectedOffset, string expectedLimit)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Id");

		var sql = Query.CreateSelect("Users", null, $"{qCol} ASC", 10, 20).Render(dialect);

		sql.Contains(expectedOffset).AssertTrue($"Should have offset pattern, got: {sql}");
		sql.Contains(expectedLimit).AssertTrue($"Should have limit pattern, got: {sql}");
	}

	#endregion

	#region CreateInsert Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void CreateInsert(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var columns = new[] { "Name", "Value" };
		var qTable = Quote(dialectName, "TestTable");
		var qName = Quote(dialectName, "Name");
		var qValue = Quote(dialectName, "Value");

		var sql = Query.CreateInsert("TestTable", columns).Render(dialect);

		sql.AssertEqual($"INSERT INTO {qTable} ({qName}, {qValue}) VALUES (@Name, @Value)");
	}

	#endregion

	#region CreateUpdate Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void CreateUpdate(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var columns = new[] { "Name", "Value" };
		var qTable = Quote(dialectName, "TestTable");
		var qName = Quote(dialectName, "Name");
		var qValue = Quote(dialectName, "Value");
		var qId = Quote(dialectName, "Id");

		var sql = Query.CreateUpdate("TestTable", columns, $"{qId} = @p0").Render(dialect);

		sql.AssertEqual($"UPDATE {qTable} SET {qName} = @Name, {qValue} = @Value WHERE {qId} = @p0");
	}

	#endregion

	#region CreateDelete Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void CreateDelete(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qTable = Quote(dialectName, "TestTable");
		var qId = Quote(dialectName, "Id");

		var sql = Query.CreateDelete("TestTable", $"{qId} = @p0").Render(dialect);

		sql.AssertEqual($"DELETE FROM {qTable} WHERE {qId} = @p0");
	}

	#endregion

	#region CreateUpsert Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void CreateUpsert_SingleKey(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var columns = new[] { "Id", "Name", "Value" };
		var keys = new[] { "Id" };

		var sql = Query.CreateUpsert("TestTable", columns, keys).Render(dialect);

		// Should not contain bare "&" which would indicate JoinAnd bug
		sql.Contains("&").AssertFalse($"UPSERT should not contain '&' character, got: {sql}");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void CreateUpsert_MultipleKeys_ShouldUseAND(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var columns = new[] { "Id", "Name", "Value" };
		var keys = new[] { "Id", "Name" };

		var sql = Query.CreateUpsert("TestTable", columns, keys).Render(dialect);

		// Should not contain bare "&"
		sql.Contains("&").AssertFalse($"Multiple keys UPSERT should not contain '&', got: {sql}");

		// Should use proper AND or comma separation for multiple keys
		if (sql.Contains("WHERE") || sql.Contains("ON"))
		{
			var hasMultiKeyLogic = sql.Contains(" AND ") || sql.Contains(",");
			hasMultiKeyLogic.AssertTrue($"Multiple key UPSERT should have proper key joining, got: {sql}");
		}
	}

	/// <summary>
	/// Verifies that CreateUpsert handles the case when all columns are keys (no non-key columns).
	/// </summary>
	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void CreateUpsert_AllColumnsAreKeys_ShouldProduceValidSql(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		// All columns are keys - no columns to update
		var columns = new[] { "Id", "Name" };
		var keys = new[] { "Id", "Name" };

		var sql = Query.CreateUpsert("TestTable", columns, keys).Render(dialect);

		// SQL should not end with "SET " or "SET" followed by whitespace/nothing
		// Valid options: "DO NOTHING" for SQLite, skip UPDATE clause for SQL Server
		var trimmed = sql.Trim();
		trimmed.EndsWith("SET").AssertFalse($"UPSERT should not end with empty SET clause, got: {sql}");
		trimmed.EndsWith("SET ").AssertFalse($"UPSERT should not end with empty SET clause, got: {sql}");

		// Check that SQL doesn't contain "UPDATE SET" followed by nothing meaningful
		var setIndex = sql.LastIndexOf("SET", StringComparison.OrdinalIgnoreCase);
		if (setIndex >= 0)
		{
			var afterSet = sql.Substring(setIndex + 3).Trim();
			// After "SET" there should be actual assignments, not empty or just whitespace/newline
			(afterSet.Length > 0 && !afterSet.StartsWith("WHEN", StringComparison.OrdinalIgnoreCase))
				.AssertTrue($"After SET should be column assignments, got: '{afterSet}' in SQL: {sql}");
		}
	}

	#endregion

	#region CreateBuildCondition Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void CreateBuildCondition_Equal(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Col");

		var sql = Query.CreateBuildCondition("Col", ComparisonOperator.Equal, "p0").Render(dialect);

		sql.AssertEqual($"{qCol} = @p0");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void CreateBuildCondition_NotEqual(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Col");

		var sql = Query.CreateBuildCondition("Col", ComparisonOperator.NotEqual, "p0").Render(dialect);

		sql.AssertEqual($"{qCol} <> @p0");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void CreateBuildCondition_Greater(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Col");

		var sql = Query.CreateBuildCondition("Col", ComparisonOperator.Greater, "p0").Render(dialect);

		sql.AssertEqual($"{qCol} > @p0");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void CreateBuildCondition_Less(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Col");

		var sql = Query.CreateBuildCondition("Col", ComparisonOperator.Less, "p0").Render(dialect);

		sql.AssertEqual($"{qCol} < @p0");
	}

	/// <summary>
	/// Verifies that CreateBuildCondition with null value uses IS NULL syntax.
	/// </summary>
	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void CreateBuildCondition_NullEqual_ShouldUseIsNull(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Col");

		var sql = Query.CreateBuildCondition("Col", ComparisonOperator.Equal, null).Render(dialect);

		sql.AssertEqual($"{qCol} IS NULL");
	}

	/// <summary>
	/// Verifies that CreateBuildCondition with null and NotEqual uses IS NOT NULL syntax.
	/// </summary>
	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void CreateBuildCondition_NullNotEqual_ShouldUseIsNotNull(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Col");

		var sql = Query.CreateBuildCondition("Col", ComparisonOperator.NotEqual, null).Render(dialect);

		sql.AssertEqual($"{qCol} IS NOT NULL");
	}

	#endregion

	#region CreateBuildInCondition Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void CreateBuildInCondition(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Status");

		var sql = Query.CreateBuildInCondition("Status", ["p0", "p1", "p2"]).Render(dialect);

		sql.AssertEqual($"{qCol} IN (@p0, @p1, @p2)");
	}

	#endregion

	#region JoinConditions Tests

	/// <summary>
	/// Verifies that SQL conditions can be joined with " AND " separator.
	/// </summary>
	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void JoinConditions_MultipleConditions(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol1 = Quote(dialectName, "Col1");
		var qCol2 = Quote(dialectName, "Col2");
		var qCol3 = Quote(dialectName, "Col3");

		var cond1 = Query.CreateBuildCondition("Col1", ComparisonOperator.Equal, "p0").Render(dialect);
		var cond2 = Query.CreateBuildCondition("Col2", ComparisonOperator.Equal, "p1").Render(dialect);
		var cond3 = Query.CreateBuildCondition("Col3", ComparisonOperator.Greater, "p2").Render(dialect);

		// Use .Join(" AND ") for SQL logical AND - not .JoinAnd() which is for URL query strings
		var joined = new[] { cond1, cond2, cond3 }.Join(" AND ");

		joined.AssertEqual($"{qCol1} = @p0 AND {qCol2} = @p1 AND {qCol3} > @p2");
	}

	#endregion

	#region QuoteIdentifier Tests

	[TestMethod]
	[DataRow("SqlServer", "[Column]")]
	[DataRow("SQLite", "\"Column\"")]
	public void QuoteIdentifier(string dialectName, string expected)
	{
		var dialect = GetDialect(dialectName);

		dialect.QuoteIdentifier("Column").AssertEqual(expected);
	}

	#endregion

	#region ParameterPrefix Tests

	[TestMethod]
	[DataRow("SqlServer", "@")]
	[DataRow("SQLite", "@")]
	public void ParameterPrefix(string dialectName, string expected)
	{
		var dialect = GetDialect(dialectName);

		dialect.ParameterPrefix.AssertEqual(expected);
	}

	#endregion
}
