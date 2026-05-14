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

	/// <summary>
	/// Entity declares a <c>[Index]</c> (or <c>[Unique]</c>) column but the
	/// live database has no matching single-column index. Backfilled via
	/// <c>CREATE INDEX</c> by <see cref="SchemaMigrator.GenerateMigrationSql"/>.
	/// </summary>
	MissingIndex,

	/// <summary>
	/// Database has an index that no entity column declares. Informational
	/// only — never auto-dropped (mirrors <see cref="ExtraColumn"/> and
	/// <see cref="ExtraForeignKey"/>). Primary-key and unique-constraint
	/// backing indexes are filtered out before reaching this diff so they
	/// don't surface as false positives.
	/// </summary>
	ExtraIndex,
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
	/// <param name="dbIndexes">
	/// Indexes read via <see cref="ISqlDialect.ReadDbIndexesAsync"/>. When
	/// supplied, missing/extra single-column indexes are surfaced as
	/// <see cref="SchemaDiffKind.MissingIndex"/> /
	/// <see cref="SchemaDiffKind.ExtraIndex"/> diffs. Pass <see langword="null"/>
	/// (the default) to skip index comparison and preserve the pre-index behaviour.
	/// </param>
	/// <returns>List of differences found.</returns>
	public static IReadOnlyList<SchemaDiff> Compare(
		IEnumerable<Schema> entities,
		IReadOnlyList<DbColumnInfo> dbColumns,
		ISqlDialect dialect,
		bool skipComputed,
		IReadOnlyList<DbForeignKeyInfo> dbForeignKeys = null,
		IReadOnlyList<DbIndexInfo> dbIndexes = null)
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

		if (dbIndexes is not null)
			AppendIndexDiffs(entities, dbIndexes, dbTables, diffs);

		return diffs;
	}

	/// <summary>
	/// Reads columns, foreign keys and indexes via <paramref name="dialect"/>
	/// in one shot and forwards to <see cref="Compare(IEnumerable{Schema},IReadOnlyList{DbColumnInfo},ISqlDialect,bool,IReadOnlyList{DbForeignKeyInfo},IReadOnlyList{DbIndexInfo})"/>.
	/// Convenience for callers that want the full FK + index-aware comparison
	/// without orchestrating three metadata reads themselves.
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
		var dbIndexes = await dialect.ReadDbIndexesAsync(connection, tableSchema, cancellationToken);

		return Compare(snapshot, dbColumns, dialect, skipComputed, dbForeignKeys, dbIndexes);
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

	/// <summary>
	/// Emits CREATE INDEX statements declared on <paramref name="schema"/>.
	/// One column may carry several <c>[Index]</c> attributes (its own
	/// single-column index plus participation in any number of named
	/// composites), so each <see cref="SchemaColumnIndex"/> entry across
	/// every column drives a separate index. Entries sharing
	/// <see cref="SchemaColumnIndex.Name"/> are grouped into one composite
	/// ordered by <see cref="SchemaColumnIndex.Order"/>; entries with
	/// <see langword="null"/> <see cref="SchemaColumnIndex.Name"/> each get
	/// their own single-column index named <c>IX_{Table}_{Column}</c>.
	/// </summary>
	private static void EmitCreateIndexesForTable(ISqlDialect dialect, StringBuilder sb, Schema schema, string tableName)
	{
		// Flatten (column, index-participation) pairs so we can group across
		// columns by composite name regardless of which column declared
		// which slot. Each [Index] attribute on a property becomes one
		// participation; columns with no [Index] but a non-Identity
		// [Unique] (legacy single-column unique) still emit via the
		// IsUnique fallback below.
		var participations = schema.Columns
			.SelectMany(c => c.Indexes.Select(ix => (Column: c, Index: ix)))
			.ToArray();

		// Single-column path — one CREATE INDEX per (column, null-named)
		// participation. Naming sticks to IX_{Table}_{Column} so existing
		// monitoring / DBA scripts keyed off that pattern keep working.
		foreach (var (col, ix) in participations)
		{
			if (ix.Name is not null)
				continue;

			dialect.AppendCreateIndex(sb,
				indexName: $"IX_{tableName}_{col.Name}",
				tableName: tableName,
				columnName: col.Name,
				unique: col.IsUnique);
			sb.AppendLine(";");
		}

		// Legacy fallback for hand-crafted schemas that flag IsIndex /
		// IsUnique without populating the Indexes participation list
		// (most reflection-built schemas go through CollectIndexes and
		// fill Indexes, but tests and a few production callers construct
		// SchemaColumn manually). Emit a single-column IX_{Table}_{Column}
		// for them.
		foreach (var col in schema.Columns)
		{
			if (col.Indexes.Count > 0)
				continue;

			if (!col.IsIndex && !col.IsUnique)
				continue;

			dialect.AppendCreateIndex(sb,
				indexName: $"IX_{tableName}_{col.Name}",
				tableName: tableName,
				columnName: col.Name,
				unique: col.IsUnique);
			sb.AppendLine(";");
		}

		// Composite path — one CREATE INDEX per distinct Name, columns
		// ordered by SchemaColumnIndex.Order across every column that
		// declares membership in that name. Uniqueness is taken from any
		// participating column flagged IsUnique (callers can mark the
		// composite UNIQUE by giving one of its members [Unique(Name = ...)]).
		var composites = participations
			.Where(p => p.Index.Name is not null)
			.GroupBy(p => p.Index.Name, StringComparer.Ordinal);

		foreach (var group in composites)
		{
			var ordered = group
				.OrderBy(p => p.Index.Order)
				.ThenBy(p => p.Column.Name, StringComparer.Ordinal)
				.ToArray();
			var unique = ordered.Any(p => p.Column.IsUnique);

			var cols = ordered.Select(p => dialect.QuoteIdentifier(p.Column.Name)).JoinCommaSpace();

			sb.Append(unique ? "CREATE UNIQUE INDEX " : "CREATE INDEX ");
			sb.Append(dialect.QuoteIdentifier(group.Key));
			sb.Append(" ON ");
			sb.Append(dialect.QuoteIdentifier(tableName));
			sb.Append(" (");
			sb.Append(cols);
			sb.AppendLine(");");
		}
	}

	private static void AppendIndexDiffs(
		IEnumerable<Schema> entities,
		IReadOnlyList<DbIndexInfo> dbIndexes,
		IReadOnlyDictionary<string, Dictionary<string, DbColumnInfo>> dbTables,
		List<SchemaDiff> diffs)
	{
		// Group DB rows by (Table, IndexName) so a composite shows up as one
		// entry whose columns are ordered by ColumnOrdinal. PK backing
		// indexes are filtered out — they live with the identity column.
		var dbByName = dbIndexes
			.Where(ix => !ix.IsPrimaryKey)
			.GroupBy(ix => (ix.TableName, ix.IndexName), TableIndexNameComparer.Instance)
			.ToDictionary(
				g => g.Key,
				g => new DbIndexShape(
					Columns: g.OrderBy(x => x.ColumnOrdinal).Select(x => x.ColumnName).ToArray(),
					IsUnique: g.First().IsUnique),
				TableIndexNameComparer.Instance);

		// Collect every entity-declared index keyed by (Table, ResolvedName)
		// where ResolvedName is the explicit composite name when set,
		// otherwise the IX_{Table}_{Column} fallback for single-column.
		var declaredByName = new Dictionary<(string Table, string Name), ExpectedIndexShape>(TableIndexNameComparer.Instance);

		void AddDeclared(string tableName, string indexName, string columnName, int order, bool unique)
		{
			var key = (tableName, indexName);
			if (declaredByName.TryGetValue(key, out var existing))
			{
				existing.Columns.Add((order, columnName));
				if (unique)
					existing.IsUnique = true;
			}
			else
			{
				declaredByName[key] = new ExpectedIndexShape
				{
					Columns = [(order, columnName)],
					IsUnique = unique,
				};
			}
		}

		foreach (var schema in entities)
		{
			if (schema.IsView)
				continue;

			// MissingTable already emits CREATE INDEX inline with CREATE TABLE,
			// so a table that's missing entirely is handled in the MissingTable
			// branch — skip it here.
			if (!dbTables.ContainsKey(schema.TableName))
				continue;

			foreach (var col in schema.Columns)
			{
				// Identity column is backed by the PK index automatically.
				if (col == schema.Identity)
					continue;

				if (col.Indexes.Count == 0 && col.IsUnique)
				{
					// Legacy [Unique] without explicit [Index] — emit a
					// standalone unique index named IX_{Table}_{Column}.
					AddDeclared(schema.TableName, $"IX_{schema.TableName}_{col.Name}", col.Name, 0, unique: true);
					continue;
				}

				foreach (var ix in col.Indexes)
				{
					var name = ix.Name ?? $"IX_{schema.TableName}_{col.Name}";
					AddDeclared(schema.TableName, name, col.Name, ix.Order, col.IsUnique);
				}
			}
		}

		// Compare: declared vs DB by (Table, IndexName).
		foreach (var kv in declaredByName)
		{
			var (table, name) = kv.Key;
			var expected = kv.Value;

			var expectedCols = expected.Columns
				.OrderBy(c => c.Order)
				.Select(c => c.ColumnName)
				.ToArray();

			if (dbByName.TryGetValue(kv.Key, out var actual))
			{
				// Same name — verify column shape + uniqueness match.
				var matches = actual.Columns.SequenceEqual(expectedCols, StringComparer.OrdinalIgnoreCase)
					&& actual.IsUnique == expected.IsUnique;

				if (!matches)
				{
					diffs.Add(new(table, name, SchemaDiffKind.MissingIndex,
						$"({string.Join(", ", expectedCols)}){(expected.IsUnique ? " UNIQUE" : "")}",
						$"({string.Join(", ", actual.Columns)}){(actual.IsUnique ? " UNIQUE" : "")}"));
				}
				// Mark as seen so ExtraIndex pass below skips it.
				dbByName.Remove(kv.Key);
			}
			else
			{
				diffs.Add(new(table, name, SchemaDiffKind.MissingIndex,
					$"({string.Join(", ", expectedCols)}){(expected.IsUnique ? " UNIQUE" : "")}",
					"missing"));
			}
		}

		// Anything left in dbByName is an index that exists in DB but no
		// entity declares — informational only, not auto-dropped. Only
		// surface ExtraIndex on tables the caller actually asked about
		// (i.e. tables present in the `entities` argument); indexes on
		// unrelated tables (other fixtures' leftovers, unrelated app
		// schemas sharing the DB) would otherwise bubble through callers
		// that pass a narrow schemas list.
		var entityTableNames = new HashSet<string>(
			entities.Where(s => !s.IsView).Select(s => s.TableName),
			StringComparer.OrdinalIgnoreCase);

		foreach (var kv in dbByName)
		{
			var (table, name) = kv.Key;
			if (!entityTableNames.Contains(table))
				continue;

			var actual = kv.Value;

			diffs.Add(new(table, name, SchemaDiffKind.ExtraIndex,
				string.Empty,
				$"({string.Join(", ", actual.Columns)}){(actual.IsUnique ? " UNIQUE" : "")}"));
		}
	}

	/// <summary>
	/// Emits one CREATE INDEX for the index identified by
	/// <paramref name="indexName"/> on <paramref name="schema"/>. Used by
	/// the MissingIndex diff branch: the diff carries only the index name
	/// (composite name or IX_{Table}_{Column} fallback), and the schema
	/// supplies the participating columns / uniqueness via
	/// <see cref="SchemaColumn.Indexes"/> participations or the legacy
	/// IsUnique fallback.
	/// </summary>
	private static void EmitIndexByName(ISqlDialect dialect, StringBuilder sb, Schema schema, string tableName, string indexName)
	{
		// Composite participation match — columns where any
		// SchemaColumnIndex.Name (resolved) equals indexName.
		var participating = schema.Columns
			.SelectMany(c => c.Indexes
				.Where(ix => string.Equals(ix.Name ?? $"IX_{tableName}_{c.Name}", indexName, StringComparison.OrdinalIgnoreCase))
				.Select(ix => (Column: c, Order: ix.Order)))
			.OrderBy(p => p.Order)
			.ThenBy(p => p.Column.Name, StringComparer.Ordinal)
			.ToArray();

		if (participating.Length == 0)
		{
			// No [Index] participation matched — must be a legacy [Unique]
			// without explicit [Index]. Find the column whose
			// IX_{Table}_{Column} fallback matches the requested name.
			var legacy = schema.Columns.FirstOrDefault(c =>
				c.IsUnique
				&& c.Indexes.Count == 0
				&& string.Equals($"IX_{tableName}_{c.Name}", indexName, StringComparison.OrdinalIgnoreCase));

			if (legacy is null)
				return;

			dialect.AppendCreateIndex(sb,
				indexName: indexName,
				tableName: tableName,
				columnName: legacy.Name,
				unique: true);
			sb.AppendLine(";");
			return;
		}

		var unique = participating.Any(p => p.Column.IsUnique);

		if (participating.Length == 1)
		{
			dialect.AppendCreateIndex(sb,
				indexName: indexName,
				tableName: tableName,
				columnName: participating[0].Column.Name,
				unique: unique);
			sb.AppendLine(";");
			return;
		}

		// Composite — emit inline rather than via dialect.AppendCreateIndex
		// (which is single-column). The CREATE INDEX syntax is identical
		// across SqlServer / Postgres / SQLite up to the quoting style,
		// which dialect.QuoteIdentifier handles.
		var cols = participating.Select(p => dialect.QuoteIdentifier(p.Column.Name)).JoinCommaSpace();

		sb.Append(unique ? "CREATE UNIQUE INDEX " : "CREATE INDEX ");
		sb.Append(dialect.QuoteIdentifier(indexName));
		sb.Append(" ON ");
		sb.Append(dialect.QuoteIdentifier(tableName));
		sb.Append(" (");
		sb.Append(cols);
		sb.AppendLine(");");
	}

	private sealed class ExpectedIndexShape
	{
		public List<(int Order, string ColumnName)> Columns { get; init; } = [];
		public bool IsUnique { get; set; }
	}

	private sealed record DbIndexShape(IReadOnlyList<string> Columns, bool IsUnique);

	private sealed class TableIndexNameComparer : IEqualityComparer<(string Table, string Name)>
	{
		public static readonly TableIndexNameComparer Instance = new();

		public bool Equals((string Table, string Name) x, (string Table, string Name) y)
			=> string.Equals(x.Table, y.Table, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

		public int GetHashCode((string Table, string Name) obj)
			=> HashCode.Combine(obj.Table?.ToLowerInvariant(), obj.Name?.ToLowerInvariant());
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

					EmitCreateIndexesForTable(dialect, sb, schema, diff.TableName);

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

				case SchemaDiffKind.MissingIndex:
				{
					if (!schemaMap.TryGetValue(diff.TableName, out var schema))
						break;

					// diff.ColumnName carries the index name as resolved by
					// AppendIndexDiffs — either an explicit composite name
					// or the IX_{Table}_{Column} fallback. Walk the schema
					// to recover the participating columns and uniqueness.
					EmitIndexByName(dialect, sb, schema, diff.TableName, diff.ColumnName);
					break;
				}

				case SchemaDiffKind.ExtraIndex:
					// extra indexes are informational only, not auto-dropped
					sb.AppendLine($"-- Extra index: {dialect.QuoteIdentifier(diff.TableName)}.{dialect.QuoteIdentifier(diff.ColumnName)} {diff.Actual}");
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
