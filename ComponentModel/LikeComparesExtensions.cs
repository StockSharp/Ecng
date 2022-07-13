namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Common;

	public static class LikeComparesExtensions
	{
		public static bool Like(this string value, string like, LikeCompares? likeCompare)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			if (like.IsEmpty())
				return true;

			return likeCompare switch
			{
				LikeCompares.Contains or null => value.ContainsIgnoreCase(like),
				LikeCompares.StartWith => value.StartsWithIgnoreCase(like),
				LikeCompares.EndWith => value.EndsWithIgnoreCase(like),
				LikeCompares.Equals => value.EqualsIgnoreCase(like),
				LikeCompares.NotContains => !value.ContainsIgnoreCase(like),
				_ => throw new ArgumentOutOfRangeException(nameof(likeCompare)),
			};
		}

		public static string ToExpression(this string like, LikeCompares? likeCompare = default)
		{
			if (like.IsEmpty())
				throw new ArgumentNullException(nameof(like));

			return likeCompare switch
			{
				LikeCompares.Contains or null => $"%{like}%",
				LikeCompares.StartWith => $"{like}%",
				LikeCompares.EndWith => $"%{like}",
				LikeCompares.Equals => like,
				LikeCompares.NotContains => throw new NotSupportedException(),
				_ => throw new ArgumentOutOfRangeException(nameof(likeCompare)),
			};
		}
	}
}