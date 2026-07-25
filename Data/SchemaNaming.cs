namespace Ecng.Data;

/// <summary>
/// The naming convention for database constraints and indexes, in one place so the DDL
/// that creates them and the comparison that audits them can never drift apart.
/// </summary>
/// <remarks>
/// A name is built from the table and the participating columns, which keeps it unique
/// inside a schema (SQL Server and PostgreSQL both require constraint names to be unique
/// per schema, not merely per table) and readable in an execution plan or a lock report.
/// Note that databases silently truncate identifiers past their own limit — 63 bytes on
/// PostgreSQL, 128 on SQL Server — so a name built from very long table and column names
/// may not round-trip exactly.
/// </remarks>
public static class SchemaNaming
{
	/// <summary>
	/// Prefix of a primary-key constraint name.
	/// </summary>
	public const string PrimaryKeyPrefix = "PK";

	/// <summary>
	/// Prefix of a foreign-key constraint name.
	/// </summary>
	public const string ForeignKeyPrefix = "FK";

	/// <summary>
	/// Prefix of a non-unique index name.
	/// </summary>
	public const string IndexPrefix = "IX";

	/// <summary>
	/// Prefix of a unique index name, so uniqueness is visible without inspecting the index.
	/// </summary>
	public const string UniqueIndexPrefix = "UX";

	/// <summary>
	/// Builds the primary-key constraint name for <paramref name="tableName"/>
	/// — <c>PK_{Table}</c>.
	/// </summary>
	/// <param name="tableName">Table the primary key belongs to.</param>
	/// <returns>The constraint name.</returns>
	public static string PrimaryKey(string tableName)
		=> $"{PrimaryKeyPrefix}_{tableName}";

	/// <summary>
	/// Builds the foreign-key constraint name — <c>FK_{Table}_{Column}</c>. The name keys
	/// off the referencing column rather than the referenced table, so a table holding two
	/// foreign keys to the same parent still gets two distinct names.
	/// </summary>
	/// <param name="tableName">Table that holds the foreign-key column.</param>
	/// <param name="columnName">The foreign-key column.</param>
	/// <returns>The constraint name.</returns>
	public static string ForeignKey(string tableName, string columnName)
		=> $"{ForeignKeyPrefix}_{tableName}_{columnName}";

	/// <summary>
	/// Builds the index name — <c>IX_{Table}_{Column…}</c>, or <c>UX_…</c> when the index
	/// is unique. Columns appear in key order.
	/// </summary>
	/// <param name="tableName">Table the index belongs to.</param>
	/// <param name="columnNames">Indexed columns, in key order.</param>
	/// <param name="unique">Whether the index enforces uniqueness.</param>
	/// <returns>The index name.</returns>
	public static string Index(string tableName, IEnumerable<string> columnNames, bool unique)
		=> $"{(unique ? UniqueIndexPrefix : IndexPrefix)}_{tableName}_{string.Join("_", columnNames)}";

	/// <summary>
	/// Builds the single-column index name — <c>IX_{Table}_{Column}</c>, or <c>UX_…</c>
	/// when the index is unique.
	/// </summary>
	/// <param name="tableName">Table the index belongs to.</param>
	/// <param name="columnName">The indexed column.</param>
	/// <param name="unique">Whether the index enforces uniqueness.</param>
	/// <returns>The index name.</returns>
	public static string Index(string tableName, string columnName, bool unique)
		=> $"{(unique ? UniqueIndexPrefix : IndexPrefix)}_{tableName}_{columnName}";

	/// <summary>
	/// Determines whether <paramref name="name"/> is an identifier the database generated
	/// for itself rather than one our DDL supplied. Such names cannot be made to follow the
	/// convention, so the naming audit ignores them.
	/// </summary>
	/// <param name="name">Constraint or index name as stored by the database.</param>
	/// <returns><see langword="true"/> when the database owns the name.</returns>
	public static bool IsDatabaseGenerated(string name)
		// SQLite names the index backing a UNIQUE/PRIMARY KEY constraint sqlite_autoindex_*,
		// and reserves the whole sqlite_ prefix for its internal objects.
		=> name.IsEmpty() || name.StartsWithIgnoreCase("sqlite_");
}
