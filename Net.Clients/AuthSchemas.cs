namespace Ecng.Net;

public static class AuthSchemas
{
	public const string Basic = "Basic";
	public const string Bearer = "Bearer";

	public static string FormatAuth(this string schema, string token)
		=> $"{schema} {token}";
}