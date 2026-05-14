namespace Ecng.Data;

/// <summary>
/// Represents a single index discovered in a live database via
/// <see cref="ISqlDialect.ReadDbIndexesAsync"/>. Composite indexes are
/// surfaced as multiple rows — one per indexed column with a shared
/// <see cref="IndexName"/> and ascending <see cref="ColumnOrdinal"/>.
/// Primary-key, unique-constraint, and foreign-key backing indexes are
/// included so the schema migrator can correctly skip them when comparing
/// against entity-declared <c>[Index]</c> / <c>[Unique]</c> attributes.
/// </summary>
/// <param name="IndexName">Name of the index as stored by the database.</param>
/// <param name="TableName">Name of the table the index belongs to.</param>
/// <param name="ColumnName">Name of the indexed column.</param>
/// <param name="ColumnOrdinal">1-based position of the column inside the index. Composite indexes report each member with its key position.</param>
/// <param name="IsUnique">True when the index enforces uniqueness.</param>
/// <param name="IsPrimaryKey">True when the index backs the table's primary key.</param>
public record DbIndexInfo(
	string IndexName,
	string TableName,
	string ColumnName,
	int ColumnOrdinal,
	bool IsUnique,
	bool IsPrimaryKey);
