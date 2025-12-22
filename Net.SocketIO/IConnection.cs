namespace Ecng.Net;

/// <summary>
/// Defines a standard contract for a connection that can be established and disconnected.
/// </summary>
public interface IConnection
{
	/// <summary>
	/// Occurs when the connection state has changed.
	/// </summary>
	[Obsolete("Use StateChangedAsync event instead.")]
	event Action<ConnectionStates> StateChanged;

	/// <summary>
	/// Occurs when the connection state has changed (async version with CancellationToken).
	/// </summary>
	event Func<ConnectionStates, CancellationToken, ValueTask> StateChangedAsync;

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