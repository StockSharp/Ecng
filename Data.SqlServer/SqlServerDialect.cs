namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Data.Sql;

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
	public override string BatchSeparator => "GO";

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
	public override string GetColumnDefinition(Type clrType, bool isNullable, int maxLength = 0, int precision = 0, int scale = 0)
	{
		var underlying = clrType.GetUnderlyingType() ?? clrType;

		string typeName;

		// MaxLength == int.MaxValue (ColumnAttribute.Max) is the explicit "MAX"
		// sentinel — same encoding as MaxLength == 0, but lets entity authors
		// document intent ("yes, this column is intentionally unbounded").
		var isMax = maxLength <= 0 || maxLength == int.MaxValue;
		if (underlying == typeof(string))
			typeName = isMax ? "NVARCHAR(MAX)" : $"NVARCHAR({maxLength})";
		else if (underlying == typeof(byte[]))
			typeName = isMax ? "VARBINARY(MAX)" : $"VARBINARY({maxLength})";
		else if (underlying == typeof(decimal) && precision > 0)
			typeName = $"DECIMAL({precision},{scale})";
		else if ((underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset)) && precision > 0)
			typeName = $"{GetSqlTypeName(clrType)}({precision})";
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
	public override void AppendFallbackOrderBy(StringBuilder sb)
	{
		sb.AppendLine("ORDER BY (SELECT NULL)");
	}

	/// <inheritdoc />
	public override void AppendPaginationParams(StringBuilder sb, string skipParamExpr, string takeParamExpr)
	{
		if (skipParamExpr is null && takeParamExpr is null)
			return;

		// SQL Server requires OFFSET before FETCH NEXT
		sb.AppendLine(skipParamExpr is not null ? FormatSkip(skipParamExpr) : "offset 0 rows");

		if (takeParamExpr is not null)
			sb.AppendLine(FormatTake(takeParamExpr));
	}

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
		var literal = tableName.Replace("'", "''");
		sb.Append($"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{literal}') CREATE TABLE {QuoteIdentifier(tableName)} ({columnDefs})");
	}

	/// <inheritdoc />
	public override void AppendDropTable(StringBuilder sb, string tableName)
	{
		var literal = tableName.Replace("'", "''");
		sb.Append($"IF OBJECT_ID('{literal}', 'U') IS NOT NULL DROP TABLE {QuoteIdentifier(tableName)}");
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

		// SELECT shape:
		//   c.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.IS_NULLABLE,
		//   c.CHARACTER_MAXIMUM_LENGTH, c.NUMERIC_PRECISION, c.NUMERIC_SCALE,
		//   COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsComputed')
		// FROM INFORMATION_SCHEMA.COLUMNS c
		// WHERE c.TABLE_SCHEMA = @schema
		// ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION
		var sql = new Query()
			.Select()
				.Column("c", "TABLE_NAME").Comma()
				.Column("c", "COLUMN_NAME").Comma()
				.Column("c", "DATA_TYPE").Comma()
				.Column("c", "IS_NULLABLE").Comma()
				.Column("c", "CHARACTER_MAXIMUM_LENGTH").Comma()
				.Column("c", "NUMERIC_PRECISION").Comma()
				.Column("c", "NUMERIC_SCALE").Comma()
				.Raw("COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsComputed')").NewLine()
			.From().Raw("INFORMATION_SCHEMA.COLUMNS c").NewLine()
			.Where().NewLine()
				.Column("c", "TABLE_SCHEMA").Equal().Param("schema").NewLine()
			.OrderBy().Column("c", "TABLE_NAME").Comma().Column("c", "ORDINAL_POSITION")
			.Render(this);

		using var cmd = connection.CreateCommand();
		cmd.CommandText = sql;

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
				IsComputed: !reader.IsDBNull(7) && reader.GetValue(7).To<int>() == 1
			));
		}

		return result;
	}

	/// <inheritdoc />
	public override async Task<IReadOnlyList<DbForeignKeyInfo>> ReadDbForeignKeysAsync(
		DbConnection connection,
		string tableSchema = null,
		CancellationToken cancellationToken = default)
	{
		tableSchema ??= "dbo";

		// SELECT shape:
		//   fk.name, OBJECT_NAME(fkc.parent_object_id), c1.name,
		//   OBJECT_NAME(fkc.referenced_object_id), c2.name
		// FROM sys.foreign_keys fk
		// JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
		// JOIN sys.columns c1 ON c1.object_id = fkc.parent_object_id AND c1.column_id = fkc.parent_column_id
		// JOIN sys.columns c2 ON c2.object_id = fkc.referenced_object_id AND c2.column_id = fkc.referenced_column_id
		// WHERE SCHEMA_NAME(fk.schema_id) = @schema
		// ORDER BY fk.name, fkc.constraint_column_id
		var sql = new Query()
			.Select()
				.Column("fk", "name").Comma()
				.Raw("OBJECT_NAME(fkc.parent_object_id)").Comma()
				.Column("c1", "name").Comma()
				.Raw("OBJECT_NAME(fkc.referenced_object_id)").Comma()
				.Column("c2", "name").NewLine()
			.From().Raw("sys.foreign_keys fk").NewLine()
			.InnerJoin().Raw("sys.foreign_key_columns fkc").On()
				.Column("fkc", "constraint_object_id").Equal().Column("fk", "object_id").NewLine()
			.InnerJoin().Raw("sys.columns c1").On()
				.Column("c1", "object_id").Equal().Column("fkc", "parent_object_id")
				.And().Column("c1", "column_id").Equal().Column("fkc", "parent_column_id").NewLine()
			.InnerJoin().Raw("sys.columns c2").On()
				.Column("c2", "object_id").Equal().Column("fkc", "referenced_object_id")
				.And().Column("c2", "column_id").Equal().Column("fkc", "referenced_column_id").NewLine()
			.Where().NewLine()
				.Raw("SCHEMA_NAME(fk.schema_id) ").Equal().Param("schema").NewLine()
			.OrderBy().Column("fk", "name").Comma().Column("fkc", "constraint_column_id")
			.Render(this);

		using var cmd = connection.CreateCommand();
		cmd.CommandText = sql;

		var param = cmd.CreateParameter();
		param.ParameterName = "@schema";
		param.Value = tableSchema;
		cmd.Parameters.Add(param);

		var result = new List<DbForeignKeyInfo>();

		using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

		while (await reader.ReadAsync(cancellationToken))
		{
			result.Add(new DbForeignKeyInfo(
				ConstraintName: reader.GetString(0),
				TableName: reader.GetString(1),
				ColumnName: reader.GetString(2),
				RefTableName: reader.GetString(3),
				RefColumnName: reader.GetString(4)));
		}

		return result;
	}

	/// <inheritdoc />
	public override async Task<IReadOnlyList<DbIndexInfo>> ReadDbIndexesAsync(
		DbConnection connection,
		string tableSchema = null,
		CancellationToken cancellationToken = default)
	{
		tableSchema ??= "dbo";

		// SELECT shape:
		//   i.name, OBJECT_NAME(i.object_id), c.name, ic.key_ordinal,
		//   i.is_unique, i.is_primary_key
		// FROM sys.indexes i
		// JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
		// JOIN sys.columns c       ON c.object_id  = ic.object_id AND c.column_id = ic.column_id
		// JOIN sys.tables t        ON t.object_id  = i.object_id
		// WHERE SCHEMA_NAME(t.schema_id) = @schema
		//   AND i.type > 0                  -- skip heaps
		//   AND ic.is_included_column = 0   -- skip INCLUDE columns
		// ORDER BY i.name, ic.key_ordinal
		var sql = new Query()
			.Select()
				.Column("i", "name").Comma()
				.Raw("OBJECT_NAME(i.object_id)").Comma()
				.Column("c", "name").Comma()
				.Column("ic", "key_ordinal").Comma()
				.Column("i", "is_unique").Comma()
				.Column("i", "is_primary_key").NewLine()
			.From().Raw("sys.indexes i").NewLine()
			.InnerJoin().Raw("sys.index_columns ic").On()
				.Column("ic", "object_id").Equal().Column("i", "object_id")
				.And().Column("ic", "index_id").Equal().Column("i", "index_id").NewLine()
			.InnerJoin().Raw("sys.columns c").On()
				.Column("c", "object_id").Equal().Column("ic", "object_id")
				.And().Column("c", "column_id").Equal().Column("ic", "column_id").NewLine()
			.InnerJoin().Raw("sys.tables t").On()
				.Column("t", "object_id").Equal().Column("i", "object_id").NewLine()
			.Where().NewLine()
				.Raw("SCHEMA_NAME(t.schema_id) ").Equal().Param("schema")
				.And().Column("i", "type").More().Raw("0")
				.And().Column("ic", "is_included_column").Equal().Raw("0").NewLine()
			.OrderBy().Column("i", "name").Comma().Column("ic", "key_ordinal")
			.Render(this);

		using var cmd = connection.CreateCommand();
		cmd.CommandText = sql;

		var param = cmd.CreateParameter();
		param.ParameterName = "@schema";
		param.Value = tableSchema;
		cmd.Parameters.Add(param);

		var result = new List<DbIndexInfo>();

		using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

		while (await reader.ReadAsync(cancellationToken))
		{
			result.Add(new DbIndexInfo(
				IndexName: reader.GetString(0),
				TableName: reader.GetString(1),
				ColumnName: reader.GetString(2),
				ColumnOrdinal: reader.GetByte(3),
				IsUnique: reader.GetBoolean(4),
				IsPrimaryKey: reader.GetBoolean(5)));
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
		if (whereColumns.Length == 0)
			throw new InvalidOperationException($"Cannot generate UPDATE for '{tableName}': no key columns specified for WHERE clause.");

		var quotedAlias = QuoteIdentifier("e");

		sb.AppendLine($"update {quotedAlias}");
		sb.AppendLine("set");

		for (var i = 0; i < setColumns.Length; i++)
		{
			var comma = i < setColumns.Length - 1 ? "," : "";
			sb.AppendLine($"\t{quotedAlias}.{QuoteIdentifier(setColumns[i])} = {ParameterPrefix}{setColumns[i]}{comma}");
		}

		sb.AppendLine($"from {QuoteIdentifier(tableName)} {quotedAlias}");
		sb.AppendLine("where");

		for (var i = 0; i < whereColumns.Length; i++)
		{
			if (i > 0)
				sb.Append(" and ");
			sb.Append($"{quotedAlias}.{QuoteIdentifier(whereColumns[i])} = {ParameterPrefix}{whereColumns[i]}");
		}
	}

	/// <inheritdoc />
	public override void AppendDeleteBy(StringBuilder sb, string tableName, string[] whereColumns)
	{
		if (whereColumns.Length == 0)
			throw new InvalidOperationException($"Cannot generate DELETE for '{tableName}': no key columns specified for WHERE clause.");

		var quotedAlias = QuoteIdentifier("e");

		sb.Append($"delete {quotedAlias}");
		sb.AppendLine();
		sb.Append($"from {QuoteIdentifier(tableName)} {quotedAlias}");
		sb.AppendLine();
		sb.AppendLine("where");

		for (var i = 0; i < whereColumns.Length; i++)
		{
			if (i > 0)
				sb.Append(" and ");
			sb.Append($"{quotedAlias}.{QuoteIdentifier(whereColumns[i])} = {ParameterPrefix}{whereColumns[i]}");
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

		sb.Append($"WHEN NOT MATCHED THEN INSERT ({insertCols}) VALUES ({insertVals}) ");

		// OUTPUT lets the caller see whether the row was INSERTed or UPDATEd
		// and pick up the post-merge key values (incl. server-generated
		// identities). Without it MERGE was a black box for callers that
		// expected the same identity round-trip the other dialects provide.
		var keyOutputs = keyColumns.Select(k => $"INSERTED.{QuoteIdentifier(k)}").JoinCommaSpace();
		sb.Append($"OUTPUT $action AS {QuoteIdentifier("MergeAction")}, {keyOutputs};");
	}
}
