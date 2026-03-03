namespace Ecng.Data;

using System;
using System.Data.Common;
using System.Linq;
using System.Text;

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
	public override string GetIdentitySelect(string idCol) => "last_insert_rowid() as " + idCol;

	/// <inheritdoc />
	public override string FormatSkip(string skip) => $"OFFSET {skip}";

	/// <inheritdoc />
	public override string FormatTake(string take) => $"LIMIT {take}";

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
