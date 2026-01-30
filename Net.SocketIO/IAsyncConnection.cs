namespace Ecng.Net;

/// <summary>
/// Defines an asynchronous contract for a connection that can be established and disconnected.
/// </summary>
public interface IAsyncConnection
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
	/// Disconnects the current connection.
	/// </summary>
	void Disconnect();
}
