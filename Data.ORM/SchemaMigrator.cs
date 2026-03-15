namespace Ecng.Serialization;

using System.Text;

using Ecng.Data;

/// <summary>
/// Describes a difference between entity schema and database schema.
/// </summary>
public enum SchemaDiffKind
{
	/// <summary>Column exists in entity but not in database.</summary>
	MissingColumn,

	/// <summary>Column exists in database but not in entity.</summary>
	ExtraColumn,

	/// <summary>Column SQL type differs between entity and database.</summary>
	TypeMismatch,

	/// <summary>Column nullability differs between entity and database.</summary>
	NullabilityMismatch,
}

/// <summary>
/// Represents a single difference between entity schema and database schema.
/// </summary>
/// <param name="TableName">The table name.</param>
/// <param name="ColumnName">The column name.</param>
/// <param name="Kind">The kind of difference.</param>
/// <param name="Expected">Expected value (from entity).</param>
/// <param name="Actual">Actual value (from database).</param>
public record SchemaDiff(string TableName, string ColumnName, SchemaDiffKind Kind, string Expected, string Actual);

/// <summary>
/// Represents column metadata read from a live database.
/// </summary>
/// <param name="TableName">The table name.</param>
/// <param name="ColumnName">The column name.</param>
/// <param name="DataType">SQL data type name (e.g. "nvarchar", "bigint").</param>
/// <param name="IsNullable">Whether the column allows NULLs.</param>
/// <param name="MaxLength">Max character length for string columns, or null.</param>
/// <param name="NumericPrecision">Numeric precision, or null.</param>
/// <param name="NumericScale">Numeric scale, or null.</param>
public record DbColumnInfo(
	string TableName,
	string ColumnName,
	string DataType,
	bool IsNullable,
	int? MaxLength,
	int? NumericPrecision,
	int? NumericScale);

/// <summary>
/// Compares entity schemas (from <see cref="SchemaRegistry"/>) with a live database
/// and generates migration DDL.
/// </summary>
public static class SchemaMigrator
{
	/// <summary>
	/// Reads column metadata from a live database via INFORMATION_SCHEMA.COLUMNS.
	/// </summary>
	/// <param name="connection">Open database connection.</param>
	/// <param name="tableSchema">Schema filter (default "dbo").</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of column metadata from the database.</returns>
	public static async Task<IReadOnlyList<DbColumnInfo>> ReadDbSchemaAsync(
		DbConnection connection,
		string tableSchema = "dbo",
		CancellationToken cancellationToken = default)
	{
		var result = new List<DbColumnInfo>();

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

	/// <summary>
	/// Compares entity schemas with database column metadata and returns differences.
	/// </summary>
	/// <param name="entities">Entity schemas to compare.</param>
	/// <param name="dbColumns">Database columns read via <see cref="ReadDbSchemaAsync"/>.</param>
	/// <param name="dialect">SQL dialect for type name resolution.</param>
	/// <returns>List of differences found.</returns>
	public static IReadOnlyList<SchemaDiff> Compare(
		IEnumerable<Schema> entities,
		IReadOnlyList<DbColumnInfo> dbColumns,
		ISqlDialect dialect)
	{
		var diffs = new List<SchemaDiff>();

		// group DB columns by table
		var dbTables = dbColumns
			.GroupBy(c => c.TableName, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(g => g.Key, g => g.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

		foreach (var schema in entities)
		{
			if (schema.IsView)
				continue;

			if (!dbTables.TryGetValue(schema.TableName, out var dbCols))
				continue; // table doesn't exist in DB — handled separately

			foreach (var col in schema.AllColumns)
			{
				if (!dbCols.TryGetValue(col.Name, out var dbCol))
				{
					diffs.Add(new(schema.TableName, col.Name, SchemaDiffKind.MissingColumn,
						dialect.GetColumnDefinition(col.ClrType, col.IsNullable, col.MaxLength), string.Empty));
					continue;
				}

				// compare nullability
				if (col.IsNullable != dbCol.IsNullable)
				{
					diffs.Add(new(schema.TableName, col.Name, SchemaDiffKind.NullabilityMismatch,
						col.IsNullable ? "NULL" : "NOT NULL",
						dbCol.IsNullable ? "NULL" : "NOT NULL"));
				}

				// compare type
				var expectedType = NormalizeSqlType(dialect.GetSqlTypeName(col.ClrType));
				var actualType = NormalizeDbType(dbCol);

				if (!expectedType.EqualsIgnoreCase(actualType))
				{
					diffs.Add(new(schema.TableName, col.Name, SchemaDiffKind.TypeMismatch,
						expectedType, actualType));
				}

				dbCols.Remove(col.Name);
			}

			// remaining DB columns not in entity
			foreach (var (colName, _) in dbCols)
			{
				// skip identity columns from DB that might use different names
				if (colName.EqualsIgnoreCase("Id"))
					continue;

				diffs.Add(new(schema.TableName, colName, SchemaDiffKind.ExtraColumn,
					string.Empty, "exists in DB"));
			}
		}

		return diffs;
	}

	/// <summary>
	/// Generates migration SQL for the given differences.
	/// </summary>
	/// <param name="dialect">SQL dialect.</param>
	/// <param name="diffs">Differences to generate SQL for.</param>
	/// <param name="schemas">Entity schemas (for column type lookup).</param>
	/// <returns>Migration SQL script.</returns>
	public static string GenerateMigrationSql(
		ISqlDialect dialect,
		IReadOnlyList<SchemaDiff> diffs,
		IEnumerable<Schema> schemas)
	{
		var schemaMap = schemas.ToDictionary(s => s.TableName, StringComparer.OrdinalIgnoreCase);
		var sb = new StringBuilder();

		foreach (var diff in diffs)
		{
			switch (diff.Kind)
			{
				case SchemaDiffKind.MissingColumn:
				{
					if (!schemaMap.TryGetValue(diff.TableName, out var schema))
						break;

					var col = schema.TryGetColumn(diff.ColumnName);
					if (col is null)
						break;

					var colDef = dialect.GetColumnDefinition(col.ClrType, col.IsNullable, col.MaxLength);
					dialect.AppendAddColumn(sb, diff.TableName, diff.ColumnName, colDef);
					sb.AppendLine(";");
					break;
				}

				case SchemaDiffKind.TypeMismatch:
				case SchemaDiffKind.NullabilityMismatch:
				{
					if (!schemaMap.TryGetValue(diff.TableName, out var schema))
						break;

					var col = schema.TryGetColumn(diff.ColumnName);
					if (col is null)
						break;

					var colDef = dialect.GetColumnDefinition(col.ClrType, col.IsNullable, col.MaxLength);
					dialect.AppendAlterColumn(sb, diff.TableName, diff.ColumnName, colDef);
					sb.AppendLine(";");
					break;
				}

				case SchemaDiffKind.ExtraColumn:
					// extra columns are informational only, not auto-dropped
					sb.AppendLine($"-- Extra column: {dialect.QuoteIdentifier(diff.TableName)}.{dialect.QuoteIdentifier(diff.ColumnName)}");
					break;
			}
		}

		return sb.ToString();
	}

	/// <summary>
	/// Applies migration SQL directly to the database.
	/// </summary>
	/// <param name="connection">Open database connection.</param>
	/// <param name="migrationSql">SQL to execute.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async Task ApplyAsync(DbConnection connection, string migrationSql, CancellationToken cancellationToken = default)
	{
		if (migrationSql.IsEmpty())
			return;

		using var cmd = connection.CreateCommand();
		cmd.CommandText = migrationSql;
		await cmd.ExecuteNonQueryAsync(cancellationToken);
	}

	private static string NormalizeSqlType(string sqlType)
	{
		// strip parentheses for comparison: "NVARCHAR(MAX)" -> "NVARCHAR"
		var paren = sqlType.IndexOf('(');
		return paren > 0 ? sqlType[..paren].Trim() : sqlType.Trim();
	}

	private static string NormalizeDbType(DbColumnInfo col)
	{
		return col.DataType.Trim().ToUpperInvariant() switch
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
			"DATETIME" or "DATETIME2" => "DATETIME2",
			"DATETIMEOFFSET" => "DATETIMEOFFSET",
			"UNIQUEIDENTIFIER" or "UUID" => "UNIQUEIDENTIFIER",
			"VARBINARY" or "BINARY" or "IMAGE" or "BYTEA" => "VARBINARY",
			var other => other,
		};
	}
}
