namespace Ecng.Data;

using System.Data.Common;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Base class for SQL dialect implementations.
/// </summary>
public abstract class SqlDialectBase : ISqlDialect
{
	/// <inheritdoc />
	public virtual string ConcatOperator => "+";

	/// <inheritdoc />
	public virtual string TrueLiteral => "1";

	/// <inheritdoc />
	public virtual string FalseLiteral => "0";

	/// <inheritdoc />
	public virtual string BooleanCastSqlType => "bit";

	/// <inheritdoc />
	public virtual string UnicodePrefix => "N";

	/// <inheritdoc />
	public virtual string LenFunction => "len";

	/// <inheritdoc />
	public virtual string IsNullFunction => "isnull";

	/// <inheritdoc />
	public virtual void AppendDatePartOpen(StringBuilder sb, string part)
	{
		sb.Append($"datePart({part},");
	}

	/// <inheritdoc />
	public virtual void AppendDatePartClose(StringBuilder sb)
	{
		sb.Append(')');
	}

	/// <inheritdoc />
	public virtual void AppendDateAdd(StringBuilder sb, string part, string amountSql, string sourceSql)
	{
		sb.Append($"dateAdd({part},{amountSql},{sourceSql})");
	}

	/// <inheritdoc />
	public virtual void AppendTrimOpen(StringBuilder sb)
	{
		sb.Append("LTrim(RTrim(");
	}

	/// <inheritdoc />
	public virtual void AppendTrimClose(StringBuilder sb)
	{
		sb.Append("))");
	}

	/// <inheritdoc />
	public virtual void AppendFallbackOrderBy(StringBuilder sb) { }

	/// <inheritdoc />
	public virtual void AppendPaginationParams(StringBuilder sb, string skipParamExpr, string takeParamExpr)
	{
		// Default: SqlServer order (OFFSET first, then FETCH)
		if (skipParamExpr is not null)
			sb.AppendLine(FormatSkip(skipParamExpr));
		if (takeParamExpr is not null)
			sb.AppendLine(FormatTake(takeParamExpr));
	}

	/// <inheritdoc />
	public virtual string BatchSeparator => string.Empty;

	/// <inheritdoc />
	public abstract int MaxParameters { get; }

	/// <inheritdoc />
	public abstract string ParameterPrefix { get; }

	/// <inheritdoc />
	public abstract string QuoteIdentifier(string identifier);

	/// <inheritdoc />
	public abstract string GetSqlTypeName(Type clrType);

	/// <inheritdoc />
	public virtual object ConvertToDbValue(object value, Type clrType)
	{
		if (value is null)
			return DBNull.Value;

		// TimeSpan stored as ticks (BIGINT)
		if (value is TimeSpan ts)
			return ts.Ticks;

		return value;
	}

	/// <inheritdoc />
	public virtual object ConvertFromDbValue(object value, Type targetType)
	{
		if (value is null || value is DBNull)
			return null;

		// TimeSpan from ticks
		if (targetType == typeof(TimeSpan) && value is long ticks)
			return new TimeSpan(ticks);

		return value;
	}

	/// <inheritdoc />
	public abstract string GetIdentityColumnSuffix();

	/// <inheritdoc />
	public virtual string GetForeignKeyConstraint(string tableName, string columnName, string refTableName, string refColumnName)
		=> $"CONSTRAINT {QuoteIdentifier($"FK_{tableName}_{columnName}")} FOREIGN KEY ({QuoteIdentifier(columnName)}) REFERENCES {QuoteIdentifier(refTableName)} ({QuoteIdentifier(refColumnName)})";

	/// <inheritdoc />
	public virtual void AppendAddForeignKey(StringBuilder sb, string tableName, string columnName, string refTableName, string refColumnName)
	{
		sb.Append($"ALTER TABLE {QuoteIdentifier(tableName)} ADD {GetForeignKeyConstraint(tableName, columnName, refTableName, refColumnName)}");
	}

	/// <inheritdoc />
	public virtual void AppendCreateIndex(StringBuilder sb, string indexName, string tableName, string columnName, bool unique)
	{
		sb.Append("CREATE ");

		if (unique)
			sb.Append("UNIQUE ");

		sb.Append($"INDEX {QuoteIdentifier(indexName)} ON {QuoteIdentifier(tableName)} ({QuoteIdentifier(columnName)})");
	}

	/// <inheritdoc />
	public abstract void AppendCreateTable(StringBuilder sb, string tableName, string columnDefs);

	/// <inheritdoc />
	public abstract void AppendDropTable(StringBuilder sb, string tableName);

	/// <inheritdoc />
	public abstract void AppendPagination(StringBuilder sb, long? skip, long? take, bool hasOrderBy);

	/// <inheritdoc />
	public abstract void AppendUpsert(StringBuilder sb, string tableName, string[] allColumns, string[] keyColumns);

	/// <inheritdoc />
	public virtual void AppendInsertReturningClause(StringBuilder sb, string idColumn)
	{
		// Default: no RETURNING. SqlServer/SQLite read identity through a
		// separate scope_identity()/last_insert_rowid() select.
	}

	/// <inheritdoc />
	public virtual bool SupportsInsertReturning => false;

	/// <inheritdoc />
	public virtual string GetIdentitySelect(string idCol) => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual string FormatSkip(string skip) => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual string FormatTake(string take) => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual string Now() => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual string UtcNow() => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual string SysNow() => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual string SysUtcNow() => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual string NewId() => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual string GetColumnDefinition(Type clrType, bool isNullable, int maxLength = 0, int precision = 0, int scale = 0)
	{
		var typeName = GetSqlTypeName(clrType);
		return $"{typeName} {(isNullable ? "NULL" : "NOT NULL")}";
	}

	/// <inheritdoc />
	public virtual void AppendAddColumn(StringBuilder sb, string tableName, string columnName, string columnDef)
	{
		sb.Append($"ALTER TABLE {QuoteIdentifier(tableName)} ADD {QuoteIdentifier(columnName)} {columnDef}");
	}

	/// <inheritdoc />
	public virtual void AppendAlterColumn(StringBuilder sb, string tableName, string columnName, Type clrType, bool isNullable, int maxLength = 0, int precision = 0, int scale = 0)
	{
		var colDef = GetColumnDefinition(clrType, isNullable, maxLength, precision, scale);
		sb.Append($"ALTER TABLE {QuoteIdentifier(tableName)} ALTER COLUMN {QuoteIdentifier(columnName)} {colDef}");
	}

	/// <inheritdoc />
	public virtual void AppendDropColumn(StringBuilder sb, string tableName, string columnName)
	{
		sb.Append($"ALTER TABLE {QuoteIdentifier(tableName)} DROP COLUMN {QuoteIdentifier(columnName)}");
	}

	/// <inheritdoc />
	public virtual void AppendUpdateWhereNull(StringBuilder sb, string tableName, string columnName, string defaultLiteral)
	{
		sb.Append($"UPDATE {QuoteIdentifier(tableName)} SET {QuoteIdentifier(columnName)} = {defaultLiteral} WHERE {QuoteIdentifier(columnName)} IS NULL;");
	}

	/// <inheritdoc />
	public virtual string NormalizeDbType(string dbTypeName) => dbTypeName.Trim().ToUpperInvariant();

	/// <inheritdoc />
	public virtual void AppendUpdateBy(StringBuilder sb, string tableName, string[] setColumns, string[] whereColumns)
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

	/// <inheritdoc />
	public virtual void AppendDeleteBy(StringBuilder sb, string tableName, string[] whereColumns)
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

	/// <inheritdoc />
	public virtual string GetDefaultLiteral(Type clrType)
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

	/// <inheritdoc />
	public virtual Task<IReadOnlyList<DbColumnInfo>> ReadDbSchemaAsync(
		DbConnection connection,
		string tableSchema = null,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();
}
