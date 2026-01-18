namespace Ecng.Tests.Data;

using Ecng.Data;

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

	#region GenerateSelect Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void GenerateSelect_Basic(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var q = Quote(dialectName, "Users");

		var sql = dialect.GenerateSelect("Users", null, null, null, null);

		sql.AssertEqual($"SELECT * FROM {q}");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void GenerateSelect_WithWhere(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qTable = Quote(dialectName, "Users");
		var qCol = Quote(dialectName, "Status");

		var sql = dialect.GenerateSelect("Users", $"{qCol} = @p0", null, null, null);

		sql.AssertEqual($"SELECT * FROM {qTable} WHERE {qCol} = @p0");
	}

	[TestMethod]
	[DataRow("SqlServer", "OFFSET 10 ROWS", "FETCH NEXT 20 ROWS ONLY")]
	[DataRow("SQLite", "OFFSET 10", "LIMIT 20")]
	public void GenerateSelect_WithPagination(string dialectName, string expectedOffset, string expectedLimit)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Id");

		var sql = dialect.GenerateSelect("Users", null, $"{qCol} ASC", 10, 20);

		sql.Contains(expectedOffset).AssertTrue($"Should have offset pattern, got: {sql}");
		sql.Contains(expectedLimit).AssertTrue($"Should have limit pattern, got: {sql}");
	}

	#endregion

	#region GenerateInsert Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void GenerateInsert(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var columns = new[] { "Name", "Value" };
		var qTable = Quote(dialectName, "TestTable");
		var qName = Quote(dialectName, "Name");
		var qValue = Quote(dialectName, "Value");

		var sql = dialect.GenerateInsert("TestTable", columns);

		sql.AssertEqual($"INSERT INTO {qTable} ({qName}, {qValue}) VALUES (@Name, @Value)");
	}

	#endregion

	#region GenerateUpdate Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void GenerateUpdate(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var columns = new[] { "Name", "Value" };
		var qTable = Quote(dialectName, "TestTable");
		var qName = Quote(dialectName, "Name");
		var qValue = Quote(dialectName, "Value");
		var qId = Quote(dialectName, "Id");

		var sql = dialect.GenerateUpdate("TestTable", columns, $"{qId} = @p0");

		sql.AssertEqual($"UPDATE {qTable} SET {qName} = @Name, {qValue} = @Value WHERE {qId} = @p0");
	}

	#endregion

	#region GenerateDelete Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void GenerateDelete(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qTable = Quote(dialectName, "TestTable");
		var qId = Quote(dialectName, "Id");

		var sql = dialect.GenerateDelete("TestTable", $"{qId} = @p0");

		sql.AssertEqual($"DELETE FROM {qTable} WHERE {qId} = @p0");
	}

	#endregion

	#region GenerateUpsert Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void GenerateUpsert_SingleKey(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var columns = new[] { "Id", "Name", "Value" };
		var keys = new[] { "Id" };

		var sql = dialect.GenerateUpsert("TestTable", columns, keys);

		// Should not contain bare "&" which would indicate JoinAnd bug
		sql.Contains("&").AssertFalse($"UPSERT should not contain '&' character, got: {sql}");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void GenerateUpsert_MultipleKeys_ShouldUseAND(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var columns = new[] { "Id", "Name", "Value" };
		var keys = new[] { "Id", "Name" };

		var sql = dialect.GenerateUpsert("TestTable", columns, keys);

		// Should not contain bare "&"
		sql.Contains("&").AssertFalse($"Multiple keys UPSERT should not contain '&', got: {sql}");

		// Should use proper AND or comma separation for multiple keys
		if (sql.Contains("WHERE") || sql.Contains("ON"))
		{
			var hasMultiKeyLogic = sql.Contains(" AND ") || sql.Contains(",");
			hasMultiKeyLogic.AssertTrue($"Multiple key UPSERT should have proper key joining, got: {sql}");
		}
	}

	#endregion

	#region BuildCondition Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void BuildCondition_Equal(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Col");

		// paramName should be without prefix - BuildCondition adds the prefix
		var sql = dialect.BuildCondition("Col", ComparisonOperator.Equal, "p0");

		sql.AssertEqual($"{qCol} = @p0");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void BuildCondition_NotEqual(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Col");

		var sql = dialect.BuildCondition("Col", ComparisonOperator.NotEqual, "p0");

		sql.AssertEqual($"{qCol} <> @p0");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void BuildCondition_Greater(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Col");

		var sql = dialect.BuildCondition("Col", ComparisonOperator.Greater, "p0");

		sql.AssertEqual($"{qCol} > @p0");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void BuildCondition_Less(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Col");

		var sql = dialect.BuildCondition("Col", ComparisonOperator.Less, "p0");

		sql.AssertEqual($"{qCol} < @p0");
	}

	/// <summary>
	/// Verifies that BuildCondition with null value uses IS NULL syntax.
	/// </summary>
	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void BuildCondition_NullEqual_ShouldUseIsNull(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Col");

		var sql = dialect.BuildCondition("Col", ComparisonOperator.Equal, null);

		sql.AssertEqual($"{qCol} IS NULL");
	}

	/// <summary>
	/// Verifies that BuildCondition with null and NotEqual uses IS NOT NULL syntax.
	/// </summary>
	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void BuildCondition_NullNotEqual_ShouldUseIsNotNull(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Col");

		var sql = dialect.BuildCondition("Col", ComparisonOperator.NotEqual, null);

		sql.AssertEqual($"{qCol} IS NOT NULL");
	}

	#endregion

	#region BuildInCondition Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	public void BuildInCondition(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Status");

		// paramNames without prefix - BuildInCondition adds the prefix
		var sql = dialect.BuildInCondition("Status", ["p0", "p1", "p2"]);

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

		// paramName without prefix - BuildCondition adds the prefix
		var cond1 = dialect.BuildCondition("Col1", ComparisonOperator.Equal, "p0");
		var cond2 = dialect.BuildCondition("Col2", ComparisonOperator.Equal, "p1");
		var cond3 = dialect.BuildCondition("Col3", ComparisonOperator.Greater, "p2");

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
