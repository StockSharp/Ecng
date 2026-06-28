namespace Ecng.Net;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Checks whether a password appears in any known data breach corpus.
/// Implementations should fail open (treat transient/network errors as
/// "not breached") so an outage does not lock users out of password-gated flows.
/// </summary>
public interface IPasswordBreachChecker
{
	/// <summary>
	/// Returns <see langword="true"/> when the password is present in a known
	/// breach corpus and should be refused; <see langword="false"/> on a miss
	/// or on a transient failure.
	/// </summary>
	/// <param name="password">The plaintext password to check.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken = default);
}
