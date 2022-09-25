namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Common;

	public static class LikeComparesExtensions
	{
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
}