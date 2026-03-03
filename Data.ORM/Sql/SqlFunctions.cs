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