namespace Ecng.Net;

using System;
using System.Text.Json;

using Ecng.Common;

/// <summary>
/// Minimal read-only helpers over a JWT — just enough to know when an access token is about to
/// expire so a caller can refresh it before (re)authenticating. The signature is NOT verified here
/// (that is the issuer's / server's job).
/// </summary>
public static class JwtHelper
{
	/// <summary>
	/// The token's expiry (UTC) from the <c>exp</c> claim, or <see langword="null"/> if it cannot be read.
	/// </summary>
	public static DateTime? GetExpiry(string token)
	{
		if (token.IsEmptyOrWhiteSpace())
			return null;

		var parts = token.Split('.');
		if (parts.Length < 2)
			return null;

		try
		{
			var json = parts[1].Base64Url().UTF8();
			using var doc = JsonDocument.Parse(json);

			if (doc.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var seconds))
				return seconds.FromUnix();
		}
		catch
		{
			// Unparseable token — treated as "unknown expiry" by the caller.
		}

		return null;
	}

	/// <summary>
	/// Whether the token is already expired or will expire within <paramref name="skew"/>. Returns
	/// <see langword="false"/> when the expiry cannot be determined, so a readable token is never
	/// refreshed needlessly and an unreadable one is left for the server to reject.
	/// </summary>
	public static bool IsExpiredOrExpiring(string token, TimeSpan skew)
	{
		var exp = GetExpiry(token);
		return exp is not null && exp.Value <= DateTime.UtcNow + skew;
	}
}
