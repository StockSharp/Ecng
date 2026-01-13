namespace Ecng.Data;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents a database connection.
/// </summary>
public interface IDatabaseConnection : IDisposable
{
	/// <summary>
	/// Verifies the connection is valid and can be opened.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task VerifyAsync(CancellationToken cancellationToken);
}
