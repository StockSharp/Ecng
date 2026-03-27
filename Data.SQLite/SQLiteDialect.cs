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
/// SQLite dialect implementation.
/// </summary>
public class SQLiteDialect : SqlDialectBase
{
	/// <summary>
	/// Singleton instance.
	/// </summary>
	public static readonly SQLiteDialect Instance = new();

	private SQLiteDialect() { }

	/// <summary>
	/// Registers the SQLite dialect and provider factory with <see cref="DatabaseProviderRegistry"/>.
	/// </summary>
	/// <param name="factory">The <see cref="DbProviderFactory"/> for SQLite.</param>
	public static void Register(DbProviderFactory factory)
	{
		DatabaseProviderRegistry.Register(DatabaseProviderRegistry.SQLite, factory);
		DatabaseProviderRegistry.RegisterDialect(DatabaseProviderRegistry.SQLite, Instance);
	}

	/// <inheritdoc />
	public override int MaxParameters => 900; // SQLite default limit is 999, use 900 for safety

	/// <inheritdoc />
	public override string ParameterPrefix => "@";

	/// <inheritdoc />
	public override string QuoteIdentifier(string identifier)
		=> $"\"{identifier}\"";

	/// <inheritdoc />
	public override string GetSqlTypeName(Type clrType)
	{
		var underlying = clrType.GetUnderlyingType() ?? clrType;

		// SQLite has dynamic typing, but we use affinity types
		return underlying switch
		{
			_ when underlying == typeof(int) => "INTEGER",
			_ when underlying == typeof(long) => "INTEGER",
			_ when underlying == typeof(short) => "INTEGER",
			_ when underlying == typeof(byte) => "INTEGER",
			_ when underlying == typeof(bool) => "INTEGER",
			_ when underlying == typeof(decimal) => "REAL",
			_ when underlying == typeof(double) => "REAL",
			_ when underlying == typeof(float) => "REAL",
			_ when underlying == typeof(string) => "TEXT",
			_ when underlying == typeof(DateTime) => "TEXT", // ISO8601 format
			_ when underlying == typeof(DateTimeOffset) => "TEXT",
			_ when underlying == typeof(TimeSpan) => "INTEGER", // stored as ticks
			_ when underlying == typeof(Guid) => "TEXT",
			_ when underlying == typeof(byte[]) => "BLOB",
			_ when underlying == typeof(DateOnly) => "TEXT", // ISO8601 date format
			_ when underlying == typeof(TimeOnly) => "TEXT", // ISO8601 time format
			_ => throw new NotSupportedException($"Type {clrType.Name} is not supported"),
		};
	}

	/// <inheritdoc />
	public override string GetIdentitySelect(string idCol) => "last_insert_rowid() as " + idCol;

	/// <inheritdoc />
	public override string ConcatOperator => "||";

	/// <inheritdoc />
	public override string UnicodePrefix => "";

	/// <inheritdoc />
	public override string LenFunction => "LENGTH";

	/// <inheritdoc />
	public override string IsNullFunction => "coalesce";

	/// <inheritdoc />
	public override void AppendDatePartOpen(StringBuilder sb, string part)
	{
		// SQLite doesn't have EXTRACT or DATEPART natively.
		// Use strftime format specifiers mapped from standard part names.
		var fmt = part switch
		{
			"year" => "%Y",
			"month" => "%m",
			"day" => "%d",
			"hour" => "%H",
			"minute" => "%M",
			"second" => "%S",
			"dayofyear" => "%j",
			_ => throw new NotSupportedException($"Date part '{part}' is not supported in SQLite"),
		};
		sb.Append($"CAST(strftime('{fmt}',");
	}

	/// <inheritdoc />
	public override void AppendDatePartClose(StringBuilder sb)
	{
		sb.Append(") AS INTEGER)");
	}

	/// <inheritdoc />
	public override void AppendDateAdd(StringBuilder sb, string part, string amountSql, string sourceSql)
	{
		var sqlitePart = part switch
		{
			"year" => "years",
			"month" => "months",
			"day" => "days",
			"hour" => "hours",
			"minute" => "minutes",
			"second" => "seconds",
			_ => throw new NotSupportedException($"Date part '{part}' is not supported for DATEADD in SQLite"),
		};
		sb.Append($"datetime({sourceSql}, ({amountSql}) || ' {sqlitePart}')");
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
		// SQLite: LIMIT first (required for OFFSET), then OFFSET
		if (takeParamExpr is not null)
			sb.AppendLine(FormatTake(takeParamExpr));
		else if (skipParamExpr is not null)
			sb.AppendLine("LIMIT -1"); // SQLite requires LIMIT when using OFFSET
		if (skipParamExpr is not null)
			sb.AppendLine(FormatSkip(skipParamExpr));
	}

	/// <inheritdoc />
	public override string Now() => "datetime('now', 'localtime')";

	/// <inheritdoc />
	public override string UtcNow() => "datetime('now')";

	/// <inheritdoc />
	public override string SysNow() => "datetime('now', 'localtime')";

	/// <inheritdoc />
	public override string SysUtcNow() => "datetime('now')";

	/// <inheritdoc />
	public override string NewId() => "lower(hex(randomblob(16)))";

	/// <inheritdoc />
	public override string GetIdentityColumnSuffix() => "PRIMARY KEY AUTOINCREMENT";

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
		else if (skip.HasValue)
			sb.Append(" LIMIT -1");

		if (skip.HasValue)
			sb.Append($" OFFSET {skip.Value}");
	}

	/// <inheritdoc />
	public override async Task<IReadOnlyList<DbColumnInfo>> ReadDbSchemaAsync(
		DbConnection connection,
		string tableSchema = null,
		CancellationToken cancellationToken = default)
	{
		var result = new List<DbColumnInfo>();

		// get all table names
		var tables = new List<string>();

		using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";

			using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
				tables.Add(reader.GetString(0));
		}

		// read columns per table via pragma
		foreach (var table in tables)
		{
			using var cmd = connection.CreateCommand();
			cmd.CommandText = $"PRAGMA table_xinfo({QuoteIdentifier(table)})";

			using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				// table_xinfo columns: cid, name, type, notnull, dflt_value, pk, hidden
				var hidden = reader.GetInt32(6);
				result.Add(new DbColumnInfo(
					TableName: table,
					ColumnName: reader.GetString(1),
					DataType: reader.GetString(2),
					IsNullable: reader.GetInt32(3) == 0,
					MaxLength: null,
					NumericPrecision: null,
					NumericScale: null,
					IsComputed: hidden is 2 or 3
				));
			}
		}

		return result;
	}

	/// <inheritdoc />
	public override string NormalizeDbType(string dbTypeName)
	{
		return dbTypeName.Trim().ToUpperInvariant() switch
		{
			"INTEGER" or "INT" or "BIGINT" or "SMALLINT" or "TINYINT" => "INTEGER",
			"REAL" or "FLOAT" or "DOUBLE" or "DOUBLE PRECISION" or "NUMERIC" or "DECIMAL" => "REAL",
			"TEXT" or "VARCHAR" or "NVARCHAR" or "CHAR" or "NCHAR" or "CLOB" => "TEXT",
			"BLOB" or "VARBINARY" or "BINARY" or "BYTEA" => "BLOB",
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
			sb.Append($" DO UPDATE SET {nonKeys.Select(c => $"{QuoteIdentifier(c)} = excluded.{QuoteIdentifier(c)}").JoinCommaSpace()}");
	}
}
