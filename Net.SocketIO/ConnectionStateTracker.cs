namespace Ecng.Net;

/// <summary>
/// Tracks the connection states of multiple <see cref="IAsyncConnection"/> instances and aggregates their overall state.
/// </summary>
public class ConnectionStateTracker : Disposable, IAsyncConnection,
#pragma warning disable CS0618 // Type or member is obsolete
	IConnection
#pragma warning restore CS0618
{
	/// <summary>
	/// Wraps an <see cref="IAsyncConnection"/> instance to listen for its state changes.
	/// </summary>
	private class ConnectionWrapper : Disposable
	{
		private readonly IAsyncConnection _connection;
		private readonly Func<CancellationToken, ValueTask> _stateChanged;

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
		public ConnectionWrapper(IAsyncConnection connection, Func<CancellationToken, ValueTask> stateChanged)
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
		private ValueTask OnStateChanged(ConnectionStates newState, CancellationToken cancellationToken)
		{
			State = newState;
			return _stateChanged(cancellationToken);
		}
	}

#pragma warning disable CS0618 // Type or member is obsolete
	/// <summary>
	/// Adapts an <see cref="IConnection"/> to <see cref="IAsyncConnection"/>.
	/// </summary>
	private sealed class ConnectionAdapter(IConnection connection) : IAsyncConnection
	{
		public event Func<ConnectionStates, CancellationToken, ValueTask> StateChanged
		{
			add => connection.StateChanged += state => value(state, default);
			remove { }
		}

		public ValueTask ConnectAsync(CancellationToken cancellationToken)
			=> connection.ConnectAsync(cancellationToken);

		public void Disconnect()
			=> connection.Disconnect();
	}
#pragma warning restore CS0618

	private readonly CachedSynchronizedDictionary<IAsyncConnection, ConnectionWrapper> _connections = [];
	private readonly Lock _currStateLock = new();
	private ConnectionStates _currState = ConnectionStates.Disconnected;

	private event Func<ConnectionStates, CancellationToken, ValueTask> _stateChanged;
#pragma warning disable CS0618 // Type or member is obsolete
	private event Action<ConnectionStates> _syncStateChanged;

	/// <inheritdoc />
	event Action<ConnectionStates> IConnection.StateChanged
	{
		add => _syncStateChanged += value;
		remove => _syncStateChanged -= value;
	}
#pragma warning restore CS0618

	/// <inheritdoc />
	event Func<ConnectionStates, CancellationToken, ValueTask> IAsyncConnection.StateChanged
	{
		add => _stateChanged += value;
		remove => _stateChanged -= value;
	}

	/// <summary>
	/// Connects all tracked connections asynchronously.
	/// </summary>
	/// <param name="cancellationToken">A token to observe for cancellation.</param>
	/// <returns>A task that represents the asynchronous connect operation.</returns>
	/// <exception cref="InvalidOperationException">Thrown when there are no connections to connect.</exception>
	public ValueTask ConnectAsync(CancellationToken cancellationToken)
	{
		var connections = Connections;

		if (connections.Length == 0)
			throw new InvalidOperationException("No connections.");

		return connections.Select(c => c.ConnectAsync(cancellationToken)).WhenAll();
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
	public void Add(IAsyncConnection connection)
		=> _connections.Add(connection, new(connection, UpdateOverallStateAsync));

	/// <summary>
	/// Adds a connection to be tracked.
	/// </summary>
	/// <param name="connection">The connection to add.</param>
	[Obsolete("Use Add(IAsyncConnection) instead.")]
#pragma warning disable CS0618 // Type or member is obsolete
	public void Add(IConnection connection)
#pragma warning restore CS0618
		=> Add(new ConnectionAdapter(connection));

	/// <summary>
	/// Removes a tracked connection.
	/// </summary>
	/// <param name="connection">The connection to remove.</param>
	/// <returns>True if the connection was successfully removed; otherwise, false.</returns>
	public bool Remove(IAsyncConnection connection)
	{
		if (!_connections.TryGetAndRemove(connection, out var wrapper))
			return false;

		wrapper.Dispose();

		return true;
	}

	/// <summary>
	/// Removes a tracked connection.
	/// </summary>
	/// <param name="connection">The connection to remove.</param>
	/// <returns>True if the connection was successfully removed; otherwise, false.</returns>
	[Obsolete("Use Remove(IAsyncConnection) instead.")]
#pragma warning disable CS0618 // Type or member is obsolete
	public bool Remove(IConnection connection)
#pragma warning restore CS0618
	{
		var key = _connections.CachedPairs.FirstOrDefault(p => p.Key is ConnectionAdapter).Key;
		if (key is null)
			return false;

		return Remove(key);
	}

	private IAsyncConnection[] Connections => _connections.CachedKeys;
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
	private async ValueTask UpdateOverallStateAsync(CancellationToken cancellationToken)
	{
		ConnectionStates newState;

		using (_currStateLock.EnterScope())
		{
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
			else
			{
				return;
			}

			if (newState == _currState)
				return;

			_currState = newState;
		}

		_syncStateChanged?.Invoke(newState);

		if (_stateChanged is { } handler)
			await handler(newState, cancellationToken);
	}
}
