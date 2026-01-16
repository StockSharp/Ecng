namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Linq;

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

	/// <inheritdoc />
	public override string ParameterPrefix => "@";

	/// <inheritdoc />
	public override string QuoteIdentifier(string identifier)
		=> $"[{identifier}]";

	/// <inheritdoc />
	public override string GetSqlTypeName(Type clrType)
	{
		var underlying = Nullable.GetUnderlyingType(clrType) ?? clrType;

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
	public override string GenerateCreateTable(string tableName, IDictionary<string, Type> columns)
	{
		var quotedName = QuoteIdentifier(tableName);
		var columnDefs = BuildColumnDefinitions(columns);

		return $@"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}')
CREATE TABLE {quotedName} ({columnDefs})";
	}

	/// <inheritdoc />
	public override string GenerateDropTable(string tableName)
	{
		var quotedName = QuoteIdentifier(tableName);
		return $"IF OBJECT_ID('{tableName}', 'U') IS NOT NULL DROP TABLE {quotedName}";
	}

	/// <inheritdoc />
	public override string GenerateSelect(string tableName, string whereClause, string orderByClause, long? skip, long? take)
	{
		var sql = $"SELECT * FROM {QuoteIdentifier(tableName)}";

		if (!whereClause.IsEmpty())
			sql += $" WHERE {whereClause}";

		if (!orderByClause.IsEmpty())
			sql += $" ORDER BY {orderByClause}";

		// SQL Server 2012+ pagination
		if (skip.HasValue || take.HasValue)
		{
			if (orderByClause.IsEmpty())
				sql += " ORDER BY (SELECT NULL)";

			sql += $" OFFSET {skip ?? 0} ROWS";

			if (take.HasValue)
				sql += $" FETCH NEXT {take.Value} ROWS ONLY";
		}

		return sql;
	}

	/// <inheritdoc />
	public override string GenerateUpsert(string tableName, IEnumerable<string> columns, IEnumerable<string> keyColumns)
	{
		var cols = columns.ToArray();
		var keys = keyColumns.ToArray();
		var nonKeys = cols.Except(keys).ToArray();

		var quotedTable = QuoteIdentifier(tableName);

		// Build MERGE statement
		var matchCondition = keys.Select(k => $"target.{QuoteIdentifier(k)} = source.{QuoteIdentifier(k)}").JoinAnd();
		var sourceValues = cols.Select(c => $"{ParameterPrefix}{c} AS {QuoteIdentifier(c)}").JoinComma();
		var insertCols = cols.Select(QuoteIdentifier).JoinComma();
		var insertVals = cols.Select(c => $"source.{QuoteIdentifier(c)}").JoinComma();

		var sql = $@"MERGE {quotedTable} AS target
USING (SELECT {sourceValues}) AS source
ON ({matchCondition})
WHEN MATCHED THEN UPDATE SET {nonKeys.Select(c => $"{QuoteIdentifier(c)} = source.{QuoteIdentifier(c)}").JoinComma()}
WHEN NOT MATCHED THEN INSERT ({insertCols}) VALUES ({insertVals});";

		return sql;
	}
}
