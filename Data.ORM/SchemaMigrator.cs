namespace Ecng.Serialization;

using System.Text;

using Ecng.Data;

/// <summary>
/// Describes a difference between entity schema and database schema.
/// </summary>
public enum SchemaDiffKind
{
	/// <summary>Table exists in entity schema but not in database.</summary>
	MissingTable,

	/// <summary>Column exists in entity but not in database.</summary>
	MissingColumn,

	/// <summary>Column exists in database but not in entity.</summary>
	ExtraColumn,

	/// <summary>Column SQL type differs between entity and database.</summary>
	TypeMismatch,

	/// <summary>Column nullability differs between entity and database.</summary>
	NullabilityMismatch,

	/// <summary>Column max length differs between entity and database.</summary>
	MaxLengthMismatch,
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
/// Compares entity schemas (from <see cref="SchemaRegistry"/>) with a live database
/// and generates migration DDL.
/// </summary>
public static class SchemaMigrator
{
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
			{
				diffs.Add(new(schema.TableName, string.Empty, SchemaDiffKind.MissingTable, "expected", "missing"));
				continue;
			}

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
				var actualType = dialect.NormalizeDbType(dbCol.DataType);

				if (!expectedType.EqualsIgnoreCase(actualType))
				{
					diffs.Add(new(schema.TableName, col.Name, SchemaDiffKind.TypeMismatch,
						expectedType, actualType));
				}

				// compare max length
				if (col.MaxLength > 0 && dbCol.MaxLength is not null && col.MaxLength != dbCol.MaxLength)
				{
					diffs.Add(new(schema.TableName, col.Name, SchemaDiffKind.MaxLengthMismatch,
						col.MaxLength.ToString(), dbCol.MaxLength.Value.ToString()));
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
				case SchemaDiffKind.MissingTable:
				{
					if (!schemaMap.TryGetValue(diff.TableName, out var schema))
						break;

					var colDefs = new List<string>();

					if (schema.Identity is not null)
					colDefs.Add(schema.Identity.ClrType.IsNumeric()
						? $"{dialect.QuoteIdentifier(schema.Identity.Name)} {dialect.GetSqlTypeName(schema.Identity.ClrType)} {dialect.GetIdentityColumnSuffix()}"
						: $"{dialect.QuoteIdentifier(schema.Identity.Name)} {dialect.GetSqlTypeName(schema.Identity.ClrType)} PRIMARY KEY");

					foreach (var col in schema.Columns)
					{
						var cd = dialect.GetColumnDefinition(col.ClrType, col.IsNullable, col.MaxLength);
						colDefs.Add($"{dialect.QuoteIdentifier(col.Name)} {cd}");
					}

					dialect.AppendCreateTable(sb, diff.TableName, colDefs.JoinCommaSpace());
					sb.AppendLine(";");
					break;
				}

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
				case SchemaDiffKind.MaxLengthMismatch:
				{
					if (!schemaMap.TryGetValue(diff.TableName, out var schema))
						break;

					var col = schema.TryGetColumn(diff.ColumnName);
					if (col is null)
						break;

					dialect.AppendAlterColumn(sb, diff.TableName, diff.ColumnName, col.ClrType, col.IsNullable, col.MaxLength);
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

}
