namespace Ecng.ComponentModel;

using System;

using Ecng.Common;

/// <summary>
/// Provides extension methods for comparing string values using a specified comparison operator.
/// </summary>
public static class LikeComparesExtensions
{
	/// <summary>
	/// Determines if the specified string value matches a pattern based on the provided comparison operator.
	/// </summary>
	/// <param name="value">The string value to evaluate.</param>
	/// <param name="like">The pattern to compare against.</param>
	/// <param name="likeCompare">The comparison operator to use for the evaluation.</param>
	/// <returns>
	/// True if the value satisfies the comparison; otherwise, false.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the comparison operator is not supported.</exception>
	public static bool Like(this string value, string like, ComparisonOperator? likeCompare)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		if (like.IsEmpty())
			return true;

		return likeCompare switch
		{
			ComparisonOperator.In or null => value.ContainsIgnoreCase(like),
			ComparisonOperator.Greater or ComparisonOperator.GreaterOrEqual => value.StartsWithIgnoreCase(like),
			ComparisonOperator.Less or ComparisonOperator.LessOrEqual => value.EndsWithIgnoreCase(like),
			ComparisonOperator.Equal => value.EqualsIgnoreCase(like),
			ComparisonOperator.NotEqual => !value.ContainsIgnoreCase(like),
			_ => throw new ArgumentOutOfRangeException(nameof(likeCompare)),
		};
	}

	/// <summary>
	/// Converts a pattern into an expression according to the specified comparison operator.
	/// </summary>
	/// <param name="like">The pattern to convert.</param>
	/// <param name="likeCompare">
	/// The comparison operator that determines how the expression is formed. 
	/// Defaults to ComparisonOperator.In if null.
	/// </param>
	/// <returns>A string expression representing the pattern.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="like"/> is empty.</exception>
	/// <exception cref="NotSupportedException">Thrown when the NotEqual operator is used.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the comparison operator is not supported.</exception>
	public static string ToExpression(this string like, ComparisonOperator? likeCompare = default)
	{
		if (like.IsEmpty())
			throw new ArgumentNullException(nameof(like));

		return likeCompare switch
		{
			ComparisonOperator.In or null => $"%{like}%",
			ComparisonOperator.Greater or ComparisonOperator.GreaterOrEqual => $"{like}%",
			ComparisonOperator.Less or ComparisonOperator.LessOrEqual => $"%{like}",
			ComparisonOperator.Equal => like,
			ComparisonOperator.NotEqual => throw new NotSupportedException(),
			_ => throw new ArgumentOutOfRangeException(nameof(likeCompare)),
		};
	}
}