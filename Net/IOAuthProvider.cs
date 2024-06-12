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
	Task<(SecureString token, DateTime? till)> RequestToken(long socialId, CancellationToken cancellationToken);
}