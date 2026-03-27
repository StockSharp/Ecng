namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// PostgreSQL dialect implementation.
/// </summary>
public class PostgreSqlDialect : SqlDialectBase
{
	/// <summary>
	/// Singleton instance.
	/// </summary>
	public static readonly PostgreSqlDialect Instance = new();

	private PostgreSqlDialect() { }

	/// <summary>
	/// Registers the PostgreSQL dialect and provider factory with <see cref="DatabaseProviderRegistry"/>.
	/// </summary>
	/// <param name="factory">The <see cref="DbProviderFactory"/> for PostgreSQL.</param>
	public static void Register(DbProviderFactory factory)
	{
		DatabaseProviderRegistry.Register(DatabaseProviderRegistry.PostgreSql, factory);
		DatabaseProviderRegistry.RegisterDialect(DatabaseProviderRegistry.PostgreSql, Instance);
	}

	/// <inheritdoc />
	public override int MaxParameters => 65000; // PostgreSQL limit is 65535, use 65000 for safety

	/// <inheritdoc />
	public override string ParameterPrefix => "@";

	/// <inheritdoc />
	public override string QuoteIdentifier(string identifier)
		=> $"\"{identifier}\"";

	/// <inheritdoc />
	public override string GetSqlTypeName(Type clrType)
	{
		var underlying = clrType.GetUnderlyingType() ?? clrType;

		return underlying switch
		{
			_ when underlying == typeof(int) => "INTEGER",
			_ when underlying == typeof(long) => "BIGINT",
			_ when underlying == typeof(short) => "SMALLINT",
			_ when underlying == typeof(byte) => "SMALLINT", // PostgreSQL has no single-byte integer
			_ when underlying == typeof(bool) => "BOOLEAN",
			_ when underlying == typeof(decimal) => "NUMERIC(18,8)",
			_ when underlying == typeof(double) => "DOUBLE PRECISION",
			_ when underlying == typeof(float) => "REAL",
			_ when underlying == typeof(string) => "TEXT",
			_ when underlying == typeof(DateTime) => "TIMESTAMP",
			_ when underlying == typeof(DateTimeOffset) => "TIMESTAMPTZ",
			_ when underlying == typeof(TimeSpan) => "BIGINT", // stored as ticks
			_ when underlying == typeof(Guid) => "UUID",
			_ when underlying == typeof(byte[]) => "BYTEA",
			_ when underlying == typeof(DateOnly) => "DATE",
			_ when underlying == typeof(TimeOnly) => "TIME",
			_ => throw new NotSupportedException($"Type {clrType.Name} is not supported"),
		};
	}

	/// <inheritdoc />
	public override string GetIdentitySelect(string idCol) => "lastval() as " + idCol;

	/// <inheritdoc />
	public override string ConcatOperator => "||";

	/// <inheritdoc />
	public override string TrueLiteral => "TRUE";

	/// <inheritdoc />
	public override string FalseLiteral => "FALSE";

	/// <inheritdoc />
	public override string UnicodePrefix => "";

	/// <inheritdoc />
	public override string LenFunction => "LENGTH";

	/// <inheritdoc />
	public override string IsNullFunction => "coalesce";

	/// <inheritdoc />
	public override void AppendDatePartOpen(StringBuilder sb, string part)
	{
		sb.Append($"EXTRACT({part} FROM ");
	}

	/// <inheritdoc />
	public override void AppendDateAdd(StringBuilder sb, string part, string amountSql, string sourceSql)
	{
		var pgPart = part switch
		{
			"year" => "years",
			"month" => "months",
			"day" => "days",
			"hour" => "hours",
			"minute" => "mins",
			"second" => "secs",
			_ => throw new NotSupportedException($"Date part '{part}' is not supported for DATEADD in PostgreSQL"),
		};
		sb.Append($"({sourceSql} + make_interval({pgPart} => {amountSql}))");
	}

	/// <inheritdoc />
	public override void AppendTrimOpen(StringBuilder sb)
	{
		sb.Append("TRIM(");
	}

	/// <inheritdoc />
	public override void AppendTrimClose(StringBuilder sb)
	{
		sb.Append(")");
	}

	/// <inheritdoc />
	public override string FormatSkip(string skip) => $"OFFSET {skip}";

	/// <inheritdoc />
	public override string FormatTake(string take) => $"LIMIT {take}";

	/// <inheritdoc />
	public override void AppendPaginationParams(StringBuilder sb, string skipParamExpr, string takeParamExpr)
	{
		// PostgreSQL: LIMIT first, then OFFSET
		if (takeParamExpr is not null)
			sb.AppendLine(FormatTake(takeParamExpr));
		if (skipParamExpr is not null)
			sb.AppendLine(FormatSkip(skipParamExpr));
	}

	/// <inheritdoc />
	public override string Now() => "now()";

	/// <inheritdoc />
	public override string UtcNow() => "now() AT TIME ZONE 'UTC'";

	/// <inheritdoc />
	public override string SysNow() => "now()";

	/// <inheritdoc />
	public override string SysUtcNow() => "now() AT TIME ZONE 'UTC'";

	/// <inheritdoc />
	public override string NewId() => "gen_random_uuid()";

	/// <inheritdoc />
	public override string GetIdentityColumnSuffix() => "GENERATED ALWAYS AS IDENTITY PRIMARY KEY";

	/// <inheritdoc />
	public override void AppendCreateTable(StringBuilder sb, string tableName, string columnDefs)
	{
		sb.Append($"CREATE TABLE IF NOT EXISTS {QuoteIdentifier(tableName)} ({columnDefs})");
	}

	/// <inheritdoc />
	public override void AppendDropTable(StringBuilder sb, string tableName)
	{
		sb.Append($"DROP TABLE IF EXISTS {QuoteIdentifier(tableName)}");
	}

	/// <inheritdoc />
	public override void AppendPagination(StringBuilder sb, long? skip, long? take, bool hasOrderBy)
	{
		if (!skip.HasValue && !take.HasValue)
			return;

		if (take.HasValue)
			sb.Append($" LIMIT {take.Value}");

		if (skip.HasValue)
			sb.Append($" OFFSET {skip.Value}");
	}

	/// <inheritdoc />
	public override string GetColumnDefinition(Type clrType, bool isNullable, int maxLength = 0, int precision = 0, int scale = 0)
	{
		var underlying = clrType.GetUnderlyingType() ?? clrType;

		string typeName;

		if (underlying == typeof(string))
			typeName = maxLength > 0 ? $"VARCHAR({maxLength})" : "TEXT";
		else if (underlying == typeof(byte[]))
			typeName = "BYTEA";
		else if (underlying == typeof(decimal) && precision > 0)
			typeName = $"NUMERIC({precision},{scale})";
		else if ((underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset)) && precision > 0)
			typeName = $"{GetSqlTypeName(clrType)}({precision})";
		else
			typeName = GetSqlTypeName(clrType);

		return $"{typeName} {(isNullable ? "NULL" : "NOT NULL")}";
	}

	/// <inheritdoc />
	public override async Task<IReadOnlyList<DbColumnInfo>> ReadDbSchemaAsync(
		DbConnection connection,
		string tableSchema = null,
		CancellationToken cancellationToken = default)
	{
		tableSchema ??= "public";

		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"
SELECT table_name, column_name, data_type, is_nullable, character_maximum_length, numeric_precision, numeric_scale,
       is_generated <> 'NEVER' AS is_computed
FROM information_schema.columns
WHERE table_schema = @schema
ORDER BY table_name, ordinal_position";

		var param = cmd.CreateParameter();
		param.ParameterName = "@schema";
		param.Value = tableSchema;
		cmd.Parameters.Add(param);

		var result = new List<DbColumnInfo>();

		using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

		while (await reader.ReadAsync(cancellationToken))
		{
			result.Add(new DbColumnInfo(
				TableName: reader.GetString(0),
				ColumnName: reader.GetString(1),
				DataType: reader.GetString(2),
				IsNullable: reader.GetString(3).EqualsIgnoreCase("YES"),
				MaxLength: reader.IsDBNull(4) ? null : reader.GetInt32(4),
				NumericPrecision: reader.IsDBNull(5) ? null : reader.GetValue(5).To<int?>(),
				NumericScale: reader.IsDBNull(6) ? null : reader.GetValue(6).To<int?>(),
				IsComputed: !reader.IsDBNull(7) && reader.GetBoolean(7)
			));
		}

		return result;
	}

	/// <inheritdoc />
	public override void AppendAlterColumn(StringBuilder sb, string tableName, string columnName, Type clrType, bool isNullable, int maxLength = 0, int precision = 0, int scale = 0)
	{
		var underlying = clrType.GetUnderlyingType() ?? clrType;

		string typeName;

		if (underlying == typeof(string))
			typeName = maxLength > 0 ? $"VARCHAR({maxLength})" : "TEXT";
		else if (underlying == typeof(byte[]))
			typeName = "BYTEA";
		else if (underlying == typeof(decimal) && precision > 0)
			typeName = $"NUMERIC({precision},{scale})";
		else
			typeName = GetSqlTypeName(clrType);

		var qt = QuoteIdentifier(tableName);
		var qc = QuoteIdentifier(columnName);

		// PostgreSQL requires separate statements for type and nullability changes
		sb.Append($"ALTER TABLE {qt} ALTER COLUMN {qc} SET DATA TYPE {typeName}; ");
		sb.Append($"ALTER TABLE {qt} ALTER COLUMN {qc} {(isNullable ? "DROP NOT NULL" : "SET NOT NULL")}");
	}

	/// <inheritdoc />
	public override string NormalizeDbType(string dbTypeName)
	{
		return dbTypeName.Trim().ToUpperInvariant() switch
		{
			"CHARACTER VARYING" or "VARCHAR" => "TEXT",
			"TEXT" => "TEXT",
			"INTEGER" or "INT" => "INTEGER",
			"BIGINT" => "BIGINT",
			"SMALLINT" => "SMALLINT",
			"BOOLEAN" or "BIT" => "BOOLEAN",
			"NUMERIC" or "DECIMAL" => "NUMERIC",
			"DOUBLE PRECISION" or "FLOAT" => "DOUBLE PRECISION",
			"REAL" => "REAL",
			"DATE" => "DATE",
			"TIME" or "TIME WITHOUT TIME ZONE" => "TIME",
			"TIMESTAMP" or "TIMESTAMP WITHOUT TIME ZONE" => "TIMESTAMP",
			"TIMESTAMPTZ" or "TIMESTAMP WITH TIME ZONE" => "TIMESTAMPTZ",
			"UUID" or "UNIQUEIDENTIFIER" => "UUID",
			"BYTEA" or "VARBINARY" => "BYTEA",
			var other => other,
		};
	}

	/// <inheritdoc />
	public override void AppendUpsert(StringBuilder sb, string tableName, string[] allColumns, string[] keyColumns)
	{
		var nonKeys = allColumns.Except(keyColumns).ToArray();
		var quotedTable = QuoteIdentifier(tableName);
		var insertCols = allColumns.Select(QuoteIdentifier).JoinCommaSpace();
		var insertParams = allColumns.Select(c => ParameterPrefix + c).JoinCommaSpace();
		var conflictCols = keyColumns.Select(QuoteIdentifier).JoinCommaSpace();

		sb.Append($"INSERT INTO {quotedTable} ({insertCols}) VALUES ({insertParams}) ON CONFLICT ({conflictCols})");

		if (nonKeys.Length == 0)
			sb.Append(" DO NOTHING");
		else
			sb.Append($" DO UPDATE SET {nonKeys.Select(c => $"{QuoteIdentifier(c)} = EXCLUDED.{QuoteIdentifier(c)}").JoinCommaSpace()}");
	}
}
