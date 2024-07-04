namespace Ecng.Net;

/// <summary>
/// Provider for OAuth.
/// </summary>
public interface IOAuthProvider
{
	/// <summary>
	/// Request token.
	/// </summary>
	/// <param name="socialId">The social ID.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>
	/// The task representing the request for the OAuth token.
	/// </returns>
	Task<IOAuthToken> RequestToken(long socialId, CancellationToken cancellationToken);
}