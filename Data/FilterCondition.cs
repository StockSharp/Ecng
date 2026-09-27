namespace Ecng.Data;

/// <summary>
/// Represents a filter condition for database queries.
/// </summary>
/// <param name="Column">Column name.</param>
/// <param name="Operator">Comparison operator.</param>
/// <param name="Value">Value to compare against.</param>
public sealed record FilterCondition(string Column, ComparisonOperator Operator, object Value);

/// <summary>
/// Represents an order-by condition for database queries.
/// </summary>
/// <param name="Column">Column name.</param>
/// <param name="ColumnType">CLR type of the column's values; a dialect that stores decimals as text needs it to sort them as numbers.</param>
/// <param name="Descending">True for descending order, false for ascending.</param>
public sealed record OrderByCondition(string Column, Type ColumnType, bool Descending = false)
{
	/// <summary>
	/// CLR type of the column's values.
	/// </summary>
	public Type ColumnType { get; init; } = ColumnType ?? throw new ArgumentNullException(nameof(ColumnType));
}
