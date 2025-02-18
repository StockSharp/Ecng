namespace Ecng.Net;

/// <summary>
/// Represents an OAuth token with its associated value and expiration information.
/// </summary>
public interface IOAuthToken
{
	/// <summary>
	/// Gets the value of the OAuth token.
	/// </summary>
	string Value { get; }

	/// <summary>
	/// Gets the expiration date and time of the OAuth token, if available.
	/// </summary>
	DateTime? Expires { get; }
}