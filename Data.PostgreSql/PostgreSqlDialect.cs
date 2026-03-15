namespace Ecng.Data;

using System;
using System.Data.Common;
using System.Linq;
using System.Text;

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
			_ => throw new NotSupportedException($"Type {clrType.Name} is not supported"),
		};
	}

	/// <inheritdoc />
	public override string GetIdentitySelect(string idCol) => idCol;

	/// <inheritdoc />
	public override string FormatSkip(string skip) => $"OFFSET {skip}";

	/// <inheritdoc />
	public override string FormatTake(string take) => $"LIMIT {take}";

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
	public override string GetColumnDefinition(Type clrType, bool isNullable, int maxLength = 0)
	{
		var underlying = clrType.GetUnderlyingType() ?? clrType;

		string typeName;

		if (underlying == typeof(string))
			typeName = maxLength > 0 ? $"VARCHAR({maxLength})" : "TEXT";
		else if (underlying == typeof(byte[]))
			typeName = "BYTEA";
		else
			typeName = GetSqlTypeName(clrType);

		return $"{typeName} {(isNullable ? "NULL" : "NOT NULL")}";
	}

	/// <inheritdoc />
	public override void AppendAlterColumn(StringBuilder sb, string tableName, string columnName, string columnDef)
	{
		// PostgreSQL uses SET DATA TYPE and SET/DROP NOT NULL separately
		sb.Append($"ALTER TABLE {QuoteIdentifier(tableName)} ALTER COLUMN {QuoteIdentifier(columnName)} SET DATA TYPE {columnDef}");
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
