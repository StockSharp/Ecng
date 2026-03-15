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
			_ when underlying == typeof(DateOnly) => "DATE",
			_ when underlying == typeof(TimeOnly) => "TIME",
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
	public override async Task<IReadOnlyList<DbColumnInfo>> ReadDbSchemaAsync(
		DbConnection connection,
		string tableSchema = null,
		CancellationToken cancellationToken = default)
	{
		tableSchema ??= "dbo";

		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = @schema
ORDER BY TABLE_NAME, ORDINAL_POSITION";

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
				NumericScale: reader.IsDBNull(6) ? null : reader.GetValue(6).To<int?>()
			));
		}

		return result;
	}

	/// <inheritdoc />
	public override string NormalizeDbType(string dbTypeName)
	{
		return dbTypeName.Trim().ToUpperInvariant() switch
		{
			"NVARCHAR" or "VARCHAR" or "NCHAR" or "CHAR" or "NTEXT" or "TEXT" => "NVARCHAR",
			"INT" or "INTEGER" => "INT",
			"BIGINT" => "BIGINT",
			"SMALLINT" => "SMALLINT",
			"TINYINT" => "TINYINT",
			"BIT" or "BOOLEAN" => "BIT",
			"DECIMAL" or "NUMERIC" => "DECIMAL",
			"FLOAT" or "DOUBLE PRECISION" => "FLOAT",
			"REAL" => "REAL",
			"DATE" => "DATE",
			"TIME" => "TIME",
			"DATETIME" or "DATETIME2" => "DATETIME2",
			"DATETIMEOFFSET" => "DATETIMEOFFSET",
			"UNIQUEIDENTIFIER" or "UUID" => "UNIQUEIDENTIFIER",
			"VARBINARY" or "BINARY" or "IMAGE" or "BYTEA" => "VARBINARY",
			var other => other,
		};
	}

	/// <inheritdoc />
	public override void AppendUpdateBy(StringBuilder sb, string tableName, string[] setColumns, string[] whereColumns)
	{
		const string alias = "e";

		sb.AppendLine($"update {QuoteIdentifier(alias)}");
		sb.AppendLine("set");

		for (var i = 0; i < setColumns.Length; i++)
		{
			var comma = i < setColumns.Length - 1 ? "," : "";
			sb.AppendLine($"\t{alias}.{QuoteIdentifier(setColumns[i])} = {ParameterPrefix}{setColumns[i]}{comma}");
		}

		sb.AppendLine($"from {QuoteIdentifier(tableName)} {alias}");
		sb.AppendLine("where");

		for (var i = 0; i < whereColumns.Length; i++)
		{
			if (i > 0)
				sb.Append(" and ");
			sb.Append($"{alias}.{QuoteIdentifier(whereColumns[i])} = {ParameterPrefix}{whereColumns[i]}");
		}
	}

	/// <inheritdoc />
	public override void AppendDeleteBy(StringBuilder sb, string tableName, string[] whereColumns)
	{
		const string alias = "e";

		sb.Append($"delete {alias}");
		sb.AppendLine();
		sb.Append($"from {QuoteIdentifier(tableName)} {alias}");
		sb.AppendLine();
		sb.AppendLine("where");

		for (var i = 0; i < whereColumns.Length; i++)
		{
			if (i > 0)
				sb.Append(" and ");
			sb.Append($"{alias}.{QuoteIdentifier(whereColumns[i])} = {ParameterPrefix}{whereColumns[i]}");
		}
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
