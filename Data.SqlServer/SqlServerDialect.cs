namespace Ecng.Data;

using System;
using System.Data.Common;
using System.Linq;
using System.Text;

using Ecng.Common;

/// <summary>
/// SQL Server dialect implementation.
/// </summary>
public class SqlServerDialect : SqlDialectBase
{
	/// <summary>
	/// Singleton instance.
	/// </summary>
	public static readonly SqlServerDialect Instance = new();

	private SqlServerDialect() { }

	/// <summary>
	/// Registers the SQL Server dialect and provider factory with <see cref="DatabaseProviderRegistry"/>.
	/// </summary>
	/// <param name="factory">The <see cref="DbProviderFactory"/> for SQL Server.</param>
	public static void Register(DbProviderFactory factory)
	{
		DatabaseProviderRegistry.Register(DatabaseProviderRegistry.SqlServer, factory);
		DatabaseProviderRegistry.RegisterDialect(DatabaseProviderRegistry.SqlServer, Instance);
	}

	/// <inheritdoc />
	public override int MaxParameters => 2000; // SQL Server limit is 2100, use 2000 for safety

	/// <inheritdoc />
	public override string ParameterPrefix => "@";

	/// <inheritdoc />
	public override string QuoteIdentifier(string identifier)
		=> $"[{identifier}]";

	/// <inheritdoc />
	public override string GetSqlTypeName(Type clrType)
	{
		var underlying = clrType.GetUnderlyingType() ?? clrType;

		return underlying switch
		{
			_ when underlying == typeof(int) => "INT",
			_ when underlying == typeof(long) => "BIGINT",
			_ when underlying == typeof(short) => "SMALLINT",
			_ when underlying == typeof(byte) => "TINYINT",
			_ when underlying == typeof(bool) => "BIT",
			_ when underlying == typeof(decimal) => "DECIMAL(18,8)",
			_ when underlying == typeof(double) => "FLOAT",
			_ when underlying == typeof(float) => "REAL",
			_ when underlying == typeof(string) => "NVARCHAR(MAX)",
			_ when underlying == typeof(DateTime) => "DATETIME2",
			_ when underlying == typeof(DateTimeOffset) => "DATETIMEOFFSET",
			_ when underlying == typeof(TimeSpan) => "BIGINT", // stored as ticks
			_ when underlying == typeof(Guid) => "UNIQUEIDENTIFIER",
			_ when underlying == typeof(byte[]) => "VARBINARY(MAX)",
			_ => throw new NotSupportedException($"Type {clrType.Name} is not supported"),
		};
	}

	/// <inheritdoc />
	public override string GetColumnDefinition(Type clrType, bool isNullable, int maxLength = 0)
	{
		var underlying = clrType.GetUnderlyingType() ?? clrType;

		string typeName;

		if (underlying == typeof(string))
			typeName = maxLength > 0 ? $"NVARCHAR({maxLength})" : "NVARCHAR(MAX)";
		else if (underlying == typeof(byte[]))
			typeName = maxLength > 0 ? $"VARBINARY({maxLength})" : "VARBINARY(MAX)";
		else
			typeName = GetSqlTypeName(clrType);

		return $"{typeName} {(isNullable ? "NULL" : "NOT NULL")}";
	}

	/// <inheritdoc />
	public override string GetIdentitySelect(string idCol) => "scope_identity() as " + idCol;

	/// <inheritdoc />
	public override string FormatSkip(string skip) => $"offset {skip} rows";

	/// <inheritdoc />
	public override string FormatTake(string take) => $"fetch next {take} rows only";

	/// <inheritdoc />
	public override string Now() => "getDate()";

	/// <inheritdoc />
	public override string UtcNow() => "getUtcDate()";

	/// <inheritdoc />
	public override string SysNow() => "sysDateTimeOffset()";

	/// <inheritdoc />
	public override string SysUtcNow() => "sysUtcDateTime()";

	/// <inheritdoc />
	public override string NewId() => "newId()";

	/// <inheritdoc />
	public override string GetIdentityColumnSuffix() => "IDENTITY(1,1) PRIMARY KEY";

	/// <inheritdoc />
	public override void AppendCreateTable(StringBuilder sb, string tableName, string columnDefs)
	{
		sb.Append($"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}') CREATE TABLE {QuoteIdentifier(tableName)} ({columnDefs})");
	}

	/// <inheritdoc />
	public override void AppendDropTable(StringBuilder sb, string tableName)
	{
		sb.Append($"IF OBJECT_ID('{tableName}', 'U') IS NOT NULL DROP TABLE {QuoteIdentifier(tableName)}");
	}

	/// <inheritdoc />
	public override void AppendPagination(StringBuilder sb, long? skip, long? take, bool hasOrderBy)
	{
		if (!skip.HasValue && !take.HasValue)
			return;

		if (!hasOrderBy)
			sb.Append(" ORDER BY (SELECT NULL)");

		sb.Append($" OFFSET {skip ?? 0} ROWS");

		if (take.HasValue)
			sb.Append($" FETCH NEXT {take.Value} ROWS ONLY");
	}

	/// <inheritdoc />
	public override void AppendUpsert(StringBuilder sb, string tableName, string[] allColumns, string[] keyColumns)
	{
		var nonKeys = allColumns.Except(keyColumns).ToArray();
		var quotedTable = QuoteIdentifier(tableName);

		var matchCondition = keyColumns.Select(k => $"target.{QuoteIdentifier(k)} = source.{QuoteIdentifier(k)}").Join(" AND ");
		var sourceValues = allColumns.Select(c => $"{ParameterPrefix}{c} AS {QuoteIdentifier(c)}").JoinCommaSpace();
		var insertCols = allColumns.Select(QuoteIdentifier).JoinCommaSpace();
		var insertVals = allColumns.Select(c => $"source.{QuoteIdentifier(c)}").JoinCommaSpace();

		sb.Append($"MERGE {quotedTable} AS target USING (SELECT {sourceValues}) AS source ON ({matchCondition}) ");

		if (nonKeys.Length > 0)
			sb.Append($"WHEN MATCHED THEN UPDATE SET {nonKeys.Select(c => $"{QuoteIdentifier(c)} = source.{QuoteIdentifier(c)}").JoinCommaSpace()} ");

		sb.Append($"WHEN NOT MATCHED THEN INSERT ({insertCols}) VALUES ({insertVals});");
	}
}
