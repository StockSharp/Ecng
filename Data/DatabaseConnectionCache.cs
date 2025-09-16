namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Serialization;

/// <summary>
/// Represents a cache for <see cref="DatabaseConnectionPair"/> objects.
/// </summary>
public class DatabaseConnectionCache : IPersistable
{
	private readonly CachedSynchronizedSet<DatabaseConnectionPair> _connections = [];

	/// <summary>
	/// Gets all database connection pairs.
	/// </summary>
	public IEnumerable<DatabaseConnectionPair> Connections => _connections.Cache;

	/// <summary>
	/// Occurs when a new database connection pair is created.
	/// </summary>
	public event Action<DatabaseConnectionPair> ConnectionCreated;

	/// <summary>
	/// Occurs when a database connection pair is deleted.
	/// </summary>
	public event Action<DatabaseConnectionPair> ConnectionDeleted;

	/// <summary>
	/// Occurs when the connection cache is updated.
	/// </summary>
	public event Action Updated;

	/// <summary>
	/// Retrieves an existing connection matching the specified provider and connection string or adds a new connection if it does not exist.
	/// </summary>
	/// <param name="provider">The database provider.</param>
	/// <param name="connectionString">The connection string.</param>
	/// <returns>The corresponding <see cref="DatabaseConnectionPair"/>.</returns>
	public DatabaseConnectionPair GetOrAdd(string provider, string connectionString)
	{
		if (provider.IsEmpty())
			throw new ArgumentNullException(nameof(provider));

		if (connectionString.IsEmpty())
			throw new ArgumentNullException(nameof(connectionString));

		var isNew = false;
		DatabaseConnectionPair connection;

		lock (_connections.SyncRoot)
		{
			connection = _connections.FirstOrDefault(p => p.Provider.EqualsIgnoreCase(provider) && p.ConnectionString.EqualsIgnoreCase(connectionString));

			if (connection is null)
			{
				isNew = true;
				_connections.Add(connection = new()
				{
					Provider = provider,
					ConnectionString = connectionString,
				});
			}
		}

		if (isNew)
			ConnectionCreated?.Invoke(connection);

		Updated?.Invoke();
		return connection;
	}

	/// <summary>
	/// Deletes the specified database connection pair from the cache.
	/// </summary>
	/// <param name="connection">The database connection pair to delete.</param>
	/// <returns>
	/// True if the connection was successfully removed; otherwise, false.
	/// </returns>
	public bool DeleteConnection(DatabaseConnectionPair connection)
	{
		if (connection is null)
			throw new ArgumentNullException(nameof(connection));

		if (!_connections.Remove(connection))
			return false;

		ConnectionDeleted?.Invoke(connection);
		Updated?.Invoke();
		return true;
	}

	/// <summary>
	/// Loads the database connection pairs from the specified settings storage.
	/// </summary>
	/// <param name="storage">The settings storage to load from.</param>
	public void Load(SettingsStorage storage)
	{
		_connections.AddRange(storage
			.GetValue<IEnumerable<DatabaseConnectionPair>>(nameof(Connections))
			.Where(p => !p.Provider.IsEmpty()));
	}

	/// <summary>
	/// Saves the database connection pairs to the specified settings storage.
	/// </summary>
	/// <param name="storage">The settings storage to save to.</param>
	public void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Connections), Connections.Select(pair => pair.Save()).ToArray());
	}
}