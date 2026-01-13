namespace Ecng.Data;

using Ecng.Common;

/// <summary>
/// Represents a filter condition for database queries.
/// </summary>
public class FilterCondition
{
	/// <summary>
	/// Column name.
	/// </summary>
	public string Column { get; }

	/// <summary>
	/// Comparison operator.
	/// </summary>
	public ComparisonOperator Operator { get; }

	/// <summary>
	/// Value to compare against.
	/// </summary>
	public object Value { get; }

	/// <summary>
	/// Creates a new filter condition.
	/// </summary>
	/// <param name="column">Column name.</param>
	/// <param name="operator">Comparison operator.</param>
	/// <param name="value">Value to compare against.</param>
	public FilterCondition(string column, ComparisonOperator @operator, object value)
	{
		Column = column;
		Operator = @operator;
		Value = value;
	}
}

/// <summary>
/// Represents an order by condition for database queries.
/// </summary>
public class OrderByCondition
{
	/// <summary>
	/// Column name.
	/// </summary>
	public string Column { get; }

	/// <summary>
	/// True for descending order, false for ascending.
	/// </summary>
	public bool Descending { get; }

	/// <summary>
	/// Creates a new order by condition.
	/// </summary>
	/// <param name="column">Column name.</param>
	/// <param name="descending">True for descending order.</param>
	public OrderByCondition(string column, bool descending = false)
	{
		Column = column;
		Descending = descending;
	}
}
