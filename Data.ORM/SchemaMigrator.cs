namespace Ecng.Serialization;

using System.Text;

using Ecng.Common;
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

	/// <summary>Column precision or scale differs between entity and database.</summary>
	PrecisionMismatch,

	/// <summary>
	/// Entity declares a <c>[RelationSingle]</c> column but the live database
	/// has no matching foreign-key constraint. Backfilled via
	/// <c>ALTER TABLE … ADD CONSTRAINT</c>.
	/// </summary>
	MissingForeignKey,

	/// <summary>
	/// Database has a foreign-key constraint that no entity column declares.
	/// Informational only — never auto-dropped (mirrors <see cref="ExtraColumn"/>).
	/// </summary>
	ExtraForeignKey,
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
	/// <param name="dbColumns">Database columns read via <see cref="ISqlDialect.ReadDbSchemaAsync"/>.</param>
	/// <param name="dialect">SQL dialect for type name resolution.</param>
	/// <param name="skipComputed">Skip computed (calculated) database columns.</param>
	/// <param name="dbForeignKeys">
	/// Foreign keys read via <see cref="ISqlDialect.ReadDbForeignKeysAsync"/>. When
	/// supplied, missing/extra FK constraints are surfaced as
	/// <see cref="SchemaDiffKind.MissingForeignKey"/> /
	/// <see cref="SchemaDiffKind.ExtraForeignKey"/> diffs. Pass <see langword="null"/>
	/// (the default) to skip FK comparison and preserve the pre-FK behaviour.
	/// </param>
	/// <returns>List of differences found.</returns>
	public static IReadOnlyList<SchemaDiff> Compare(
		IEnumerable<Schema> entities,
		IReadOnlyList<DbColumnInfo> dbColumns,
		ISqlDialect dialect,
		bool skipComputed,
		IReadOnlyList<DbForeignKeyInfo> dbForeignKeys = null)
	{
		var diffs = new List<SchemaDiff>();

		// group DB columns by table
		var filtered = skipComputed ? dbColumns.Where(c => !c.IsComputed) : dbColumns;
		var dbTables = filtered
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

				// compare max length. int.MaxValue (ColumnAttribute.Max) is the
				// explicit "unbounded" sentinel; SQL Server's sys.columns reports
				// max_length == -1 for NVARCHAR(MAX)/VARBINARY(MAX). Treat the two
				// as equivalent so an entity that explicitly declares its column
				// as Max doesn't show a perpetual MaxLengthMismatch diff.
				var entityMaxIsUnbounded = col.MaxLength == int.MaxValue;
				var dbMaxIsUnbounded = dbCol.MaxLength == -1;
				if (col.MaxLength > 0 && !(entityMaxIsUnbounded && dbMaxIsUnbounded) && dbCol.MaxLength is not null && col.MaxLength != dbCol.MaxLength)
				{
					diffs.Add(new(schema.TableName, col.Name, SchemaDiffKind.MaxLengthMismatch,
						col.MaxLength.ToString(), dbCol.MaxLength.Value.ToString()));
				}

				// compare precision/scale
				if (col.Precision > 0 && dbCol.NumericPrecision is not null && col.Precision != dbCol.NumericPrecision)
				{
					diffs.Add(new(schema.TableName, col.Name, SchemaDiffKind.PrecisionMismatch,
						$"({col.Precision},{col.Scale})", $"({dbCol.NumericPrecision},{dbCol.NumericScale ?? 0})"));
				}
				else if (col.Scale > 0 && dbCol.NumericScale is not null && col.Scale != dbCol.NumericScale)
				{
					diffs.Add(new(schema.TableName, col.Name, SchemaDiffKind.PrecisionMismatch,
						$"({col.Precision},{col.Scale})", $"({dbCol.NumericPrecision ?? 0},{dbCol.NumericScale})"));
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

		if (dbForeignKeys is not null)
			AppendForeignKeyDiffs(entities, dbColumns, dbForeignKeys, dbTables, diffs);

		return diffs;
	}

	/// <summary>
	/// Reads columns and foreign keys via <paramref name="dialect"/> in one
	/// shot and forwards to <see cref="Compare(IEnumerable{Schema},IReadOnlyList{DbColumnInfo},ISqlDialect,bool,IReadOnlyList{DbForeignKeyInfo})"/>.
	/// Convenience for callers that want the full FK-aware comparison without
	/// orchestrating two metadata reads themselves.
	/// </summary>
	public static async Task<IReadOnlyList<SchemaDiff>> CompareAsync(
		IEnumerable<Schema> entities,
		DbConnection connection,
		ISqlDialect dialect,
		bool skipComputed,
		string tableSchema = null,
		CancellationToken cancellationToken = default)
	{
		if (entities is null)	throw new ArgumentNullException(nameof(entities));
		if (connection is null)	throw new ArgumentNullException(nameof(connection));
		if (dialect is null)	throw new ArgumentNullException(nameof(dialect));

		var snapshot = entities as IReadOnlyList<Schema> ?? [.. entities];

		var dbColumns = await dialect.ReadDbSchemaAsync(connection, tableSchema, cancellationToken);
		var dbForeignKeys = await dialect.ReadDbForeignKeysAsync(connection, tableSchema, cancellationToken);

		return Compare(snapshot, dbColumns, dialect, skipComputed, dbForeignKeys);
	}

	private static void AppendForeignKeyDiffs(
		IEnumerable<Schema> entities,
		IReadOnlyList<DbColumnInfo> dbColumns,
		IReadOnlyList<DbForeignKeyInfo> dbForeignKeys,
		IReadOnlyDictionary<string, Dictionary<string, DbColumnInfo>> dbTables,
		List<SchemaDiff> diffs)
	{
		var dbFkByPair = new Dictionary<(string Table, string Column), DbForeignKeyInfo>(
			new TableColumnComparer());

		foreach (var fk in dbForeignKeys)
			dbFkByPair[(fk.TableName, fk.ColumnName)] = fk;

		var entityFkPairs = new HashSet<(string Table, string Column)>(new TableColumnComparer());

		foreach (var schema in entities)
		{
			if (schema.IsView)
				continue;

			// MissingTable already creates the table with inline FKs — skip
			// it here so we don't double-emit.
			if (!dbTables.ContainsKey(schema.TableName))
				continue;

			foreach (var col in schema.AllColumns)
			{
				if (col.ReferencedEntityType is null)
					continue;

				var refSchema = SchemaRegistry.Get(col.ReferencedEntityType);
				var refCol = refSchema.Identity?.Name ?? "Id";

				entityFkPairs.Add((schema.TableName, col.Name));

				if (!dbFkByPair.TryGetValue((schema.TableName, col.Name), out var existing) ||
					!existing.RefTableName.EqualsIgnoreCase(refSchema.TableName) ||
					!existing.RefColumnName.EqualsIgnoreCase(refCol))
				{
					diffs.Add(new(schema.TableName, col.Name, SchemaDiffKind.MissingForeignKey,
						$"{refSchema.TableName}.{refCol}",
						existing is null ? "missing" : $"{existing.RefTableName}.{existing.RefColumnName}"));
				}
			}
		}

		foreach (var fk in dbForeignKeys)
		{
			if (entityFkPairs.Contains((fk.TableName, fk.ColumnName)))
				continue;

			diffs.Add(new(fk.TableName, fk.ColumnName, SchemaDiffKind.ExtraForeignKey,
				string.Empty,
				$"{fk.RefTableName}.{fk.RefColumnName} ({fk.ConstraintName})"));
		}
	}

	private sealed class TableColumnComparer : IEqualityComparer<(string Table, string Column)>
	{
		public bool Equals((string Table, string Column) x, (string Table, string Column) y)
			=> string.Equals(x.Table, y.Table, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.Column, y.Column, StringComparison.OrdinalIgnoreCase);

		public int GetHashCode((string Table, string Column) obj)
			=> HashCode.Combine(
				obj.Table?.ToLowerInvariant(),
				obj.Column?.ToLowerInvariant());
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

		foreach (var schema in schemaMap.Values)
		{
			foreach (var col in schema.Columns)
			{
				if (col.ClrType == typeof(string) && (col.IsUnique || col.IsIndex) && col.MaxLength <= 0)
					throw new InvalidOperationException($"Indexed string column '{schema.TableName}.{col.Name}' requires [MaxLength].");
			}
		}

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
						var cd = dialect.GetColumnDefinition(col.ClrType, col.IsNullable, col.MaxLength, col.Precision, col.Scale);
						colDefs.Add($"{dialect.QuoteIdentifier(col.Name)} {cd}");
					}

					// inline FOREIGN KEY constraints for columns marked as FK
					foreach (var col in schema.Columns)
					{
						if (col.ReferencedEntityType is null)
							continue;

						var refSchema = SchemaRegistry.Get(col.ReferencedEntityType);
						var refCol = refSchema.Identity?.Name ?? "Id";
						colDefs.Add(dialect.GetForeignKeyConstraint(diff.TableName, col.Name, refSchema.TableName, refCol));
					}

					dialect.AppendCreateTable(sb, diff.TableName, colDefs.JoinCommaSpace());
					sb.AppendLine(";");

					// generate CREATE INDEX for indexed columns
					foreach (var col in schema.Columns)
					{
						if (!col.IsUnique && !col.IsIndex)
							continue;

						dialect.AppendCreateIndex(sb,
							indexName: $"IX_{diff.TableName}_{col.Name}",
							tableName: diff.TableName,
							columnName: col.Name,
							unique: col.IsUnique);
						sb.AppendLine(";");
					}

					break;
				}

				case SchemaDiffKind.MissingColumn:
				{
					if (!schemaMap.TryGetValue(diff.TableName, out var schema))
						break;

					var col = schema.TryGetColumn(diff.ColumnName);
					if (col is null)
						break;

					if (!col.IsNullable)
					{
						var batch = dialect.BatchSeparator;

						// 3-step: add as NULL → fill default → alter to NOT NULL
						var nullableDef = dialect.GetColumnDefinition(col.ClrType, isNullable: true, col.MaxLength, col.Precision, col.Scale);
						dialect.AppendAddColumn(sb, diff.TableName, diff.ColumnName, nullableDef);
						sb.AppendLine(";");

						if (!batch.IsEmpty())
							sb.AppendLine(batch);

						var defaultVal = dialect.GetDefaultLiteral(col.ClrType);
						dialect.AppendUpdateWhereNull(sb, diff.TableName, diff.ColumnName, defaultVal);
						sb.AppendLine();

						if (!batch.IsEmpty())
							sb.AppendLine(batch);

						dialect.AppendAlterColumn(sb, diff.TableName, diff.ColumnName, col.ClrType, col.IsNullable, col.MaxLength, col.Precision, col.Scale);
						sb.AppendLine(";");
					}
					else
					{
						var colDef = dialect.GetColumnDefinition(col.ClrType, col.IsNullable, col.MaxLength, col.Precision, col.Scale);
						dialect.AppendAddColumn(sb, diff.TableName, diff.ColumnName, colDef);
						sb.AppendLine(";");
					}

					// if the new column is a foreign key, append ALTER TABLE ADD CONSTRAINT
					if (col.ReferencedEntityType is not null)
					{
						var refSchema = SchemaRegistry.Get(col.ReferencedEntityType);
						var refCol = refSchema.Identity?.Name ?? "Id";
						dialect.AppendAddForeignKey(sb, diff.TableName, col.Name, refSchema.TableName, refCol);
						sb.AppendLine(";");
					}

					break;
				}

				case SchemaDiffKind.TypeMismatch:
				case SchemaDiffKind.NullabilityMismatch:
				case SchemaDiffKind.MaxLengthMismatch:
				case SchemaDiffKind.PrecisionMismatch:
				{
					if (!schemaMap.TryGetValue(diff.TableName, out var schema))
						break;

					var col = schema.TryGetColumn(diff.ColumnName);
					if (col is null)
						break;

					dialect.AppendAlterColumn(sb, diff.TableName, diff.ColumnName, col.ClrType, col.IsNullable, col.MaxLength, col.Precision, col.Scale);
					sb.AppendLine(";");
					break;
				}

				case SchemaDiffKind.MissingForeignKey:
				{
					if (!schemaMap.TryGetValue(diff.TableName, out var schema))
						break;

					var col = schema.TryGetColumn(diff.ColumnName);
					if (col?.ReferencedEntityType is null)
						break;

					var refSchema = SchemaRegistry.Get(col.ReferencedEntityType);
					var refCol = refSchema.Identity?.Name ?? "Id";
					dialect.AppendAddForeignKey(sb, diff.TableName, col.Name, refSchema.TableName, refCol);
					sb.AppendLine(";");
					break;
				}

				case SchemaDiffKind.ExtraForeignKey:
					// extra FKs are informational only, not auto-dropped (mirrors ExtraColumn)
					sb.AppendLine($"-- Extra foreign key: {dialect.QuoteIdentifier(diff.TableName)}.{dialect.QuoteIdentifier(diff.ColumnName)} -> {diff.Actual}");
					break;

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
	/// <param name="dialect">Dialect whose <see cref="ISqlDialect.BatchSeparator"/> is used to split the SQL into batches before execution. If the separator is empty, the SQL is sent as a single command.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async Task ApplyAsync(DbConnection connection, string migrationSql, ISqlDialect dialect, CancellationToken cancellationToken = default)
	{
		if (connection is null)
			throw new ArgumentNullException(nameof(connection));

		if (dialect is null)
			throw new ArgumentNullException(nameof(dialect));

		if (migrationSql.IsEmpty())
			return;

		var separator = dialect.BatchSeparator;

		if (separator.IsEmpty())
		{
			using var cmd = connection.CreateCommand();
			cmd.CommandText = migrationSql;
			await cmd.ExecuteNonQueryAsync(cancellationToken).NoWait();
			return;
		}

		foreach (var batch in SplitByBatchSeparator(migrationSql, separator))
		{
			using var cmd = connection.CreateCommand();
			cmd.CommandText = batch;
			await cmd.ExecuteNonQueryAsync(cancellationToken).NoWait();
		}
	}

	private static IEnumerable<string> SplitByBatchSeparator(string sql, string separator)
	{
		var current = new StringBuilder();

		foreach (var line in sql.Split('\n'))
		{
			if (line.Trim().EqualsIgnoreCase(separator))
			{
				var batch = current.ToString().Trim();
				if (!batch.IsEmpty())
					yield return batch;
				current.Clear();
			}
			else
			{
				current.AppendLine(line.TrimEnd('\r'));
			}
		}

		var tail = current.ToString().Trim();
		if (!tail.IsEmpty())
			yield return tail;
	}

	private static string NormalizeSqlType(string sqlType)
	{
		// strip parentheses for comparison: "NVARCHAR(MAX)" -> "NVARCHAR"
		var paren = sqlType.IndexOf('(');
		return paren > 0 ? sqlType[..paren].Trim() : sqlType.Trim();
	}

}
