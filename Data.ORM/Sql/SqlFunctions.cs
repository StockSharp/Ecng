namespace Ecng.Data.Sql;

public static class SqlFunctions
{
	public static bool Like(this string s, string what)
		=> throw new NotSupportedException();

	public static string IfNull(this string s, string what)
		=> throw new NotSupportedException();

	public static T IsNull<T>(this T s, T what)
		=> throw new NotSupportedException();
}