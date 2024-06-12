namespace Ecng.Net;

/// <summary>
/// Provider for OAuth.
/// </summary>
public interface IOAuthProvider
{
	/// <summary>
	/// Request token.
	/// </summary>
	/// <param name="socialId"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<IOAuthToken> RequestToken(long socialId, CancellationToken cancellationToken);
}