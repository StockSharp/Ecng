namespace Ecng.Data;

using System.Data.Common;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Interface for database-specific SQL dialect.
/// Provides SQL syntax variations for different database providers.
/// </summary>
public interface ISqlDialect
{
	/// <summary>
	/// Gets the maximum number of parameters allowed in a single query.
	/// </summary>
	/// <remarks>
	/// SQL Server: 2100, SQLite: 999, PostgreSQL: 65535, MySQL: 65535.
	/// </remarks>
	int MaxParameters { get; }

	/// <summary>
	/// Gets the parameter prefix (e.g., "@" for SQL Server, "$" for PostgreSQL).
	/// </summary>
	string ParameterPrefix { get; }

	/// <summary>
	/// Quotes an identifier (table name, column name).
	/// </summary>
	/// <param name="identifier">The identifier to quote.</param>
	/// <returns>Quoted identifier.</returns>
	string QuoteIdentifier(string identifier);

	/// <summary>
	/// Gets the SQL type name for a CLR type.
	/// </summary>
	/// <param name="clrType">CLR type.</param>
	/// <returns>SQL type name.</returns>
	string GetSqlTypeName(Type clrType);

	/// <summary>
	/// Converts a value to database format if needed.
	/// </summary>
	/// <param name="value">Original value.</param>
	/// <param name="clrType">CLR type of the value.</param>
	/// <returns>Converted value.</returns>
	object ConvertToDbValue(object value, Type clrType);

	/// <summary>
	/// Converts a value from database format if needed.
	/// </summary>
	/// <param name="value">Database value.</param>
	/// <param name="targetType">Target CLR type.</param>
	/// <returns>Converted value.</returns>
	object ConvertFromDbValue(object value, Type targetType);

	/// <summary>
	/// Gets the SQL expression for retrieving the last inserted identity value.
	/// </summary>
	string GetIdentitySelect(string idCol) => throw new NotSupportedException();

	/// <summary>
	/// Formats a SKIP/OFFSET clause.
	/// </summary>
	string FormatSkip(string skip) => throw new NotSupportedException();

	/// <summary>
	/// Formats a TAKE/FETCH clause.
	/// </summary>
	string FormatTake(string take) => throw new NotSupportedException();

	/// <summary>
	/// Gets the SQL expression for the current local date/time.
	/// </summary>
	string Now() => throw new NotSupportedException();

	/// <summary>
	/// Gets the SQL expression for the current UTC date/time.
	/// </summary>
	string UtcNow() => throw new NotSupportedException();

	/// <summary>
	/// Gets the SQL expression for the current system local date/time with offset.
	/// </summary>
	string SysNow() => throw new NotSupportedException();

	/// <summary>
	/// Gets the SQL expression for the current system UTC date/time.
	/// </summary>
	string SysUtcNow() => throw new NotSupportedException();

	/// <summary>
	/// Gets the SQL expression for generating a new unique identifier.
	/// </summary>
	string NewId() => throw new NotSupportedException();

	/// <summary>
	/// Appends CREATE TABLE IF NOT EXISTS statement to a <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="tableName">Table name (unquoted).</param>
	/// <param name="columnDefs">Pre-built column definitions string.</param>
	void AppendCreateTable(StringBuilder sb, string tableName, string columnDefs);

	/// <summary>
	/// Appends DROP TABLE IF EXISTS statement to a <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="tableName">Table name (unquoted).</param>
	void AppendDropTable(StringBuilder sb, string tableName);

	/// <summary>
	/// Appends pagination (OFFSET/FETCH or LIMIT/OFFSET) to a <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="skip">Rows to skip, or null.</param>
	/// <param name="take">Rows to take, or null.</param>
	/// <param name="hasOrderBy">Whether the query already has an ORDER BY clause.</param>
	void AppendPagination(StringBuilder sb, long? skip, long? take, bool hasOrderBy);

	/// <summary>
	/// Appends UPSERT (MERGE / INSERT ON CONFLICT) statement to a <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="tableName">Table name (unquoted).</param>
	/// <param name="allColumns">All column names.</param>
	/// <param name="keyColumns">Key column names for matching.</param>
	void AppendUpsert(StringBuilder sb, string tableName, string[] allColumns, string[] keyColumns);

	/// <summary>
	/// Gets the SQL suffix for an identity (auto-increment primary key) column definition.
	/// </summary>
	string GetIdentityColumnSuffix();

	/// <summary>
	/// Gets an inline FOREIGN KEY constraint clause for use inside a CREATE TABLE column list.
	/// </summary>
	/// <param name="tableName">The table being created (used to build constraint name).</param>
	/// <param name="columnName">The referencing column.</param>
	/// <param name="refTableName">The referenced table.</param>
	/// <param name="refColumnName">The referenced column (typically the referenced table's primary key).</param>
	/// <returns>A constraint clause like <c>CONSTRAINT FK_X_Y FOREIGN KEY (Y) REFERENCES X (Id)</c>.</returns>
	string GetForeignKeyConstraint(string tableName, string columnName, string refTableName, string refColumnName)
		=> throw new NotSupportedException();

	/// <summary>
	/// Appends an ALTER TABLE ADD CONSTRAINT statement for a new foreign key.
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="tableName">The table on which to add the constraint.</param>
	/// <param name="columnName">The referencing column.</param>
	/// <param name="refTableName">The referenced table.</param>
	/// <param name="refColumnName">The referenced column.</param>
	void AppendAddForeignKey(StringBuilder sb, string tableName, string columnName, string refTableName, string refColumnName)
		=> throw new NotSupportedException();

	/// <summary>
	/// Gets the full column definition (SQL type + NULL/NOT NULL).
	/// </summary>
	/// <param name="clrType">CLR type of the column.</param>
	/// <param name="isNullable">Whether the column allows NULLs.</param>
	/// <param name="maxLength">Max length for string columns (0 = MAX/unlimited).</param>
	/// <param name="precision">Numeric precision (0 = use default).</param>
	/// <param name="scale">Numeric scale (0 = use default).</param>
	/// <returns>Column definition string (e.g. "NVARCHAR(128) NOT NULL").</returns>
	string GetColumnDefinition(Type clrType, bool isNullable, int maxLength = 0, int precision = 0, int scale = 0)
	{
		var typeName = GetSqlTypeName(clrType);
		return $"{typeName} {(isNullable ? "NULL" : "NOT NULL")}";
	}

	/// <summary>
	/// Appends ALTER TABLE ADD COLUMN statement.
	/// </summary>
	void AppendAddColumn(StringBuilder sb, string tableName, string columnName, string columnDef)
	{
		sb.Append($"ALTER TABLE {QuoteIdentifier(tableName)} ADD {QuoteIdentifier(columnName)} {columnDef}");
	}

	/// <summary>
	/// Appends ALTER TABLE ALTER COLUMN statement.
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="tableName">Table name (unquoted).</param>
	/// <param name="columnName">Column name (unquoted).</param>
	/// <param name="clrType">CLR type of the column.</param>
	/// <param name="isNullable">Whether the column allows NULLs.</param>
	/// <param name="maxLength">Max length for string/binary columns.</param>
	/// <param name="precision">Numeric precision (0 = use default).</param>
	/// <param name="scale">Numeric scale (0 = use default).</param>
	void AppendAlterColumn(StringBuilder sb, string tableName, string columnName, Type clrType, bool isNullable, int maxLength = 0, int precision = 0, int scale = 0)
	{
		var colDef = GetColumnDefinition(clrType, isNullable, maxLength, precision, scale);
		sb.Append($"ALTER TABLE {QuoteIdentifier(tableName)} ALTER COLUMN {QuoteIdentifier(columnName)} {colDef}");
	}

	/// <summary>
	/// Appends ALTER TABLE DROP COLUMN statement.
	/// </summary>
	void AppendDropColumn(StringBuilder sb, string tableName, string columnName)
	{
		sb.Append($"ALTER TABLE {QuoteIdentifier(tableName)} DROP COLUMN {QuoteIdentifier(columnName)}");
	}

	/// <summary>
	/// Normalizes a raw database type name to the canonical form used by this dialect.
	/// Used by schema comparison to match DB-reported types against <see cref="GetSqlTypeName"/> output.
	/// </summary>
	/// <param name="dbTypeName">Raw type name from database metadata.</param>
	/// <returns>Normalized type name.</returns>
	string NormalizeDbType(string dbTypeName) => dbTypeName.Trim().ToUpperInvariant();

	/// <summary>
	/// Appends a dialect-specific UPDATE ... SET ... WHERE statement.
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="tableName">Table name (unquoted).</param>
	/// <param name="setColumns">Column names for the SET clause.</param>
	/// <param name="whereColumns">Column names for the WHERE clause.</param>
	void AppendUpdateBy(StringBuilder sb, string tableName, string[] setColumns, string[] whereColumns)
	{
		if (whereColumns.Length == 0)
			throw new InvalidOperationException($"Cannot generate UPDATE for '{tableName}': no key columns specified for WHERE clause.");

		sb.AppendLine($"update {QuoteIdentifier(tableName)}");
		sb.AppendLine("set");

		for (var i = 0; i < setColumns.Length; i++)
		{
			var comma = i < setColumns.Length - 1 ? "," : "";
			sb.AppendLine($"\t{QuoteIdentifier(setColumns[i])} = {ParameterPrefix}{setColumns[i]}{comma}");
		}

		sb.AppendLine("where");
		for (var i = 0; i < whereColumns.Length; i++)
		{
			if (i > 0)
				sb.Append(" and ");
			sb.Append($"{QuoteIdentifier(whereColumns[i])} = {ParameterPrefix}{whereColumns[i]}");
		}
	}

	/// <summary>
	/// Appends a dialect-specific DELETE ... WHERE statement.
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="tableName">Table name (unquoted).</param>
	/// <param name="whereColumns">Column names for the WHERE clause.</param>
	void AppendDeleteBy(StringBuilder sb, string tableName, string[] whereColumns)
	{
		if (whereColumns.Length == 0)
			throw new InvalidOperationException($"Cannot generate DELETE for '{tableName}': no key columns specified for WHERE clause.");

		sb.AppendLine("delete");
		sb.Append($"from {QuoteIdentifier(tableName)}");
		sb.AppendLine();
		sb.AppendLine("where");
		for (var i = 0; i < whereColumns.Length; i++)
		{
			if (i > 0)
				sb.Append(" and ");
			sb.Append($"{QuoteIdentifier(whereColumns[i])} = {ParameterPrefix}{whereColumns[i]}");
		}
	}

	/// <summary>
	/// Gets the SQL string concatenation operator (+ for SQL Server, || for PostgreSQL/SQLite).
	/// </summary>
	string ConcatOperator => "+";

	/// <summary>
	/// Gets the SQL literal for boolean true (1 for SQL Server/SQLite, TRUE for PostgreSQL).
	/// </summary>
	string TrueLiteral => "1";

	/// <summary>
	/// Gets the SQL literal for boolean false (0 for SQL Server/SQLite, FALSE for PostgreSQL).
	/// </summary>
	string FalseLiteral => "0";

	/// <summary>
	/// Gets the Unicode string literal prefix (N for SQL Server, empty for PostgreSQL/SQLite).
	/// </summary>
	string UnicodePrefix => "N";

	/// <summary>
	/// Gets the SQL function name for string length (LEN for SQL Server, LENGTH for PostgreSQL/SQLite).
	/// </summary>
	string LenFunction => "len";

	/// <summary>
	/// Gets the SQL function name for null-coalescing (ISNULL for SQL Server, COALESCE for PostgreSQL/SQLite).
	/// </summary>
	string IsNullFunction => "isnull";

	/// <summary>
	/// Appends dialect-specific date part extraction opening.
	/// SQL Server: DATEPART(part, ...); PostgreSQL/SQLite: EXTRACT(part FROM ...).
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="part">Date part name (year, month, day, etc.).</param>
	void AppendDatePartOpen(StringBuilder sb, string part)
	{
		sb.Append($"datePart({part},");
	}

	/// <summary>
	/// Appends the closing part of a dialect-specific date part extraction.
	/// </summary>
	void AppendDatePartClose(StringBuilder sb)
	{
		sb.Append(')');
	}

	/// <summary>
	/// Appends dialect-specific DATEADD expression.
	/// SQL Server: dateAdd(part, amount, source); PostgreSQL: (source + make_interval(part => amount)); SQLite: datetime(source, amount || ' part').
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="part">Date part name (year, month, day, etc.).</param>
	/// <param name="amountSql">Rendered SQL for the amount to add.</param>
	/// <param name="sourceSql">Rendered SQL for the source date expression.</param>
	void AppendDateAdd(StringBuilder sb, string part, string amountSql, string sourceSql)
	{
		sb.Append($"dateAdd({part},{amountSql},{sourceSql})");
	}

	/// <summary>
	/// Appends the opening part of a dialect-specific TRIM expression.
	/// SQL Server: LTRIM(RTRIM(..., PostgreSQL/SQLite: TRIM(....
	/// </summary>
	void AppendTrimOpen(StringBuilder sb)
	{
		sb.Append("LTrim(RTrim(");
	}

	/// <summary>
	/// Appends the closing part of a dialect-specific TRIM expression.
	/// SQL Server: )), PostgreSQL/SQLite: ).
	/// </summary>
	void AppendTrimClose(StringBuilder sb)
	{
		sb.Append("))");
	}

	/// <summary>
	/// Appends a fallback ORDER BY clause when no explicit ordering or identity column exists.
	/// SQL Server requires ORDER BY for OFFSET/FETCH; PostgreSQL/SQLite do not.
	/// </summary>
	void AppendFallbackOrderBy(StringBuilder sb) { }

	/// <summary>
	/// Appends dialect-specific parameterized pagination clause.
	/// SQL Server outputs OFFSET then FETCH; PostgreSQL/SQLite output LIMIT then OFFSET.
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="skipParamExpr">Full parameter expression for skip (e.g. "@skip"), or null.</param>
	/// <param name="takeParamExpr">Full parameter expression for take (e.g. "@take"), or null.</param>
	void AppendPaginationParams(StringBuilder sb, string skipParamExpr, string takeParamExpr)
	{
		// Default: SqlServer order (OFFSET first, then FETCH)
		if (skipParamExpr is not null)
			sb.AppendLine(FormatSkip(skipParamExpr));
		if (takeParamExpr is not null)
			sb.AppendLine(FormatTake(takeParamExpr));
	}

	/// <summary>
	/// Gets the batch separator for this dialect (e.g. "GO" for SQL Server).
	/// Empty string means no batch separation is needed.
	/// </summary>
	string BatchSeparator => string.Empty;

	/// <summary>
	/// Appends UPDATE ... SET column = value WHERE column IS NULL statement.
	/// Used during migration to fill default values before altering nullability.
	/// </summary>
	/// <param name="sb">String builder.</param>
	/// <param name="tableName">Table name (unquoted).</param>
	/// <param name="columnName">Column name (unquoted).</param>
	/// <param name="defaultLiteral">SQL literal for the default value.</param>
	void AppendUpdateWhereNull(StringBuilder sb, string tableName, string columnName, string defaultLiteral)
	{
		sb.Append($"UPDATE {QuoteIdentifier(tableName)} SET {QuoteIdentifier(columnName)} = {defaultLiteral} WHERE {QuoteIdentifier(columnName)} IS NULL;");
	}

	/// <summary>
	/// Gets a SQL literal representing the default value for the given CLR type.
	/// Used during migration to fill NOT NULL columns before altering nullability.
	/// </summary>
	/// <param name="clrType">CLR type.</param>
	/// <returns>SQL literal string (e.g. "N''" for string, "0" for int).</returns>
	string GetDefaultLiteral(Type clrType)
	{
		clrType = clrType.IsNullable() ? clrType.GetUnderlyingType() : clrType;

		if (clrType == typeof(string))
			return UnicodePrefix + "''";
		if (clrType == typeof(bool))
			return FalseLiteral;
		if (clrType == typeof(DateTime) || clrType == typeof(DateTimeOffset))
			return "'0001-01-01T00:00:00'";
		if (clrType == typeof(Guid))
			return "'00000000-0000-0000-0000-000000000000'";
		if (clrType == typeof(byte[]))
			return "0x";
		if (clrType.IsNumeric())
			return "0";

		return "N''";
	}

	/// <summary>
	/// Reads column metadata from a live database.
	/// </summary>
	/// <param name="connection">Open database connection.</param>
	/// <param name="tableSchema">Schema filter (e.g. "dbo" for SQL Server, "public" for PostgreSQL). Null uses dialect default.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of column metadata from the database.</returns>
	Task<IReadOnlyList<DbColumnInfo>> ReadDbSchemaAsync(
		DbConnection connection,
		string tableSchema = null,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();
}
