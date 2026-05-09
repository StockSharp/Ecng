namespace Ecng.Data;

/// <summary>
/// Represents a single foreign-key constraint discovered in a live database
/// via <see cref="ISqlDialect.ReadDbForeignKeysAsync"/>. Compound foreign keys
/// are surfaced as multiple rows — one per referencing/referenced column pair.
/// </summary>
/// <param name="ConstraintName">Name of the <c>FOREIGN KEY</c> constraint as stored by the database.</param>
/// <param name="TableName">Name of the table that holds the FK column.</param>
/// <param name="ColumnName">Name of the column constrained by this FK.</param>
/// <param name="RefTableName">Name of the referenced (parent) table.</param>
/// <param name="RefColumnName">Name of the referenced column on the parent table.</param>
public record DbForeignKeyInfo(
	string ConstraintName,
	string TableName,
	string ColumnName,
	string RefTableName,
	string RefColumnName);
