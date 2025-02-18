namespace Ecng.Net;

/// <summary>
/// Provides authentication schemas and extension methods for formatting authentication tokens.
/// </summary>
public static class AuthSchemas
{
	/// <summary>
	/// Represents the basic authentication scheme.
	/// </summary>
	public const string Basic = "Basic";

	/// <summary>
	/// Represents the bearer authentication scheme.
	/// </summary>
	public const string Bearer = "Bearer";

	/// <summary>
	/// Formats the authentication header using a secure string token.
	/// The token is converted to its unsecured string representation before formatting.
	/// </summary>
	/// <param name="schema">The authentication scheme.</param>
	/// <param name="token">The secure string token.</param>
	/// <returns>A formatted authentication header.</returns>
	public static string FormatAuth(this string schema, SecureString token)
		=> schema.FormatAuth(token.UnSecure());

	/// <summary>
	/// Formats the authentication header using a regular string token.
	/// </summary>
	/// <param name="schema">The authentication scheme.</param>
	/// <param name="token">The token as a string.</param>
	/// <returns>A formatted authentication header.</returns>
	public static string FormatAuth(this string schema, string token)
		=> $"{schema} {token}";
}