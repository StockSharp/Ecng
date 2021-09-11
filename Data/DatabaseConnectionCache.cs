namespace Ecng.Data
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// Кэш <see cref="DatabaseConnectionPair"/>.
	/// </summary>
	public class DatabaseConnectionCache : IPersistable
	{
		private readonly CachedSynchronizedSet<DatabaseConnectionPair> _connections = new();

		/// <summary>
		/// </summary>
		public DatabaseConnectionCache()
		{
		}

		/// <summary>
		/// Список всех подключений.
		/// </summary>
		public IEnumerable<DatabaseConnectionPair> Connections => _connections.Cache;

		/// <summary>
		/// Событие создания нового подключения.
		/// </summary>
		public event Action<DatabaseConnectionPair> ConnectionCreated;

		/// <summary>
		/// Событие удаления подключения.
		/// </summary>
		public event Action<DatabaseConnectionPair> ConnectionDeleted;

		/// <summary>
		/// Получить подключение к базе данных.
		/// </summary>
		/// <param name="provider">Провайдер баз данных.</param>
		/// <param name="connectionString">Строка подключения.</param>
		/// <returns>Подключение к базе данных.</returns>
		public DatabaseConnectionPair GetConnection(Type provider, string connectionString)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			if (connectionString.IsEmpty())
				throw new ArgumentNullException(nameof(connectionString));

			var connection = Connections.FirstOrDefault(p => p.Provider == provider && p.ConnectionString.EqualsIgnoreCase(connectionString));

			if (connection is null)
			{
				connection = new DatabaseConnectionPair { Provider = provider, ConnectionString = connectionString };
				AddConnection(connection);
			}

			return connection;
		}

		/// <summary>
		/// Добавить новое подключение к базе данных.
		/// </summary>
		/// <param name="connection">Новое подключение.</param>
		private void AddConnection(DatabaseConnectionPair connection)
		{
			if (connection is null)
				throw new ArgumentNullException(nameof(connection));

			_connections.Add(connection);
			ConnectionCreated?.Invoke(connection);
		}

		/// <summary>
		/// Удалить подключение к базе данных.
		/// </summary>
		/// <param name="connection">Подключение.</param>
		public bool DeleteConnection(DatabaseConnectionPair connection)
		{
			if (connection is null)
				throw new ArgumentNullException(nameof(connection));

			if (!_connections.Remove(connection))
				return false;

			ConnectionDeleted?.Invoke(connection);
			return true;
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			var connections = storage
				.GetValue<IEnumerable<SettingsStorage>>(nameof(Connections))
				.Select(s => s.Load<DatabaseConnectionPair>())
				.Where(p => p.Provider != null)
				.ToArray();

			lock (_connections.SyncRoot)
				_connections.AddRange(connections);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Connections), Connections.Select(pair => pair.Save()).ToArray());
		}
	}
}