namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Linq;

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
		var underlying = Nullable.GetUnderlyingType(clrType) ?? clrType;

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
			_ => throw new NotSupportedException($"Type {clrType.Name} is not supported"),
		};
	}

	/// <inheritdoc />
	public override string GenerateCreateTable(string tableName, IDictionary<string, Type> columns)
	{
		var quotedName = QuoteIdentifier(tableName);
		var columnDefs = BuildColumnDefinitions(columns);

		return $"CREATE TABLE IF NOT EXISTS {quotedName} ({columnDefs})";
	}

	/// <inheritdoc />
	public override string GenerateDropTable(string tableName)
	{
		var quotedName = QuoteIdentifier(tableName);
		return $"DROP TABLE IF EXISTS {quotedName}";
	}

	/// <inheritdoc />
	public override string GenerateSelect(string tableName, string whereClause, string orderByClause, long? skip, long? take)
	{
		var sql = $"SELECT * FROM {QuoteIdentifier(tableName)}";

		if (!whereClause.IsEmpty())
			sql += $" WHERE {whereClause}";

		if (!orderByClause.IsEmpty())
			sql += $" ORDER BY {orderByClause}";

		// SQLite uses LIMIT/OFFSET
		if (take.HasValue)
			sql += $" LIMIT {take.Value}";
		else if (skip.HasValue)
			sql += " LIMIT -1"; // -1 means no limit in SQLite

		if (skip.HasValue)
			sql += $" OFFSET {skip.Value}";

		return sql;
	}

	/// <inheritdoc />
	public override string GenerateUpsert(string tableName, IEnumerable<string> columns, IEnumerable<string> keyColumns)
	{
		var cols = columns.ToArray();
		var keys = keyColumns.ToArray();
		var nonKeys = cols.Except(keys).ToArray();

		var quotedTable = QuoteIdentifier(tableName);
		var insertCols = cols.Select(QuoteIdentifier).JoinComma();
		var insertParams = cols.Select(c => ParameterPrefix + c).JoinComma();
		var conflictCols = keys.Select(QuoteIdentifier).JoinComma();

		// SQLite uses INSERT ... ON CONFLICT ... DO UPDATE
		var sql = $"INSERT INTO {quotedTable} ({insertCols}) VALUES ({insertParams})";
		sql += $" ON CONFLICT ({conflictCols}) DO UPDATE SET ";
		sql += nonKeys.Select(c => $"{QuoteIdentifier(c)} = excluded.{QuoteIdentifier(c)}").JoinComma();

		return sql;
	}
}
