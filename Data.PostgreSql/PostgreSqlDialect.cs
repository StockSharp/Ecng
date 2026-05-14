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
			// TIMESTAMPTZ for DateTime aligns with the Ecng read path
			// (DatabaseCommandHelper.GetValueEx normalises returned values
			// to Kind=Utc). Writing the same UTC moment we read back into a
			// time-zone-aware column is the semantically honest choice;
			// TIMESTAMP (without TZ) silently dropped the kind under Npgsql 5
			// and got rejected outright under Npgsql 6+.
			_ when underlying == typeof(DateTime) => "TIMESTAMPTZ",
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
	public override void PrepareParameter(DbParameter parameter)
	{
		// Npgsql 6+ refuses to write a Kind=Utc DateTime to a `timestamp
		// without time zone` binding, which is what DbType.DateTime /
		// DbType.DateTime2 resolve to. Re-bind DateTime values as
		// DateTimeOffset so the parameter targets `timestamp with time
		// zone` instead — that matches the schema (DateTime → TIMESTAMPTZ)
		// and the Ecng read path (DatabaseCommandHelper.GetValueEx
		// normalises returned values to Kind=Utc).
		if (parameter.Value is DateTime dt)
		{
			var utc = dt.Kind switch
			{
				DateTimeKind.Utc => dt,
				DateTimeKind.Local => dt.ToUniversalTime(),
				_ => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
			};

			parameter.Value = new DateTimeOffset(utc);
			parameter.DbType = System.Data.DbType.DateTimeOffset;
		}
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
	// PostgreSQL has a native boolean type — `cast(... as bit)` is not a
	// valid coercion from boolean. Skip the cast entirely; the case
	// expression already evaluates to boolean.
	public override string BooleanCastSqlType => null;

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
			"minute" => "minutes",
			"second" => "seconds",
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
	// GENERATED BY DEFAULT (not ALWAYS) so callers can still insert explicit
	// id values when needed (round-tripping fixtures, replicating across
	// environments, importing legacy rows). ALWAYS forbids that and
	// historically conflicted with our own INSERT path.
	public override string GetIdentityColumnSuffix() => "GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY";

	/// <inheritdoc />
	public override void AppendInsertReturningClause(StringBuilder sb, string idColumn)
	{
		// Single-statement identity round-trip. lastval() is correct under
		// a single connection but becomes a foot-gun once a sequence-using
		// trigger or a parallel sequence call sneaks in between INSERT and
		// SELECT lastval(). RETURNING is connection-pool-safe by construction.
		if (!idColumn.IsEmpty())
			sb.Append($" RETURNING {QuoteIdentifier(idColumn)}");
	}

	/// <inheritdoc />
	public override bool SupportsInsertReturning => true;

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
	public override void AppendFallbackOrderBy(StringBuilder sb)
	{
		// LIMIT/OFFSET on a result with no explicit ORDER BY is
		// syntactically valid in PostgreSQL but produces an unspecified
		// row order — emit ORDER BY 1 so paginated reads stay
		// deterministic across repeat calls.
		sb.AppendLine("ORDER BY 1");
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

		// MaxLength == int.MaxValue (ColumnAttribute.Max) is the explicit
		// unbounded sentinel — same encoding as MaxLength == 0, lets entity
		// authors document intent for "yes, intentionally TEXT" columns.
		var isMax = maxLength <= 0 || maxLength == int.MaxValue;
		if (underlying == typeof(string))
			typeName = isMax ? "TEXT" : $"VARCHAR({maxLength})";
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

		// SELECT shape:
		//   table_name, column_name, data_type, is_nullable,
		//   character_maximum_length, numeric_precision, numeric_scale,
		//   is_generated <> 'NEVER'
		// FROM information_schema.columns
		// WHERE table_schema = @schema
		// ORDER BY table_name, ordinal_position
		var sql = new Query()
			.Select()
				.Column("table_name").Comma()
				.Column("column_name").Comma()
				.Column("data_type").Comma()
				.Column("is_nullable").Comma()
				.Column("character_maximum_length").Comma()
				.Column("numeric_precision").Comma()
				.Column("numeric_scale").Comma()
				.Raw("is_generated <> 'NEVER'").NewLine()
			.From().Raw("information_schema.columns").NewLine()
			.Where().NewLine()
				.Column("table_schema").Equal().Param("schema").NewLine()
			.OrderBy().Column("table_name").Comma().Column("ordinal_position")
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
				IsComputed: !reader.IsDBNull(7) && reader.GetBoolean(7)
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
		tableSchema ??= "public";

		// SELECT shape:
		//   rc.constraint_name, kcu.table_name, kcu.column_name,
		//   ccu.table_name, ccu.column_name
		// FROM information_schema.referential_constraints rc
		// JOIN information_schema.key_column_usage kcu
		//   ON kcu.constraint_name = rc.constraint_name AND kcu.constraint_schema = rc.constraint_schema
		// JOIN information_schema.constraint_column_usage ccu
		//   ON ccu.constraint_name = rc.unique_constraint_name AND ccu.constraint_schema = rc.unique_constraint_schema
		// WHERE kcu.constraint_schema = @schema
		// ORDER BY rc.constraint_name, kcu.ordinal_position
		var sql = new Query()
			.Select()
				.Column("rc", "constraint_name").Comma()
				.Column("kcu", "table_name").Comma()
				.Column("kcu", "column_name").Comma()
				.Column("ccu", "table_name").Comma()
				.Column("ccu", "column_name").NewLine()
			.From().Raw("information_schema.referential_constraints rc").NewLine()
			.InnerJoin().Raw("information_schema.key_column_usage kcu").On()
				.Column("kcu", "constraint_name").Equal().Column("rc", "constraint_name")
				.And().Column("kcu", "constraint_schema").Equal().Column("rc", "constraint_schema").NewLine()
			.InnerJoin().Raw("information_schema.constraint_column_usage ccu").On()
				.Column("ccu", "constraint_name").Equal().Column("rc", "unique_constraint_name")
				.And().Column("ccu", "constraint_schema").Equal().Column("rc", "unique_constraint_schema").NewLine()
			.Where().NewLine()
				.Column("kcu", "constraint_schema").Equal().Param("schema").NewLine()
			.OrderBy().Column("rc", "constraint_name").Comma().Column("kcu", "ordinal_position")
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
		tableSchema ??= "public";

		// pg_index.indkey is an oid-vector of column attnums in key order;
		// unnest with WITH ORDINALITY to recover the 1-based position so
		// composite indexes can be reassembled in the right column order.
		//
		// SELECT shape:
		//   c.relname, t.relname, a.attname, k.ord, ix.indisunique, ix.indisprimary
		// FROM pg_index ix
		// JOIN pg_class c ON c.oid = ix.indexrelid
		// JOIN pg_class t ON t.oid = ix.indrelid
		// JOIN pg_namespace n ON n.oid = t.relnamespace
		// JOIN LATERAL unnest(ix.indkey) WITH ORDINALITY AS k(attnum, ord) ON TRUE
		// JOIN pg_attribute a ON a.attrelid = ix.indrelid AND a.attnum = k.attnum
		// WHERE n.nspname = @schema
		// ORDER BY c.relname, k.ord
		var sql =
			"SELECT c.relname, t.relname, a.attname, k.ord::int, ix.indisunique, ix.indisprimary " +
			"FROM pg_index ix " +
			"JOIN pg_class c ON c.oid = ix.indexrelid " +
			"JOIN pg_class t ON t.oid = ix.indrelid " +
			"JOIN pg_namespace n ON n.oid = t.relnamespace " +
			"JOIN LATERAL unnest(ix.indkey) WITH ORDINALITY AS k(attnum, ord) ON TRUE " +
			"JOIN pg_attribute a ON a.attrelid = ix.indrelid AND a.attnum = k.attnum " +
			"WHERE n.nspname = @schema " +
			"ORDER BY c.relname, k.ord";

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
				ColumnOrdinal: reader.GetInt32(3),
				IsUnique: reader.GetBoolean(4),
				IsPrimaryKey: reader.GetBoolean(5)));
		}

		return result;
	}

	/// <inheritdoc />
	public override void AppendAlterColumn(StringBuilder sb, string tableName, string columnName, Type clrType, bool isNullable, int maxLength = 0, int precision = 0, int scale = 0)
	{
		var underlying = clrType.GetUnderlyingType() ?? clrType;

		string typeName;

		// MaxLength == int.MaxValue (ColumnAttribute.Max) is the explicit
		// unbounded sentinel — same encoding as MaxLength == 0, lets entity
		// authors document intent for "yes, intentionally TEXT" columns.
		var isMax = maxLength <= 0 || maxLength == int.MaxValue;
		if (underlying == typeof(string))
			typeName = isMax ? "TEXT" : $"VARCHAR({maxLength})";
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
	/// <remarks>
	/// Length and numeric precision/scale are intentionally not folded into the
	/// canonical name returned here — they are compared separately through
	/// <see cref="DbColumnInfo.MaxLength"/>, <see cref="DbColumnInfo.NumericPrecision"/>
	/// and <see cref="DbColumnInfo.NumericScale"/>. Treat the result as a "type
	/// family" identifier rather than a full type signature.
	/// </remarks>
	public override string NormalizeDbType(string dbTypeName)
	{
		return dbTypeName.Trim().ToUpperInvariant() switch
		{
			"CHARACTER VARYING" or "VARCHAR" => "VARCHAR",
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
