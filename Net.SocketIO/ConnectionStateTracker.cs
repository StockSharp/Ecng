﻿namespace Ecng.Net;

public class ConnectionStateTracker : Disposable, IConnection
{
	private class ConnectionWrapper : Disposable
	{
		private readonly IConnection _connection;
		private readonly Action _stateChanged;

		public ConnectionStates State { get; private set; }

		public ConnectionWrapper(IConnection connection, Action stateChanged)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_stateChanged = stateChanged ?? throw new ArgumentNullException(nameof(stateChanged));

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
			_stateChanged();
		}
	}

	private readonly CachedSynchronizedDictionary<IConnection, ConnectionWrapper> _connections = [];
	private readonly SyncObject _currStateLock = new();
	private ConnectionStates _currState = ConnectionStates.Disconnected;

	public event Action<ConnectionStates> StateChanged;

	public ValueTask ConnectAsync(CancellationToken cancellationToken)
	{
		var connections = Connections;

		if (connections.Length == 0)
			throw new InvalidOperationException("No connections.");

		return connections.Select(c => c.ConnectAsync(cancellationToken)).WhenAll();
	}

	public void Disconnect()
		=> Connections.ForEach(c => c.Disconnect());

	public void Add(IConnection connection)
		=> _connections.Add(connection, new(connection, UpdateOverallState));

	public bool Remove(IConnection connection)
	{
		if (!_connections.TryGetAndRemove(connection, out var wrapper))
			return false;

		wrapper.Dispose();

		return true;
	}

	private IConnection[] Connections => _connections.CachedKeys;
	private ConnectionWrapper[] Wrappers => _connections.CachedValues;

	protected override void DisposeManaged()
	{
		foreach (var wrapper in Wrappers)
			wrapper.Dispose();

		base.DisposeManaged();
	}

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