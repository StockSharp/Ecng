namespace Ecng.Net;

/// <summary>
/// Defines an asynchronous contract for a connection that can be established and disconnected.
/// </summary>
public interface IConnection
{
	/// <summary>
	/// Occurs when the connection state has changed.
	/// </summary>
	event Func<ConnectionStates, CancellationToken, ValueTask> StateChanged;

	/// <summary>
	/// Asynchronously connects to a target.
	/// </summary>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	ValueTask ConnectAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Asynchronously disconnects the current connection.
	/// </summary>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	ValueTask DisconnectAsync(CancellationToken cancellationToken);
}
