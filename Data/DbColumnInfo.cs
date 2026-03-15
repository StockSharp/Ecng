namespace Ecng.Data;

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
