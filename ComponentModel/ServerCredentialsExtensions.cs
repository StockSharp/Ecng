namespace Ecng.ComponentModel;

using System.Text.RegularExpressions;

using Ecng.Common;

/// <summary>
/// Extensions for validating login values used by <see cref="ServerCredentials"/> and elsewhere.
/// </summary>
public static class ServerCredentialsExtensions
{
	private static readonly Regex _userNameRegex = new(
		@"^(?=.{3,64}$)[A-Za-z0-9](?:[A-Za-z0-9._-]*[A-Za-z0-9])?$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	/// <summary>
	/// Validates that the given login string satisfies the specified <paramref name="asEmail"/>.
	/// </summary>
	/// <param name="login">Login value.</param>
	/// <param name="asEmail">Validation mode: username or email.</param>
	/// <returns>True if valid; otherwise, false.</returns>
	public static bool IsValidLogin(this string login, bool asEmail = false)
	{
		if (login.IsEmptyOrWhiteSpace())
			return false;

		return asEmail
			? login.IsValidEmailAddress()
			: _userNameRegex.IsMatch(login);
	}

	/// <summary>
	/// Convenience helper to validate the current <see cref="ServerCredentials.Email"/> as login.
	/// </summary>
	/// <param name="credentials">Credentials instance.</param>
	/// <param name="asEmail">Validation mode: username or email.</param>
	/// <returns>True if valid; otherwise, false.</returns>
	public static bool IsLoginValid(this ServerCredentials credentials, bool asEmail = false)
		=> (credentials.CheckOnNull(nameof(credentials)).Email).IsValidLogin(asEmail);
}