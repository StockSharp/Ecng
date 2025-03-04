namespace Ecng.Common;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents the method for comparing two parameter values.
/// </summary>
public enum ComparisonOperator
{
	/// <summary>
	/// Indicates that the parameter values are equal.
	/// </summary>
	[Display(Name = "=")]
	Equal,

	/// <summary>
	/// Indicates that the parameter values are not equal.
	/// </summary>
	[Display(Name = "!=")]
	NotEqual,

	/// <summary>
	/// Indicates that the left parameter value is strictly greater than the right parameter value.
	/// </summary>
	[Display(Name = ">")]
	Greater,

	/// <summary>
	/// Indicates that the left parameter value is greater than or equal to the right parameter value.
	/// </summary>
	[Display(Name = ">=")]
	GreaterOrEqual,

	/// <summary>
	/// Indicates that the left parameter value is strictly less than the right parameter value.
	/// </summary>
	[Display(Name = "<")]
	Less,

	/// <summary>
	/// Indicates that the left parameter value is less than or equal to the right parameter value.
	/// </summary>
	[Display(Name = "<=")]
	LessOrEqual,

	/// <summary>
	/// Indicates that the left parameter value can be any value.
	/// </summary>
	[Display(Name = "Any")]
	Any,

	/// <summary>
	/// Indicates that the left parameter value is contained within the right parameter value.
	/// </summary>
	[Display(Name = "IN")]
	In,
}
