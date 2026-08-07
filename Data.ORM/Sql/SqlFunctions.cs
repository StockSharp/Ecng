namespace Ecng.Data.Sql;

/// <summary>
/// Provides SQL function markers for LINQ-to-SQL translation (not callable at runtime).
/// </summary>
public static class SqlFunctions
{
	/// <summary>
	/// SQL LIKE pattern matching operator.
	/// </summary>
	public static bool Like(this string s, string what)
		=> throw new NotSupportedException();

	/// <summary>
	/// SQL LIKE pattern matching operator with an explicit ESCAPE clause, so a
	/// pattern built by <see cref="SqlLike.ToLikeContains"/> matches the caller's
	/// text literally. Use this whenever the pattern comes from user input.
	/// </summary>
	public static bool LikeEscaped(this string s, string what)
		=> throw new NotSupportedException();

	/// <summary>
	/// SQL IFNULL (COALESCE) function for strings.
	/// </summary>
	public static string IfNull(this string s, string what)
		=> throw new NotSupportedException();

	/// <summary>
	/// SQL ISNULL (COALESCE) function for generic types.
	/// </summary>
	public static T IsNull<T>(this T s, T what)
		=> throw new NotSupportedException();
}