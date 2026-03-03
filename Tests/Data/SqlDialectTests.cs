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
		"PostgreSql" => PostgreSqlDialect.Instance,
		_ => throw new ArgumentException($"Unknown dialect: {dialectName}")
	};

	private static string Quote(string dialectName, string identifier) => dialectName switch
	{
		"SqlServer" => $"[{identifier}]",
		"SQLite" or "PostgreSql" => $"\"{identifier}\"",
		_ => identifier
	};

	#region CreateSelect Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql", "OFFSET 10", "LIMIT 20")]
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
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql")]
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
	[DataRow("PostgreSql", "\"Column\"")]
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
	[DataRow("PostgreSql", "@")]
	public void ParameterPrefix(string dialectName, string expected)
	{
		var dialect = GetDialect(dialectName);

		dialect.ParameterPrefix.AssertEqual(expected);
	}

	#endregion

	#region GetSqlTypeName Tests

	[TestMethod]
	[DataRow("SqlServer", "INT")]
	[DataRow("SQLite", "INTEGER")]
	[DataRow("PostgreSql", "INTEGER")]
	public void GetSqlTypeName_Int(string dialectName, string expected)
	{
		GetDialect(dialectName).GetSqlTypeName(typeof(int)).AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "BIGINT")]
	[DataRow("SQLite", "INTEGER")]
	[DataRow("PostgreSql", "BIGINT")]
	public void GetSqlTypeName_Long(string dialectName, string expected)
	{
		GetDialect(dialectName).GetSqlTypeName(typeof(long)).AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "NVARCHAR(MAX)")]
	[DataRow("SQLite", "TEXT")]
	[DataRow("PostgreSql", "TEXT")]
	public void GetSqlTypeName_String(string dialectName, string expected)
	{
		GetDialect(dialectName).GetSqlTypeName(typeof(string)).AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "DECIMAL(18,8)")]
	[DataRow("SQLite", "REAL")]
	[DataRow("PostgreSql", "NUMERIC(18,8)")]
	public void GetSqlTypeName_Decimal(string dialectName, string expected)
	{
		GetDialect(dialectName).GetSqlTypeName(typeof(decimal)).AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "DATETIME2")]
	[DataRow("SQLite", "TEXT")]
	[DataRow("PostgreSql", "TIMESTAMP")]
	public void GetSqlTypeName_DateTime(string dialectName, string expected)
	{
		GetDialect(dialectName).GetSqlTypeName(typeof(DateTime)).AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "BIT")]
	[DataRow("SQLite", "INTEGER")]
	[DataRow("PostgreSql", "BOOLEAN")]
	public void GetSqlTypeName_Bool(string dialectName, string expected)
	{
		GetDialect(dialectName).GetSqlTypeName(typeof(bool)).AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "VARBINARY(MAX)")]
	[DataRow("SQLite", "BLOB")]
	[DataRow("PostgreSql", "BYTEA")]
	public void GetSqlTypeName_ByteArray(string dialectName, string expected)
	{
		GetDialect(dialectName).GetSqlTypeName(typeof(byte[])).AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "UNIQUEIDENTIFIER")]
	[DataRow("SQLite", "TEXT")]
	[DataRow("PostgreSql", "UUID")]
	public void GetSqlTypeName_Guid(string dialectName, string expected)
	{
		GetDialect(dialectName).GetSqlTypeName(typeof(Guid)).AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "FLOAT")]
	[DataRow("SQLite", "REAL")]
	[DataRow("PostgreSql", "DOUBLE PRECISION")]
	public void GetSqlTypeName_Double(string dialectName, string expected)
	{
		GetDialect(dialectName).GetSqlTypeName(typeof(double)).AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "REAL")]
	[DataRow("SQLite", "REAL")]
	[DataRow("PostgreSql", "REAL")]
	public void GetSqlTypeName_Float(string dialectName, string expected)
	{
		GetDialect(dialectName).GetSqlTypeName(typeof(float)).AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "SMALLINT")]
	[DataRow("SQLite", "INTEGER")]
	[DataRow("PostgreSql", "SMALLINT")]
	public void GetSqlTypeName_Short(string dialectName, string expected)
	{
		GetDialect(dialectName).GetSqlTypeName(typeof(short)).AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "TINYINT")]
	[DataRow("SQLite", "INTEGER")]
	[DataRow("PostgreSql", "SMALLINT")]
	public void GetSqlTypeName_Byte(string dialectName, string expected)
	{
		GetDialect(dialectName).GetSqlTypeName(typeof(byte)).AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "BIGINT")]
	[DataRow("SQLite", "INTEGER")]
	[DataRow("PostgreSql", "BIGINT")]
	public void GetSqlTypeName_TimeSpan(string dialectName, string expected)
	{
		GetDialect(dialectName).GetSqlTypeName(typeof(TimeSpan)).AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "DATETIMEOFFSET")]
	[DataRow("SQLite", "TEXT")]
	[DataRow("PostgreSql", "TIMESTAMPTZ")]
	public void GetSqlTypeName_DateTimeOffset(string dialectName, string expected)
	{
		GetDialect(dialectName).GetSqlTypeName(typeof(DateTimeOffset)).AssertEqual(expected);
	}

	#endregion

	#region GetIdentityColumnSuffix Tests

	[TestMethod]
	[DataRow("SqlServer", "IDENTITY(1,1) PRIMARY KEY")]
	[DataRow("SQLite", "PRIMARY KEY AUTOINCREMENT")]
	[DataRow("PostgreSql", "GENERATED ALWAYS AS IDENTITY PRIMARY KEY")]
	public void GetIdentityColumnSuffix(string dialectName, string expected)
	{
		GetDialect(dialectName).GetIdentityColumnSuffix().AssertEqual(expected);
	}

	#endregion

	#region GetIdentitySelect Tests

	[TestMethod]
	[DataRow("SqlServer", "scope_identity() as Id")]
	[DataRow("SQLite", "last_insert_rowid() as Id")]
	[DataRow("PostgreSql", "Id")]
	public void GetIdentitySelect(string dialectName, string expected)
	{
		GetDialect(dialectName).GetIdentitySelect("Id").AssertEqual(expected);
	}

	#endregion

	#region FormatSkip / FormatTake Tests

	[TestMethod]
	[DataRow("SqlServer", "offset 5 rows")]
	[DataRow("SQLite", "OFFSET 5")]
	[DataRow("PostgreSql", "OFFSET 5")]
	public void FormatSkip(string dialectName, string expected)
	{
		GetDialect(dialectName).FormatSkip("5").AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "fetch next 10 rows only")]
	[DataRow("SQLite", "LIMIT 10")]
	[DataRow("PostgreSql", "LIMIT 10")]
	public void FormatTake(string dialectName, string expected)
	{
		GetDialect(dialectName).FormatTake("10").AssertEqual(expected);
	}

	#endregion

	#region Now / UtcNow / SysNow / SysUtcNow / NewId Tests

	[TestMethod]
	[DataRow("SqlServer", "getDate()")]
	[DataRow("SQLite", "datetime('now', 'localtime')")]
	[DataRow("PostgreSql", "now()")]
	public void Now(string dialectName, string expected)
	{
		GetDialect(dialectName).Now().AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "getUtcDate()")]
	[DataRow("SQLite", "datetime('now')")]
	[DataRow("PostgreSql", "now() AT TIME ZONE 'UTC'")]
	public void UtcNow(string dialectName, string expected)
	{
		GetDialect(dialectName).UtcNow().AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "sysDateTimeOffset()")]
	[DataRow("SQLite", "datetime('now', 'localtime')")]
	[DataRow("PostgreSql", "now()")]
	public void SysNow(string dialectName, string expected)
	{
		GetDialect(dialectName).SysNow().AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "sysUtcDateTime()")]
	[DataRow("SQLite", "datetime('now')")]
	[DataRow("PostgreSql", "now() AT TIME ZONE 'UTC'")]
	public void SysUtcNow(string dialectName, string expected)
	{
		GetDialect(dialectName).SysUtcNow().AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "newId()")]
	[DataRow("SQLite", "lower(hex(randomblob(16)))")]
	[DataRow("PostgreSql", "gen_random_uuid()")]
	public void NewId(string dialectName, string expected)
	{
		GetDialect(dialectName).NewId().AssertEqual(expected);
	}

	#endregion

	#region AppendCreateTable Tests

	[TestMethod]
	[DataRow("SqlServer")]
	public void AppendCreateTable_SqlServer(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var sb = new System.Text.StringBuilder();
		dialect.AppendCreateTable(sb, "TestTable", "Id INT, Name NVARCHAR(MAX)");

		var sql = sb.ToString();
		sql.Contains("sys.tables").AssertTrue($"SqlServer should use sys.tables check, got: {sql}");
		sql.Contains("CREATE TABLE [TestTable]").AssertTrue($"Should contain CREATE TABLE with quoted name, got: {sql}");
		sql.Contains("Id INT, Name NVARCHAR(MAX)").AssertTrue($"Should contain column defs, got: {sql}");
	}

	[TestMethod]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void AppendCreateTable_IfNotExists(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var sb = new System.Text.StringBuilder();
		dialect.AppendCreateTable(sb, "TestTable", "Id INTEGER, Name TEXT");

		var sql = sb.ToString();
		sql.Contains("IF NOT EXISTS").AssertTrue($"Should use IF NOT EXISTS, got: {sql}");
		sql.Contains(Quote(dialectName, "TestTable")).AssertTrue($"Should contain quoted table name, got: {sql}");
		sql.Contains("Id INTEGER, Name TEXT").AssertTrue($"Should contain column defs, got: {sql}");
	}

	#endregion

	#region AppendDropTable Tests

	[TestMethod]
	[DataRow("SqlServer")]
	public void AppendDropTable_SqlServer(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var sb = new System.Text.StringBuilder();
		dialect.AppendDropTable(sb, "TestTable");

		var sql = sb.ToString();
		sql.Contains("IF OBJECT_ID").AssertTrue($"SqlServer should use IF OBJECT_ID check, got: {sql}");
		sql.Contains("DROP TABLE [TestTable]").AssertTrue($"Should contain DROP TABLE with quoted name, got: {sql}");
	}

	[TestMethod]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void AppendDropTable_IfExists(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var sb = new System.Text.StringBuilder();
		dialect.AppendDropTable(sb, "TestTable");

		var sql = sb.ToString();
		sql.Contains("DROP TABLE IF EXISTS").AssertTrue($"Should use DROP TABLE IF EXISTS, got: {sql}");
		sql.Contains(Quote(dialectName, "TestTable")).AssertTrue($"Should contain quoted table name, got: {sql}");
	}

	#endregion

	#region AppendPagination Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void AppendPagination_NoBothSkipAndTake_NoOutput(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var sb = new System.Text.StringBuilder();
		dialect.AppendPagination(sb, null, null, false);

		sb.ToString().AssertEqual(string.Empty);
	}

	[TestMethod]
	public void AppendPagination_SqlServer_SkipOnly_AddsOrderByNull()
	{
		var dialect = GetDialect("SqlServer");
		var sb = new System.Text.StringBuilder();
		dialect.AppendPagination(sb, 10, null, false);

		var sql = sb.ToString();
		sql.Contains("ORDER BY (SELECT NULL)").AssertTrue($"SqlServer should add ORDER BY (SELECT NULL) when no ORDER BY, got: {sql}");
		sql.Contains("OFFSET 10 ROWS").AssertTrue($"Should contain OFFSET, got: {sql}");
	}

	[TestMethod]
	public void AppendPagination_SqlServer_WithOrderBy_NoExtraOrderBy()
	{
		var dialect = GetDialect("SqlServer");
		var sb = new System.Text.StringBuilder();
		dialect.AppendPagination(sb, 10, 20, true);

		var sql = sb.ToString();
		sql.Contains("ORDER BY (SELECT NULL)").AssertFalse($"Should not add extra ORDER BY when hasOrderBy=true, got: {sql}");
		sql.Contains("OFFSET 10 ROWS").AssertTrue($"Should contain OFFSET, got: {sql}");
		sql.Contains("FETCH NEXT 20 ROWS ONLY").AssertTrue($"Should contain FETCH NEXT, got: {sql}");
	}

	[TestMethod]
	public void AppendPagination_SqlServer_TakeOnly()
	{
		var dialect = GetDialect("SqlServer");
		var sb = new System.Text.StringBuilder();
		dialect.AppendPagination(sb, null, 50, false);

		var sql = sb.ToString();
		sql.Contains("OFFSET 0 ROWS").AssertTrue($"SqlServer should default skip to 0, got: {sql}");
		sql.Contains("FETCH NEXT 50 ROWS ONLY").AssertTrue($"Should contain FETCH NEXT, got: {sql}");
	}

	[TestMethod]
	public void AppendPagination_SqlServer_BothSkipAndTake()
	{
		var dialect = GetDialect("SqlServer");
		var sb = new System.Text.StringBuilder();
		dialect.AppendPagination(sb, 5, 25, true);

		var sql = sb.ToString();
		sql.Contains("OFFSET 5 ROWS").AssertTrue($"Should contain OFFSET 5, got: {sql}");
		sql.Contains("FETCH NEXT 25 ROWS ONLY").AssertTrue($"Should contain FETCH NEXT 25, got: {sql}");
	}

	[TestMethod]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void AppendPagination_TakeOnly(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var sb = new System.Text.StringBuilder();
		dialect.AppendPagination(sb, null, 50, false);

		var sql = sb.ToString();
		sql.Contains("LIMIT 50").AssertTrue($"Should contain LIMIT 50, got: {sql}");
		sql.Contains("OFFSET").AssertFalse($"Should not contain OFFSET when skip is null, got: {sql}");
	}

	[TestMethod]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void AppendPagination_BothSkipAndTake(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var sb = new System.Text.StringBuilder();
		dialect.AppendPagination(sb, 10, 20, false);

		var sql = sb.ToString();
		sql.Contains("LIMIT 20").AssertTrue($"Should contain LIMIT 20, got: {sql}");
		sql.Contains("OFFSET 10").AssertTrue($"Should contain OFFSET 10, got: {sql}");
	}

	[TestMethod]
	public void AppendPagination_SQLite_SkipOnly_UsesLimitMinus1()
	{
		var dialect = GetDialect("SQLite");
		var sb = new System.Text.StringBuilder();
		dialect.AppendPagination(sb, 10, null, false);

		var sql = sb.ToString();
		sql.Contains("LIMIT -1").AssertTrue($"SQLite should use LIMIT -1 for skip-only, got: {sql}");
		sql.Contains("OFFSET 10").AssertTrue($"Should contain OFFSET 10, got: {sql}");
	}

	[TestMethod]
	public void AppendPagination_PostgreSql_SkipOnly()
	{
		var dialect = GetDialect("PostgreSql");
		var sb = new System.Text.StringBuilder();
		dialect.AppendPagination(sb, 10, null, false);

		var sql = sb.ToString();
		sql.Contains("LIMIT").AssertFalse($"PostgreSql should not add LIMIT for skip-only, got: {sql}");
		sql.Contains("OFFSET 10").AssertTrue($"Should contain OFFSET 10, got: {sql}");
	}

	#endregion

	#region ConvertToDbValue / ConvertFromDbValue Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void ConvertToDbValue_TimeSpan_ReturnsTicksAsLong(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var ts = TimeSpan.FromHours(2);

		var result = dialect.ConvertToDbValue(ts, typeof(TimeSpan));

		(result is long).AssertTrue($"Should convert TimeSpan to long (ticks), got: {result?.GetType()?.Name}");
		((long)result).AssertEqual(ts.Ticks);
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void ConvertToDbValue_Null_ReturnsDBNull(string dialectName)
	{
		var dialect = GetDialect(dialectName);

		var result = dialect.ConvertToDbValue(null, typeof(string));

		(result is DBNull).AssertTrue($"Null should be converted to DBNull.Value, got: {result?.GetType()?.Name}");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void ConvertToDbValue_RegularValue_PassThrough(string dialectName)
	{
		var dialect = GetDialect(dialectName);

		dialect.ConvertToDbValue(42, typeof(int)).AssertEqual(42);
		dialect.ConvertToDbValue("hello", typeof(string)).AssertEqual("hello");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void ConvertFromDbValue_LongToTimeSpan(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var ticks = TimeSpan.FromMinutes(30).Ticks;

		var result = dialect.ConvertFromDbValue(ticks, typeof(TimeSpan));

		(result is TimeSpan).AssertTrue($"Should convert long ticks back to TimeSpan, got: {result?.GetType()?.Name}");
		((TimeSpan)result).AssertEqual(TimeSpan.FromMinutes(30));
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void ConvertFromDbValue_Null_ReturnsNull(string dialectName)
	{
		var dialect = GetDialect(dialectName);

		dialect.ConvertFromDbValue(null, typeof(string)).AssertNull();
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void ConvertFromDbValue_DBNull_ReturnsNull(string dialectName)
	{
		var dialect = GetDialect(dialectName);

		dialect.ConvertFromDbValue(DBNull.Value, typeof(string)).AssertNull();
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void ConvertFromDbValue_RegularValue_PassThrough(string dialectName)
	{
		var dialect = GetDialect(dialectName);

		dialect.ConvertFromDbValue(42, typeof(int)).AssertEqual(42);
		dialect.ConvertFromDbValue("hello", typeof(string)).AssertEqual("hello");
	}

	#endregion

	#region MaxParameters Tests

	[TestMethod]
	[DataRow("SqlServer", 2000)]
	[DataRow("SQLite", 900)]
	[DataRow("PostgreSql", 65000)]
	public void MaxParameters(string dialectName, int expected)
	{
		GetDialect(dialectName).MaxParameters.AssertEqual(expected);
	}

	#endregion

	#region Missing ComparisonOperator Tests

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void CreateBuildCondition_GreaterOrEqual(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Col");

		var sql = Query.CreateBuildCondition("Col", ComparisonOperator.GreaterOrEqual, "p0").Render(dialect);

		sql.AssertEqual($"{qCol} >= @p0");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void CreateBuildCondition_LessOrEqual(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Col");

		var sql = Query.CreateBuildCondition("Col", ComparisonOperator.LessOrEqual, "p0").Render(dialect);

		sql.AssertEqual($"{qCol} <= @p0");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void CreateBuildCondition_Like(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var qCol = Quote(dialectName, "Col");

		var sql = Query.CreateBuildCondition("Col", ComparisonOperator.Like, "p0").Render(dialect);

		sql.AssertEqual($"{qCol} LIKE @p0");
	}

	#endregion
}
