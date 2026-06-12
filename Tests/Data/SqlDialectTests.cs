namespace Ecng.Tests.Data;

#if NET10_0_OR_GREATER
using System.Data.Common;
#endif

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
	[DataRow("SqlServer", "offset @skip rows", "fetch next @take rows only")]
	[DataRow("SQLite", "OFFSET @skip", "LIMIT @take")]
	[DataRow("PostgreSql", "OFFSET @skip", "LIMIT @take")]
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

	[TestMethod]
	public void SqlServerDialect_QuoteIdentifier_EscapesClosingBracket()
		=> SqlServerDialect.Instance.QuoteIdentifier("Col]Name").AssertEqual("[Col]]Name]");

	[TestMethod]
	public void SQLiteDialect_QuoteIdentifier_EscapesDoubleQuote()
		=> SQLiteDialect.Instance.QuoteIdentifier("Col\"Name").AssertEqual("\"Col\"\"Name\"");

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
	[DataRow("SQLite", "TEXT")]
	[DataRow("PostgreSql", "NUMERIC(18,8)")]
	public void GetSqlTypeName_Decimal(string dialectName, string expected)
	{
		GetDialect(dialectName).GetSqlTypeName(typeof(decimal)).AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "DATETIME2")]
	[DataRow("SQLite", "TEXT")]
	[DataRow("PostgreSql", "TIMESTAMPTZ")]
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
	public void SqlServerDialect_GetColumnDefinition_UsesMaxForOversizedString()
	{
		var def = SqlServerDialect.Instance.GetColumnDefinition(typeof(string), isNullable: false, maxLength: 4001);

		def.StartsWithIgnoreCase("NVARCHAR(MAX)").AssertTrue($"Expected NVARCHAR(MAX), got: {def}");
	}

	[TestMethod]
	public void SqlServerDialect_GetColumnDefinition_UsesMaxForOversizedBinary()
	{
		var def = SqlServerDialect.Instance.GetColumnDefinition(typeof(byte[]), isNullable: false, maxLength: 8001);

		def.StartsWithIgnoreCase("VARBINARY(MAX)").AssertTrue($"Expected VARBINARY(MAX), got: {def}");
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
	[DataRow("PostgreSql", "GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY")]
	public void GetIdentityColumnSuffix(string dialectName, string expected)
	{
		GetDialect(dialectName).GetIdentityColumnSuffix().AssertEqual(expected);
	}

	#endregion

	#region GetIdentitySelect Tests

	[TestMethod]
	[DataRow("SqlServer", "scope_identity() as Id")]
	[DataRow("SQLite", "last_insert_rowid() as Id")]
	[DataRow("PostgreSql", "lastval() as Id")]
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
	[DataRow("PostgreSql", "now()")]
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
	[DataRow("PostgreSql", "now()")]
	public void SysUtcNow(string dialectName, string expected)
	{
		GetDialect(dialectName).SysUtcNow().AssertEqual(expected);
	}

	/// <summary>
	/// BUG: <c>UtcNow()</c>/<c>SysUtcNow()</c> emit <c>now() AT TIME ZONE 'UTC'</c>, which
	/// yields a <c>timestamp WITHOUT time zone</c>. But this dialect maps every
	/// <c>DateTime</c> column to <c>TIMESTAMPTZ</c> (GetSqlTypeName), so comparing/writing
	/// the without-tz value against a <c>TIMESTAMPTZ</c> column makes PostgreSQL reinterpret
	/// it in the session time zone — shifting the moment by the session UTC offset on any
	/// server whose <c>TimeZone</c> is not UTC. The expression should produce a
	/// <c>timestamptz</c> "now".
	/// Expected: both return <c>"now()"</c> (a <c>timestamptz</c>, matching the TIMESTAMPTZ schema).
	/// Actual: both return <c>"now() AT TIME ZONE 'UTC'"</c> (timestamp without tz).
	/// File: Data.PostgreSql\PostgreSqlDialect.cs:222,228.
	/// </summary>
	[TestMethod]
	public void UtcNow_SysUtcNow_ReturnTimestampTz_NotWithoutTimeZone()
	{
		var dialect = PostgreSqlDialect.Instance;

		dialect.UtcNow().AssertEqual("now()",
			"UtcNow() must yield a timestamptz ('now()') to match the dialect's TIMESTAMPTZ DateTime columns, " +
			"not a timestamp-without-tz that gets shifted by the session time zone.");

		dialect.SysUtcNow().AssertEqual("now()",
			"SysUtcNow() must yield a timestamptz ('now()') to match the dialect's TIMESTAMPTZ DateTime columns, " +
			"not a timestamp-without-tz that gets shifted by the session time zone.");
	}

	[TestMethod]
	[DataRow("SqlServer", "newId()")]
	[DataRow("PostgreSql", "gen_random_uuid()")]
	public void NewId(string dialectName, string expected)
	{
		GetDialect(dialectName).NewId().AssertEqual(expected);
	}

	[TestMethod]
	public async Task SQLiteDialect_NewId_EvaluatesToProviderGuidTextFormat()
	{
		using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
		await connection.OpenAsync(CancellationToken);

		using var command = connection.CreateCommand();
		command.CommandText = $"SELECT {SQLiteDialect.Instance.NewId()}";

		var value = (string)await command.ExecuteScalarAsync(CancellationToken);

		value.Length.AssertEqual(36);
		value[8].AssertEqual('-');
		value[13].AssertEqual('-');
		value[18].AssertEqual('-');
		value[23].AssertEqual('-');
		value.AssertEqual(Guid.Parse(value).ToString("D"));
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
	public void ConvertFromDbValue_LongToNullableTimeSpan(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var ticks = TimeSpan.FromMinutes(5).Ticks;

		dialect.ConvertFromDbValue(ticks, typeof(TimeSpan?))
			.AssertEqual(TimeSpan.FromMinutes(5));

		dialect.ConvertFromDbValue(0L, typeof(TimeSpan?))
			.AssertEqual(TimeSpan.Zero);
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

	#region Foreign key constraint

	[TestMethod]
	[DataRow("SqlServer", "[", "]")]
	[DataRow("SQLite", "\"", "\"")]
	[DataRow("PostgreSql", "\"", "\"")]
	public void GetForeignKeyConstraint_ContainsClause(string dialectName, string qOpen, string qClose)
	{
		var dialect = GetDialect(dialectName);

		var fk = dialect.GetForeignKeyConstraint("Orders", "CustomerId", "Customers", "Id");

		fk.Contains("CONSTRAINT").AssertTrue($"Expected CONSTRAINT, got: {fk}");
		fk.Contains($"{qOpen}FK_Orders_CustomerId{qClose}").AssertTrue($"Expected constraint name, got: {fk}");
		fk.Contains($"FOREIGN KEY ({qOpen}CustomerId{qClose})").AssertTrue($"Expected FOREIGN KEY (col), got: {fk}");
		fk.Contains($"REFERENCES {qOpen}Customers{qClose} ({qOpen}Id{qClose})").AssertTrue($"Expected REFERENCES clause, got: {fk}");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("PostgreSql")]
	public void AppendAddForeignKey_EmitsAlterTable(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var sb = new System.Text.StringBuilder();

		dialect.AppendAddForeignKey(sb, "Orders", "CustomerId", "Customers", "Id");

		var sql = sb.ToString();

		sql.Contains("ALTER TABLE").AssertTrue($"Expected ALTER TABLE, got: {sql}");
		sql.Contains("FOREIGN KEY").AssertTrue($"Expected FOREIGN KEY, got: {sql}");
		sql.Contains("REFERENCES").AssertTrue($"Expected REFERENCES, got: {sql}");
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

	#region INSERT RETURNING

	[TestMethod]
	[DataRow("PostgreSql", "RETURNING \"Id\"")]
	[DataRow("SqlServer", "")]   // emits identity via scope_identity() in a separate read
	[DataRow("SQLite", "")]      // emits identity via last_insert_rowid() in a separate read
	public void Dialect_AppendInsertReturningClause_PerDialect(string dialectName, string expected)
	{
		var sb = new System.Text.StringBuilder();
		GetDialect(dialectName).AppendInsertReturningClause(sb, "Id");
		var sql = sb.ToString();

		if (expected.IsEmpty())
			sql.IsEmpty().AssertTrue($"Expected empty RETURNING for {dialectName}, got: {sql}");
		else
			sql.Contains(expected).AssertTrue($"Expected '{expected}' for {dialectName}, got: {sql}");
	}

	[TestMethod]
	public void PostgreSqlDialect_CreateInsert_WithReturning_EmitsRETURNING()
	{
		var sql = Query.CreateInsert("Users", ["Name", "Email"], returningIdColumn: "Id")
			.Render(PostgreSqlDialect.Instance);

		sql.Contains("INSERT INTO \"Users\"").AssertTrue($"Expected INSERT, got: {sql}");
		sql.Contains("RETURNING \"Id\"").AssertTrue($"Expected RETURNING clause, got: {sql}");
	}

	#endregion

	#region CREATE TABLE idempotency

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void Dialect_AppendCreateTable_IsIdempotent(string dialectName)
	{
		// Re-running a migration generated by SchemaMigrator must not fail
		// just because the table already exists. Every supported dialect
		// must emit an "IF NOT EXISTS"-style guard.
		var sb = new System.Text.StringBuilder();
		GetDialect(dialectName).AppendCreateTable(sb, "Users", "Id INT");
		var sql = sb.ToString();

		sql.ContainsIgnoreCase("IF NOT EXISTS").AssertTrue($"Expected idempotency guard, got: {sql}");
	}

	#endregion

	#region PG decimal precision/scale

	[TestMethod]
	[DataRow(10, 2, "NUMERIC(10,2)")]
	[DataRow(28, 8, "NUMERIC(28,8)")]
	[DataRow(0, 0, "NUMERIC(18,8)")] // fallback to GetSqlTypeName default
	public void PostgreSqlDialect_GetColumnDefinition_HonoursDecimalPrecisionScale(int precision, int scale, string expected)
	{
		var def = PostgreSqlDialect.Instance.GetColumnDefinition(typeof(decimal), isNullable: false, precision: precision, scale: scale);
		def.StartsWithIgnoreCase(expected).AssertTrue($"Expected '{expected}' prefix, got: {def}");
	}

	#endregion

	#region SQLite type normalization

	[TestMethod]
	[DataRow("VARCHAR(50)", "TEXT")]
	[DataRow("NVARCHAR(100)", "TEXT")]
	[DataRow("DECIMAL(18,2)", "REAL")]
	[DataRow("VARBINARY(16)", "BLOB")]
	public void SQLiteDialect_NormalizeDbType_StripsLengthAndPrecisionSuffix(string dbType, string expected)
		=> SQLiteDialect.Instance.NormalizeDbType(dbType).AssertEqual(expected);

	[TestMethod]
	public void SQLiteDialect_ListUserTablesSql_EscapesInternalTablePrefixUnderscore()
	{
		var method = typeof(SQLiteDialect).GetMethod("BuildListUserTablesSql", BindingFlags.Instance | BindingFlags.NonPublic);

		var sql = (string)method.Invoke(SQLiteDialect.Instance, []);

		sql.Contains("sqlite!_%").AssertTrue($"Expected escaped sqlite_ prefix, got: {sql}");
		sql.ContainsIgnoreCase("ESCAPE").AssertTrue($"Expected LIKE escape clause, got: {sql}");
	}

	#endregion

	#region SQLite unsupported migrations

	[TestMethod]
	public void SQLiteDialect_AppendAddForeignKey_ThrowsExplicitError()
	{
		// SQLite cannot ALTER TABLE ADD CONSTRAINT for foreign keys.
		// Migrations that hit this path must fail loudly so the operator
		// notices, instead of silently emitting a comment that the migrator
		// counts as success.
		var sb = new System.Text.StringBuilder();
		Assert.ThrowsExactly<NotSupportedException>(() =>
			SQLiteDialect.Instance.AppendAddForeignKey(sb, "T", "Fk", "Ref", "Id"));
	}

	#endregion

	#region DDL injection guards

	[TestMethod]
	public void SqlServerDialect_AppendCreateTable_EscapesSingleQuotesInTableName()
	{
		// Single-quote in table name must not break the IF NOT EXISTS literal
		// that compares against sys.tables.name.
		var sb = new System.Text.StringBuilder();
		SqlServerDialect.Instance.AppendCreateTable(sb, "evil'name", "x int");
		var sql = sb.ToString();

		sql.Contains("'evil''name'").AssertTrue($"Expected escaped 'evil''name' literal, got: {sql}");
		// Bare unescaped quote in the literal would produce two adjacent strings
		// and is a textbook injection surface.
		sql.Contains("'evil'name'").AssertFalse($"Single quote leaked unescaped, got: {sql}");
	}

	[TestMethod]
	public void SqlServerDialect_AppendDropTable_EscapesSingleQuotesInTableName()
	{
		var sb = new System.Text.StringBuilder();
		SqlServerDialect.Instance.AppendDropTable(sb, "evil'name");
		var sql = sb.ToString();

		sql.Contains("OBJECT_ID(N'[dbo].[evil''name]', N'U')").AssertTrue($"Expected escaped object name literal, got: {sql}");
		sql.Contains("OBJECT_ID(N'[dbo].[evil'name]', N'U')").AssertFalse($"Single quote leaked unescaped, got: {sql}");
	}

	#endregion

	#region AppendDateAdd Tests

	[TestMethod]
	[DataRow("SqlServer", "ORDER BY (SELECT NULL)")]
	[DataRow("PostgreSql", "ORDER BY 1")]
	public void Dialect_AppendFallbackOrderBy_EmitsDeterministicClause(string dialectName, string expected)
	{
		// Pagination on a result with no explicit ORDER BY and no identity
		// column must still produce a deterministic order, otherwise OFFSET
		// can return different rows on repeat calls.
		var dialect = GetDialect(dialectName);
		var sb = new System.Text.StringBuilder();

		dialect.AppendFallbackOrderBy(sb);
		var sql = sb.ToString().Trim();

		sql.ContainsIgnoreCase(expected).AssertTrue(
			$"Expected '{expected}' fallback for {dialectName}, got: '{sql}'");
	}

	[TestMethod]
	[DataRow("year", "years")]
	[DataRow("month", "months")]
	[DataRow("day", "days")]
	[DataRow("hour", "hours")]
	[DataRow("minute", "mins")]
	[DataRow("second", "secs")]
	[DataRow("millisecond", "secs")]
	public void PostgreSqlDialect_AppendDateAdd_UsesValidMakeIntervalKeyword(string part, string pgKeyword)
	{
		// PostgreSQL's make_interval() expects years/months/days/hours/mins/secs.
		var sb = new System.Text.StringBuilder();
		PostgreSqlDialect.Instance.AppendDateAdd(sb, part, "5", "now()");
		var sql = sb.ToString();

		sql.Contains($"{pgKeyword} =>").AssertTrue(
			$"Expected '{pgKeyword} =>' in PostgreSQL DateAdd SQL, got: {sql}");
	}

	private static string PostgreSqlDatePart(string part)
	{
		var sb = new System.Text.StringBuilder();
		PostgreSqlDialect.Instance.AppendDatePartOpen(sb, part);
		sb.Append("ts");
		PostgreSqlDialect.Instance.AppendDatePartClose(sb);
		return sb.ToString();
	}

	[TestMethod]
	public void PostgreSqlDialect_AppendDatePart_DayOfYearUsesDoyToken()
		=> PostgreSqlDatePart("dayofyear").AssertEqual("EXTRACT(doy FROM ts)");

	[TestMethod]
	public void PostgreSqlDialect_AppendDatePart_SecondDropsFraction()
	{
		var sql = PostgreSqlDatePart("second");

		sql.Contains("FLOOR(").AssertTrue($"DateTime.Second must ignore fractional seconds, got: {sql}");
		sql.Contains("EXTRACT(second FROM").AssertTrue($"Expected second extraction, got: {sql}");
	}

	[TestMethod]
	public void PostgreSqlDialect_AppendDatePart_MillisecondReturnsSubsecondRemainder()
	{
		var sql = PostgreSqlDatePart("millisecond");

		sql.Contains("milliseconds").AssertTrue($"Expected PostgreSQL milliseconds extraction, got: {sql}");
		sql.Contains("% 1000").AssertTrue($"DateTime.Millisecond must return 0-999 remainder, got: {sql}");
	}

	[TestMethod]
	public void PostgreSqlDialect_AppendPagination_AddsFallbackOrderByWhenMissingOrder()
	{
		var sb = new System.Text.StringBuilder();

		PostgreSqlDialect.Instance.AppendPagination(sb, skip: 10, take: 20, hasOrderBy: false);

		var sql = sb.ToString();
		sql.Contains("ORDER BY 1").AssertTrue($"Expected fallback ORDER BY for paginated PostgreSQL query, got: {sql}");
	}

	#endregion

	#region GetDefaultLiteral

	[TestMethod]
	[DataRow("SqlServer", "N''")]
	[DataRow("SQLite", "''")]
	[DataRow("PostgreSql", "''")]
	public void GetDefaultLiteral_String(string dialectName, string expected)
		=> GetDialect(dialectName).GetDefaultLiteral(typeof(string)).AssertEqual(expected);

	[TestMethod]
	[DataRow("SqlServer", "0")]
	[DataRow("SQLite", "0")]
	[DataRow("PostgreSql", "FALSE")]
	public void GetDefaultLiteral_Bool(string dialectName, string expected)
		=> GetDialect(dialectName).GetDefaultLiteral(typeof(bool)).AssertEqual(expected);

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void GetDefaultLiteral_Numeric(string dialectName)
		=> GetDialect(dialectName).GetDefaultLiteral(typeof(int)).AssertEqual("0");

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void GetDefaultLiteral_TimeSpan(string dialectName)
		=> GetDialect(dialectName).GetDefaultLiteral(typeof(TimeSpan)).AssertEqual("0");

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void GetDefaultLiteral_Guid(string dialectName)
		=> GetDialect(dialectName).GetDefaultLiteral(typeof(Guid))
			.AssertEqual("'00000000-0000-0000-0000-000000000000'");

	/// <summary>
	/// Each dialect must emit binary-empty syntax it can actually parse:
	/// SQL Server uses 0x, SQLite uses the SQL-standard X'' bit-string
	/// literal, PostgreSQL uses an empty bytea.
	/// </summary>
	[TestMethod]
	[DataRow("SqlServer", "0x")]
	[DataRow("SQLite", "X''")]
	[DataRow("PostgreSql", "'\\x'::bytea")]
	public void GetDefaultLiteral_Binary(string dialectName, string expected)
		=> GetDialect(dialectName).GetDefaultLiteral(typeof(byte[])).AssertEqual(expected);

	/// <summary>
	/// The fallback branch (type that matches none of the explicit cases)
	/// must respect the dialect's unicode prefix, not hardcode the
	/// SQL Server N'' literal.
	/// </summary>
	[TestMethod]
	[DataRow("SqlServer", "N''")]
	[DataRow("SQLite", "''")]
	[DataRow("PostgreSql", "''")]
	public void GetDefaultLiteral_UnknownType_Fallback(string dialectName, string expected)
		=> GetDialect(dialectName).GetDefaultLiteral(typeof(object)).AssertEqual(expected);

	#endregion

	#region SqlServer DDL shape regressions

	/// <summary>
	/// BUG: <c>AppendUpsert</c> emits a bare <c>MERGE {table} AS target USING ...</c> with no
	/// locking hint. Under the default READ COMMITTED isolation a SQL Server MERGE takes no
	/// key-range locks, so two concurrent upserts of the same fresh key can both evaluate the
	/// ON predicate as "not matched" and both run the INSERT branch — either a duplicate-key
	/// exception (burned as a transient retry) or silent duplicate rows. The standard race-free
	/// form pins the target with <c>WITH (HOLDLOCK)</c> (a SERIALIZABLE range lock for the
	/// statement), matching the atomic <c>INSERT ... ON CONFLICT</c> the other dialects use.
	/// Expected: the generated MERGE locks the target via <c>WITH (HOLDLOCK)</c>.
	/// Actual: no HOLDLOCK / SERIALIZABLE hint is present — the upsert is racy.
	/// File: Data.SqlServer\SqlServerDialect.cs:456.
	/// </summary>
	[TestMethod]
	public void AppendUpsert_LocksTargetWithHoldlock_ToPreventUpsertRace()
	{
		var sb = new StringBuilder();
		SqlServerDialect.Instance.AppendUpsert(sb, "TestTable",
			allColumns: ["Id", "Name", "Value"], keyColumns: ["Id"]);
		var sql = sb.ToString();

		// Accept either spelling of the range-lock hint that closes the upsert race:
		// the documented fix is WITH (HOLDLOCK); SERIALIZABLE is its synonym.
		var hasHoldlock = sql.ContainsIgnoreCase("HOLDLOCK") || sql.ContainsIgnoreCase("SERIALIZABLE");

		hasHoldlock.AssertTrue(
			$"MERGE upsert must lock the target (WITH (HOLDLOCK)) so two concurrent upserts of the " +
			$"same key cannot both INSERT; got: {sql}");
	}

	/// <summary>
	/// BUG: <c>AppendCreateTable</c> guards creation with
	/// <c>IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{name}')</c> — no schema filter.
	/// <c>sys.tables</c> spans every schema, so when a same-named table exists in another schema
	/// (e.g. <c>history.Orders</c>) the guard is satisfied and the <c>dbo</c> table is silently
	/// never created, while the migrator's ReadDb* methods (which default to <c>dbo</c>) keep
	/// reporting it missing. The check must be scoped to the schema the table is created in.
	/// Expected: the existence guard is schema-scoped (SCHEMA_ID/schema_id or an OBJECT_ID over
	/// a schema-qualified name), not a bare name match across all schemas.
	/// Actual: the guard matches by name only, ignoring the schema.
	/// File: Data.SqlServer\SqlServerDialect.cs:154.
	/// </summary>
	[TestMethod]
	public void AppendCreateTable_ExistenceGuard_IsSchemaScoped()
	{
		var sb = new StringBuilder();
		SqlServerDialect.Instance.AppendCreateTable(sb, "Orders", "Id int NOT NULL");
		var sql = sb.ToString();

		// A schema-scoped guard pins the existence check to a single schema. Any of these tokens
		// signals that: a SCHEMA_ID('dbo')/schema_id filter, or an OBJECT_ID over a schema-qualified
		// name (e.g. N'dbo.' + QUOTENAME(...)). A bare "WHERE name = '...'" across sys.tables has none.
		var isSchemaScoped =
			sql.ContainsIgnoreCase("SCHEMA_ID") ||
			sql.ContainsIgnoreCase("schema_id") ||
			sql.ContainsIgnoreCase("dbo.");

		isSchemaScoped.AssertTrue(
			$"CREATE TABLE existence guard must be schema-scoped so a same-named table in another " +
			$"schema does not suppress creation in the target schema; got: {sql}");
	}

	#endregion

#if NET10_0_OR_GREATER

	#region ReadDbForeignKeysAsync composite FK

	[ClassInitialize]
	public static void ClassInit(TestContext context) => DbTestHelper.RegisterAll();

	[ClassCleanup]
	public static void ClassCleanup() => DbTestHelper.ClearSQLitePools();

	private static async Task<DbConnection> OpenAsync(string provider, CancellationToken cancellationToken)
	{
		var factory = DbTestHelper.GetFactory(provider);
		var connStr = DbTestHelper.TryGetConnectionString(provider);
		var conn = factory.CreateConnection();
		conn.ConnectionString = connStr;
		await conn.OpenAsync(cancellationToken);
		return conn;
	}

	/// <summary>
	/// BUG: <c>ReadDbForeignKeysAsync</c> joins <c>information_schema.key_column_usage</c>
	/// (one row per FK column, ordered) to <c>information_schema.constraint_column_usage</c>
	/// (one row per referenced column, unordered) by constraint name only. For an N-column
	/// composite FK this is a Cartesian product: N*N rows where each referencing column is
	/// paired with every referenced column, so <c>RefColumnName</c> is wrong for most rows
	/// and the rows are duplicated. The contract (<see cref="DbForeignKeyInfo"/>) promises
	/// exactly one row per referencing/referenced column pair, position-aligned.
	/// Expected: a 2-column composite FK yields exactly 2 rows, each pairing the referencing
	/// column with its correctly positioned referenced column.
	/// Actual: 4 rows (2*2) with mis-paired/duplicated referenced columns.
	/// File: Data.PostgreSql\PostgreSqlDialect.cs:403.
	/// </summary>
	[TestMethod]
	[TestCategory("Integration")]
	public async Task ReadDbForeignKeysAsync_CompositeFk_PairsColumnsByPosition_NoCartesianProduct()
	{
		const string provider = DatabaseProviderRegistry.PostgreSql;
		DbTestHelper.SkipIfUnavailable(provider);

		const string parentTable = "Ecng_PgCompositeFkParent";
		const string childTable = "Ecng_PgCompositeFkChild";

		var dialect = DbTestHelper.GetDialect(provider);

		// Child first so the FK no longer references the parent we drop next.
		DbTestHelper.DropTable(provider, childTable);
		DbTestHelper.DropTable(provider, parentTable);

		var longType = dialect.GetSqlTypeName(typeof(long));
		var qParent = dialect.QuoteIdentifier(parentTable);
		var qChild = dialect.QuoteIdentifier(childTable);
		var qA = dialect.QuoteIdentifier("KeyA");
		var qB = dialect.QuoteIdentifier("KeyB");
		var qFkA = dialect.QuoteIdentifier("ParentA");
		var qFkB = dialect.QuoteIdentifier("ParentB");
		var qConstraint = dialect.QuoteIdentifier("FK_PgComposite");

		// Parent with a two-column composite primary key.
		DbTestHelper.ExecuteRaw(provider,
			$"CREATE TABLE {qParent} ({qA} {longType} NOT NULL, {qB} {longType} NOT NULL, " +
			$"PRIMARY KEY ({qA}, {qB}))");

		// Child with a two-column composite foreign key referencing the parent's PK.
		DbTestHelper.ExecuteRaw(provider,
			$"CREATE TABLE {qChild} (" +
			$"{dialect.QuoteIdentifier("Id")} {longType} NOT NULL PRIMARY KEY, " +
			$"{qFkA} {longType} NOT NULL, {qFkB} {longType} NOT NULL, " +
			$"CONSTRAINT {qConstraint} FOREIGN KEY ({qFkA}, {qFkB}) " +
			$"REFERENCES {qParent} ({qA}, {qB}))");

		try
		{
			using var conn = await OpenAsync(provider, CancellationToken);

			var fks = (await dialect.ReadDbForeignKeysAsync(conn, cancellationToken: CancellationToken))
				.Where(fk => fk.TableName.EqualsIgnoreCase(childTable))
				.ToArray();

			var dump = string.Join(", ",
				fks.Select(f => $"{f.TableName}.{f.ColumnName}->{f.RefTableName}.{f.RefColumnName}"));

			// Exactly two pairs — not 2*2 = 4 Cartesian rows.
			fks.Length.AssertEqual(2,
				$"A 2-column composite FK must surface exactly 2 column pairs, not a Cartesian product; got: [{dump}]");

			// ParentA references KeyA, ParentB references KeyB — paired by position.
			var pairA = fks.FirstOrDefault(f => f.ColumnName.EqualsIgnoreCase("ParentA"));
			pairA.AssertNotNull($"Expected a pair for the ParentA column; got: [{dump}]");
			pairA.RefColumnName.EqualsIgnoreCase("KeyA").AssertTrue(
				$"ParentA must reference KeyA (position-aligned), got {pairA.RefColumnName}; all: [{dump}]");

			var pairB = fks.FirstOrDefault(f => f.ColumnName.EqualsIgnoreCase("ParentB"));
			pairB.AssertNotNull($"Expected a pair for the ParentB column; got: [{dump}]");
			pairB.RefColumnName.EqualsIgnoreCase("KeyB").AssertTrue(
				$"ParentB must reference KeyB (position-aligned), got {pairB.RefColumnName}; all: [{dump}]");

			pairA.RefTableName.EqualsIgnoreCase(parentTable).AssertTrue(
				$"Expected ref table {parentTable}, got {pairA.RefTableName}");
		}
		finally
		{
			DbTestHelper.DropTable(provider, childTable);
			DbTestHelper.DropTable(provider, parentTable);
		}
	}

	#endregion

	#region SQLite schema-read robustness

	/// <summary>
	/// BUG: <c>ReadDbForeignKeysAsync</c> reads the referenced column with
	/// <c>reader.GetString(4)</c> from <c>PRAGMA foreign_key_list</c>. SQLite returns NULL in
	/// the "to" column when the FK references the parent's implicit primary key — the common
	/// shorthand <c>REFERENCES Parent</c> without an explicit column list. Microsoft.Data.Sqlite
	/// throws on <c>GetString</c> over a NULL ordinal, so the whole schema read (and therefore
	/// <c>SchemaMigrator.CompareAsync</c>) blows up for any externally created DB that uses this
	/// perfectly legal FK syntax.
	/// Expected: the read completes without throwing and surfaces the FK row for the child
	/// column (RefTableName resolved to the parent; RefColumnName resolved to the parent PK or
	/// left null/empty so the diff layer can cope).
	/// Actual: <c>InvalidOperationException</c> is thrown on the NULL "to" ordinal.
	/// File: Data.SQLite\SQLiteDialect.cs:322.
	/// </summary>
	[TestMethod]
	[TestCategory("Integration")]
	public async Task ReadDbForeignKeysAsync_ImplicitParentPkRef_DoesNotThrow()
	{
		const string provider = DatabaseProviderRegistry.SQLite;
		DbTestHelper.SkipIfUnavailable(provider);

		const string parentTable = "Ecng_SqliteImplicitFkParent";
		const string childTable = "Ecng_SqliteImplicitFkChild";

		var dialect = DbTestHelper.GetDialect(provider);

		// Child first so the FK no longer references the parent we drop next.
		DbTestHelper.DropTable(provider, childTable);
		DbTestHelper.DropTable(provider, parentTable);

		var qParent = dialect.QuoteIdentifier(parentTable);
		var qChild = dialect.QuoteIdentifier(childTable);
		var qPid = dialect.QuoteIdentifier("Pid");

		// Parent with a single-column integer primary key.
		DbTestHelper.ExecuteRaw(provider,
			$"CREATE TABLE {qParent} (Id INTEGER PRIMARY KEY)");

		// Child FK uses the implicit-PK shorthand: REFERENCES Parent with no column list,
		// so PRAGMA foreign_key_list returns NULL in the "to" column.
		DbTestHelper.ExecuteRaw(provider,
			$"CREATE TABLE {qChild} ({qPid} INTEGER REFERENCES {qParent})");

		try
		{
			using var conn = await OpenAsync(provider, CancellationToken);

			IReadOnlyList<DbForeignKeyInfo> fks = null;

			// The defect throws here; the correct behaviour is a clean read.
			try
			{
				fks = await dialect.ReadDbForeignKeysAsync(conn, cancellationToken: CancellationToken);
			}
			catch (Exception ex)
			{
				Fail($"ReadDbForeignKeysAsync must tolerate an implicit-PK FK (NULL 'to' column) " +
					$"and not throw, but it threw {ex.GetType().Name}: {ex.Message}");
			}

			var match = fks.FirstOrDefault(fk =>
				fk.TableName.EqualsIgnoreCase(childTable) &&
				fk.ColumnName.EqualsIgnoreCase("Pid"));

			var dump = string.Join(", ",
				fks.Select(f => $"{f.TableName}.{f.ColumnName}->{f.RefTableName}.{f.RefColumnName}"));

			match.AssertNotNull(
				$"Expected an FK row for {childTable}.Pid referencing {parentTable}; got: [{dump}]");

			match.RefTableName.EqualsIgnoreCase(parentTable).AssertTrue(
				$"Expected ref table {parentTable}, got {match.RefTableName}; all: [{dump}]");
		}
		finally
		{
			DbTestHelper.DropTable(provider, childTable);
			DbTestHelper.DropTable(provider, parentTable);
		}
	}

	/// <summary>
	/// BUG: <c>ReadDbIndexesAsync</c> reads the column name with <c>reader.GetString(2)</c> from
	/// <c>PRAGMA index_info</c>. For an expression index (e.g. <c>CREATE INDEX i ON t(lower(Name))</c>,
	/// cid = -2) or a rowid key (cid = -1) SQLite returns NULL there, and Microsoft.Data.Sqlite
	/// throws on <c>GetString</c> over the NULL ordinal. Because every index from
	/// <c>index_list</c> is enumerated unconditionally, a single expression index makes the whole
	/// schema read — and thus <c>SchemaMigrator.CompareAsync</c> — fail for any DB that legitimately
	/// uses one.
	/// Expected: the read completes without throwing; expression-key rows are simply skipped
	/// (they cannot be matched to a single-column entity index anyway), while ordinary
	/// single-column indexes on the same table are still surfaced.
	/// Actual: <c>InvalidOperationException</c> is thrown on the NULL column-name ordinal.
	/// File: Data.SQLite\SQLiteDialect.cs:389.
	/// </summary>
	[TestMethod]
	[TestCategory("Integration")]
	public async Task ReadDbIndexesAsync_ExpressionIndex_DoesNotThrow()
	{
		const string provider = DatabaseProviderRegistry.SQLite;
		DbTestHelper.SkipIfUnavailable(provider);

		const string table = "Ecng_SqliteExprIndex";

		var dialect = DbTestHelper.GetDialect(provider);

		DbTestHelper.DropTable(provider, table);

		var qTable = dialect.QuoteIdentifier(table);
		var qName = dialect.QuoteIdentifier("Name");
		var qPriority = dialect.QuoteIdentifier("Priority");

		DbTestHelper.ExecuteRaw(provider,
			$"CREATE TABLE {qTable} (Id INTEGER PRIMARY KEY, {qName} TEXT, {qPriority} INTEGER)");

		// Expression index — PRAGMA index_info reports cid = -2 and a NULL column name.
		DbTestHelper.ExecuteRaw(provider,
			$"CREATE INDEX {dialect.QuoteIdentifier("IX_Ecng_SqliteExprIndex_LowerName")} " +
			$"ON {qTable} (lower({qName}))");

		// A plain single-column index on the same table that must still be surfaced.
		DbTestHelper.ExecuteRaw(provider,
			$"CREATE INDEX {dialect.QuoteIdentifier("IX_Ecng_SqliteExprIndex_Priority")} " +
			$"ON {qTable} ({qPriority})");

		try
		{
			using var conn = await OpenAsync(provider, CancellationToken);

			IReadOnlyList<DbIndexInfo> indexes = null;

			// The defect throws here; the correct behaviour is a clean read.
			try
			{
				indexes = await dialect.ReadDbIndexesAsync(conn, cancellationToken: CancellationToken);
			}
			catch (Exception ex)
			{
				Fail($"ReadDbIndexesAsync must tolerate an expression index (NULL column name) " +
					$"and not throw, but it threw {ex.GetType().Name}: {ex.Message}");
			}

			var tableIxs = indexes
				.Where(i => i.TableName.EqualsIgnoreCase(table) && !i.IsPrimaryKey)
				.ToArray();

			var dump = string.Join(", ",
				tableIxs.Select(i => $"{i.IndexName}({i.ColumnName})"));

			// The ordinary single-column index must survive the expression-index row.
			tableIxs.Any(i => i.ColumnName.EqualsIgnoreCase("Priority")).AssertTrue(
				$"Expected the plain Priority index to be surfaced alongside the expression index; got: [{dump}]");
		}
		finally
		{
			DbTestHelper.DropTable(provider, table);
		}
	}

	#endregion

	#region SqlServer schema-read robustness

	/// <summary>
	/// BUG: <c>ReadDbSchemaAsync</c> probes IsComputed via
	/// <c>COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), ...)</c>. OBJECT_ID parses
	/// its argument as a multi-part name, so a table whose name itself contains a dot (e.g.
	/// <c>[Order.Items]</c>) resolves to NULL, COLUMNPROPERTY returns NULL and a genuinely computed
	/// column is silently reported as <c>IsComputed=false</c>. Such names are creatable because all
	/// DDL goes through the bracket-quoting <see cref="SqlServerDialect.QuoteIdentifier"/>. The probe
	/// must quote each name part (e.g. QUOTENAME) so the OBJECT_ID resolves.
	/// Expected: a computed column on a dotted/quote-requiring table name is read back as IsComputed=true.
	/// Actual: it is silently read back as IsComputed=false.
	/// File: Data.SqlServer\SqlServerDialect.cs:203.
	/// </summary>
	[TestMethod]
	[TestCategory("Integration")]
	[TestCategory("Database")]
	public async Task ReadDbSchemaAsync_DetectsComputedColumn_OnQuoteRequiringTableName()
	{
		const string provider = DatabaseProviderRegistry.SqlServer;
		DbTestHelper.SkipIfUnavailable(provider);

		// Dotted name: needs bracket quoting and breaks the unquoted OBJECT_ID concatenation.
		const string tableName = "Ecng.SqlComputedProbe";
		var quoted = "[" + tableName.Replace("]", "]]") + "]";

		DropTableRaw(provider, tableName);

		// Base + Total, where Total is a persisted computed column (Base * 2).
		DbTestHelper.ExecuteRaw(provider,
			$"CREATE TABLE {quoted} (" +
			$"[Base] INT NOT NULL, " +
			$"[Total] AS ([Base] * 2))");

		try
		{
			using var conn = await OpenAsync(provider, CancellationToken);

			var cols = await SqlServerDialect.Instance.ReadDbSchemaAsync(conn, cancellationToken: CancellationToken);

			var total = cols.FirstOrDefault(c =>
				c.TableName.EqualsIgnoreCase(tableName) && c.ColumnName.EqualsIgnoreCase("Total"));

			total.AssertNotNull($"Expected the '{tableName}'.'Total' column to be read back.");

			total.IsComputed.AssertTrue(
				"A persisted computed column on a quote-requiring (dotted) table name must be read " +
				"back as IsComputed=true; the OBJECT_ID probe must quote the name parts instead of " +
				"silently resolving to NULL.");
		}
		finally
		{
			DropTableRaw(provider, tableName);
		}
	}

	/// <summary>
	/// BUG: <c>ReadDbSchemaAsync</c> reads <c>INFORMATION_SCHEMA.COLUMNS</c> filtered only by
	/// <c>TABLE_SCHEMA</c>. That view also exposes view columns, so every view in the schema surfaces
	/// as a "table" in the returned <see cref="DbColumnInfo"/> list. The migrator then mis-reports
	/// views as ExtraTable, or — when a view name collides with an entity table — compares the entity
	/// against the view's columns instead of reporting the real table missing. The read must be
	/// restricted to base tables (TABLE_TYPE = 'BASE TABLE'), like the SQLite dialect's
	/// <c>type = 'table'</c> filter.
	/// Expected: a view's columns are NOT present in the schema read; only base-table columns are.
	/// Actual: the view's columns are returned as if they were table columns.
	/// File: Data.SqlServer\SqlServerDialect.cs:204.
	/// </summary>
	[TestMethod]
	[TestCategory("Integration")]
	[TestCategory("Database")]
	public async Task ReadDbSchemaAsync_ExcludesViewColumns_OnlyBaseTables()
	{
		const string provider = DatabaseProviderRegistry.SqlServer;
		DbTestHelper.SkipIfUnavailable(provider);

		const string tableName = "Ecng_SqlBaseTableForView";
		const string viewName = "Ecng_SqlViewOverBase";

		DropViewRaw(provider, viewName);
		DropTableRaw(provider, tableName);

		DbTestHelper.ExecuteRaw(provider,
			$"CREATE TABLE [{tableName}] ([Id] INT NOT NULL, [Name] NVARCHAR(50) NULL)");

		DbTestHelper.ExecuteRaw(provider,
			$"CREATE VIEW [{viewName}] AS SELECT [Id], [Name] FROM [{tableName}]");

		try
		{
			using var conn = await OpenAsync(provider, CancellationToken);

			var cols = await SqlServerDialect.Instance.ReadDbSchemaAsync(conn, cancellationToken: CancellationToken);

			var viewCols = cols.Where(c => c.TableName.EqualsIgnoreCase(viewName)).ToArray();
			var tableCols = cols.Where(c => c.TableName.EqualsIgnoreCase(tableName)).ToArray();

			// Sanity: the base table must be present (proves the read works and the filter
			// is not over-restrictive).
			tableCols.Length.AssertGreater(0,
				$"The base table '{tableName}' columns must be returned by the schema read.");

			viewCols.Length.AssertEqual(0,
				$"View columns must not surface as table columns; the read must restrict to base " +
				$"tables (TABLE_TYPE = 'BASE TABLE'). Got {viewCols.Length} column(s) for view '{viewName}'.");
		}
		finally
		{
			DropViewRaw(provider, viewName);
			DropTableRaw(provider, tableName);
		}
	}

	private static void DropTableRaw(string provider, string tableName)
	{
		var dialect = DbTestHelper.GetDialect(provider);
		DbTestHelper.ExecuteRaw(provider, Query.CreateDropTable(tableName).Render(dialect));
	}

	private static void DropViewRaw(string provider, string viewName)
	{
		var quoted = "[" + viewName.Replace("]", "]]") + "]";
		var literal = $"[dbo].{quoted}".Replace("'", "''");
		DbTestHelper.ExecuteRaw(provider,
			$"IF OBJECT_ID(N'{literal}', N'V') IS NOT NULL DROP VIEW {quoted}");
	}

	#endregion

#endif
}
