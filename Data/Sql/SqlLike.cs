namespace Ecng.Data.Sql;

/// <summary>
/// Builds LIKE patterns from caller-supplied text. A pattern produced here
/// matches its input literally, provided the predicate declares
/// <see cref="EscapeChar"/> in an ESCAPE clause — the SQL-standard clause
/// understood alike by SQL Server, PostgreSQL, SQLite and MySQL, which is what
/// keeps the escaping free of dialect knowledge.
/// </summary>
public static class SqlLike
{
	/// <summary>
	/// Character declared by the ESCAPE clause of a LIKE predicate.
	/// </summary>
	/// <remarks>
	/// An exclamation mark rather than a backslash: a backslash is itself the
	/// string-literal escape in MySQL, so <c>escape '\'</c> would have to be
	/// written differently per dialect.
	/// </remarks>
	public const char EscapeChar = '!';

	/// <summary>
	/// Escapes the pattern metacharacters in <paramref name="value"/> so that
	/// LIKE matches it as plain text.
	/// </summary>
	/// <param name="value">Text to be matched literally.</param>
	/// <returns>The escaped text.</returns>
	/// <remarks>
	/// Besides the standard <c>%</c> and <c>_</c>, an opening bracket is escaped
	/// too: SQL Server reads <c>[...]</c> as a character class, so an unescaped
	/// <c>[a-z]</c> would match any single letter instead of the typed text. The
	/// other dialects have no bracket syntax and read an escaped bracket as the
	/// plain character, so the result stays portable. A closing bracket needs no
	/// escaping — outside a class it is already literal everywhere.
	/// </remarks>
	public static string EscapeLike(this string value)
	{
		if (value.IsEmpty())
			return value;

		var sb = new StringBuilder(value.Length);

		foreach (var c in value)
		{
			if (c is EscapeChar or '%' or '_' or '[')
				sb.Append(EscapeChar);

			sb.Append(c);
		}

		return sb.ToString();
	}

	/// <summary>
	/// Builds a "contains" pattern matching <paramref name="value"/> literally
	/// anywhere in the compared column.
	/// </summary>
	/// <param name="value">Text to be matched literally.</param>
	/// <returns>The pattern.</returns>
	public static string ToLikeContains(this string value)
		=> $"%{value.EscapeLike()}%";
}
