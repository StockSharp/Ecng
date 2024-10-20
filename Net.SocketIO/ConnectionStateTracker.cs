namespace Ecng.Net;

public class ConnectionStateTracker : Disposable, IConnection
{
	private class ConnectionWrapper : Disposable
	{
		private readonly IConnection _connection;
		public ConnectionStates State { get; private set; }

		public ConnectionWrapper(IConnection connection)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			State = ConnectionStates.Disconnected;

			_connection.StateChanged += OnStateChanged;
		}

		protected override void DisposeManaged()
		{
			_connection.StateChanged -= OnStateChanged;
			base.DisposeManaged();
		}

		private void OnStateChanged(ConnectionStates newState)
		{
			State = newState;
			StateChanged?.Invoke(this, newState);
		}

		public event Action<ConnectionWrapper, ConnectionStates> StateChanged;
	}

	private readonly ConnectionWrapper[] _connections;
	private readonly SyncObject _currStateLock = new();
	private ConnectionStates _currState = ConnectionStates.Disconnected;

	public event Action<ConnectionStates> StateChanged;

	public ConnectionStateTracker(IEnumerable<IConnection> connections)
	{
		if (connections is null)
			throw new ArgumentNullException(nameof(connections));

		_connections = connections.Select(c => new ConnectionWrapper(c)).ToArray();

		foreach (var wrapper in _connections)
		{
			wrapper.StateChanged += OnConnectionStateChanged;
		}
	}

	protected override void DisposeManaged()
	{
		foreach (var wrapper in _connections)
		{
			wrapper.StateChanged -= OnConnectionStateChanged;
			wrapper.Dispose();
		}

		base.DisposeManaged();
	}

	private void OnConnectionStateChanged(ConnectionWrapper connection, ConnectionStates newState)
	{
		UpdateOverallState();
	}

	private void UpdateOverallState()
	{
		lock (_currStateLock)
		{
			ConnectionStates newState;

			if (_connections.All(c => c.State == ConnectionStates.Connected))
			{
				newState = ConnectionStates.Connected;
			}
			else if (_connections.Any(c => c.State == ConnectionStates.Reconnecting))
			{
				newState = ConnectionStates.Reconnecting;
			}
			else if (_connections.All(c => c.State == ConnectionStates.Connected || c.State == ConnectionStates.Restored))
			{
				newState = ConnectionStates.Restored;
			}
			else if (_connections.All(c => c.State == ConnectionStates.Disconnected || c.State == ConnectionStates.Failed))
			{
				newState = ConnectionStates.Disconnected;
			}
			else if (_connections.All(c => c.State == ConnectionStates.Failed))
			{
				newState = ConnectionStates.Failed;
			}
			else
			{
				return;
			}

			if (newState == _currState)
				return;

			_currState = newState;
		}
		
		StateChanged?.Invoke(_currState);
	}
}