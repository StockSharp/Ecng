namespace Ecng.Data.Sql;

using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Provides SQL functions usable from a query.
/// </summary>
/// <remarks>
/// A query written with these is normally translated to SQL, but the same query also runs over a list the ORM
/// keeps in memory (see BulkLoad), where LINQ executes it directly. Each function therefore carries a body
/// that answers as the database would -- pattern matching anchored to the whole value and blind to case, as a
/// case-insensitive collation compares it.
/// </remarks>
public static class SqlFunctions
{
	/// <summary>
	/// SQL LIKE pattern matching operator.
	/// </summary>
	/// <param name="s">Value to compare.</param>
	/// <param name="what">Pattern, where <c>%</c> stands for any run of characters, <c>_</c> for one, and
	/// <c>[...]</c> for a character class.</param>
	/// <returns>Whether the value matches the pattern. A null on either side matches nothing, as a comparison
	/// against NULL leaves the row out.</returns>
	public static bool Like(this string s, string what)
		=> IsLike(s, what, null);

	/// <summary>
	/// SQL LIKE pattern matching operator with an explicit ESCAPE clause, so a
	/// pattern built by <see cref="SqlLike.ToLikeContains"/> matches the caller's
	/// text literally. Use this whenever the pattern comes from user input.
	/// </summary>
	/// <param name="s">Value to compare.</param>
	/// <param name="what">Pattern whose metacharacters are escaped with <see cref="SqlLike.EscapeChar"/>.</param>
	/// <returns>Whether the value matches the pattern.</returns>
	public static bool LikeEscaped(this string s, string what)
		=> IsLike(s, what, SqlLike.EscapeChar);

	/// <summary>
	/// SQL IFNULL (COALESCE) function for strings.
	/// </summary>
	/// <param name="s">Value.</param>
	/// <param name="what">What to answer with when the value is null.</param>
	/// <returns>The value, or <paramref name="what"/> when it is null.</returns>
	public static string IfNull(this string s, string what)
		=> s ?? what;

	/// <summary>
	/// SQL ISNULL (COALESCE) function for generic types.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="s">Value.</param>
	/// <param name="what">What to answer with when the value is null.</param>
	/// <returns>The value, or <paramref name="what"/> when it is null.</returns>
	public static T IsNull<T>(this T s, T what)
		=> s is null ? what : s;

	private static bool IsLike(string value, string pattern, char? escape)
	{
		if (value is null || pattern is null)
			return false;

		return Regex.IsMatch(value, ToRegex(pattern, escape), RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
	}

	private static string ToRegex(string pattern, char? escape)
	{
		var sb = new StringBuilder(pattern.Length * 2);

		sb.Append(@"\A");

		for (var i = 0; i < pattern.Length; i++)
		{
			var c = pattern[i];

			if (escape is char e && c == e && i + 1 < pattern.Length)
			{
				sb.Append(Regex.Escape(pattern[++i].ToString()));
				continue;
			}

			switch (c)
			{
				case '%':
					sb.Append(".*");
					break;
				case '_':
					sb.Append('.');
					break;
				case '[':
				{
					var close = pattern.IndexOf(']', i + 1);

					// An opening bracket with no closing one is not a class -- the database reads it as text.
					if (close < 0)
					{
						sb.Append(Regex.Escape(c.ToString()));
						break;
					}

					sb.Append('[');

					for (var j = i + 1; j < close; j++)
					{
						var inner = pattern[j];

						// A caret opens a negated class only in first position; a dash spells a range unless it
						// sits at either end. Both carry that meaning in a regular expression too, so they are
						// passed through and everything else is written as itself.
						if ((inner == '^' && j == i + 1) || (inner == '-' && j > i + 1 && j < close - 1))
							sb.Append(inner);
						else
							sb.Append(Regex.Escape(inner.ToString()));
					}

					sb.Append(']');

					i = close;
					break;
				}
				default:
					sb.Append(Regex.Escape(c.ToString()));
					break;
			}
		}

		sb.Append(@"\z");

		return sb.ToString();
	}
}
