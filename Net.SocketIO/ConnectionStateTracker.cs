﻿namespace Ecng.Net;

/// <summary>
/// Tracks the connection states of multiple IConnection instances and aggregates their overall state.
/// </summary>
public class ConnectionStateTracker : Disposable, IConnection
{
	/// <summary>
	/// Wraps an IConnection instance to listen for its state changes.
	/// </summary>
	private class ConnectionWrapper : Disposable
	{
		private readonly IConnection _connection;
		private readonly Action _stateChanged;

		/// <summary>
		/// Gets the current connection state of the wrapped connection.
		/// </summary>
		public ConnectionStates State { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionWrapper"/> class.
		/// </summary>
		/// <param name="connection">The connection to wrap.</param>
		/// <param name="stateChanged">Callback invoked when the connection state changes.</param>
		/// <exception cref="ArgumentNullException">Thrown when connection or stateChanged is null.</exception>
		public ConnectionWrapper(IConnection connection, Action stateChanged)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_stateChanged = stateChanged ?? throw new ArgumentNullException(nameof(stateChanged));

			State = ConnectionStates.Disconnected;

			_connection.StateChanged += OnStateChanged;
		}

		/// <summary>
		/// Disposes the managed resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			_connection.StateChanged -= OnStateChanged;
			base.DisposeManaged();
		}

		/// <summary>
		/// Handles the state change event from the wrapped connection.
		/// </summary>
		/// <param name="newState">The new state of the connection.</param>
		private void OnStateChanged(ConnectionStates newState)
		{
			State = newState;
			_stateChanged();
		}
	}

	private readonly CachedSynchronizedDictionary<IConnection, ConnectionWrapper> _connections = [];
	private readonly SyncObject _currStateLock = new();
	private ConnectionStates _currState = ConnectionStates.Disconnected;

	/// <summary>
	/// Occurs when the overall connection state changes.
	/// </summary>
	public event Action<ConnectionStates> StateChanged;

	/// <summary>
	/// Connects all tracked connections asynchronously.
	/// </summary>
	/// <param name="cancellationToken">A token to observe for cancellation.</param>
	/// <returns>A task that represents the asynchronous connect operation.</returns>
	/// <exception cref="InvalidOperationException">Thrown when there are no connections to connect.</exception>
	/// <exception cref="AggregateException">Thrown when one or more connections fail to connect.</exception>
	public async ValueTask ConnectAsync(CancellationToken cancellationToken)
	{
		var connections = Connections;

		if (connections.Length == 0)
			throw new InvalidOperationException("No connections.");

		var tasks = connections.Select(c => c.ConnectAsync(cancellationToken).AsTask()).ToArray();

		try
		{
			await Task.WhenAll(tasks).ConfigureAwait(false);
		}
		catch
		{
			var exceptions = tasks
				.Where(t => t.IsFaulted)
				.SelectMany(t => t.Exception?.InnerExceptions ?? [])
				.ToArray();

			if (exceptions.Length > 0)
				throw new AggregateException("One or more connections failed to connect.", exceptions);

			throw;
		}
	}

	/// <summary>
	/// Disconnects all tracked connections.
	/// </summary>
	public void Disconnect()
		=> Connections.ForEach(c => c.Disconnect());

	/// <summary>
	/// Adds a connection to be tracked.
	/// </summary>
	/// <param name="connection">The connection to add.</param>
	public void Add(IConnection connection)
		=> _connections.Add(connection, new(connection, UpdateOverallState));

	/// <summary>
	/// Removes a tracked connection.
	/// </summary>
	/// <param name="connection">The connection to remove.</param>
	/// <returns>True if the connection was successfully removed; otherwise, false.</returns>
	public bool Remove(IConnection connection)
	{
		if (!_connections.TryGetAndRemove(connection, out var wrapper))
			return false;

		wrapper.Dispose();

		return true;
	}

	private IConnection[] Connections => _connections.CachedKeys;
	private ConnectionWrapper[] Wrappers => _connections.CachedValues;

	/// <summary>
	/// Releases the managed resources used by the <see cref="ConnectionStateTracker"/>.
	/// </summary>
	protected override void DisposeManaged()
	{
		foreach (var wrapper in Wrappers)
			wrapper.Dispose();

		base.DisposeManaged();
	}

	/// <summary>
	/// Updates the overall state based on the states of all tracked connections.
	/// </summary>
	private void UpdateOverallState()
	{
		lock (_currStateLock)
		{
			ConnectionStates newState;

			if (Wrappers.All(c => c.State == ConnectionStates.Connected))
			{
				newState = ConnectionStates.Connected;
			}
			else if (Wrappers.Any(c => c.State == ConnectionStates.Reconnecting))
			{
				newState = ConnectionStates.Reconnecting;
			}
			else if (Wrappers.All(c => c.State == ConnectionStates.Connected || c.State == ConnectionStates.Restored))
			{
				newState = ConnectionStates.Restored;
			}
			else if (Wrappers.All(c => c.State == ConnectionStates.Failed))
			{
				newState = ConnectionStates.Failed;
			}
			else if (Wrappers.All(c => c.State == ConnectionStates.Disconnected || c.State == ConnectionStates.Failed))
			{
				newState = ConnectionStates.Disconnected;
			}
			else if (Wrappers.Any(c => c.State == ConnectionStates.Connecting))
			{
				newState = ConnectionStates.Connecting;
			}
			else if (Wrappers.Any(c => c.State == ConnectionStates.Disconnecting))
			{
				newState = ConnectionStates.Disconnecting;
			}
			else if (Wrappers.Any(c => c.State == ConnectionStates.Failed))
			{
				newState = ConnectionStates.Failed;
			}
			else
			{
				// For any other mixed states, keep current state
				return;
			}

			if (newState == _currState)
				return;

			_currState = newState;
		}
		
		StateChanged?.Invoke(_currState);
	}
}