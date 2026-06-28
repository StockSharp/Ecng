namespace Ecng.Net;

using System;

using Ecng.Common;

/// <summary>
/// Base64url (RFC 4648 §5) encoding — '-' '_' instead of '+' '/', no '=' padding. Used by JWT,
/// OAuth / PKCE, WebPush and similar web payloads. The actual base64 step delegates to the existing
/// <c>Base64()</c> string/byte[] extensions rather than calling <see cref="Convert"/> again.
/// </summary>
public static class Base64UrlExtensions
{
	/// <summary>
	/// Decodes a base64url-encoded string to bytes.
	/// </summary>
	/// <param name="value">The base64url-encoded string.</param>
	/// <returns>The byte array representation.</returns>
	public static byte[] Base64Url(this string value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		var s = value.Replace('-', '+').Replace('_', '/');

		switch (s.Length % 4)
		{
			case 2: s += "=="; break;
			case 3: s += "="; break;
		}

		return s.Base64();
	}

	/// <summary>
	/// Encodes a byte array to a base64url string (no '=' padding).
	/// </summary>
	/// <param name="value">The byte array.</param>
	/// <returns>The base64url-encoded string.</returns>
	public static string Base64Url(this byte[] value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		return value.Base64().TrimEnd('=').Replace('+', '-').Replace('/', '_');
	}
}
