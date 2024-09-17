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
		private readonly CachedSynchronizedSet<DatabaseConnectionPair> _connections = [];

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

		public event Action Updated;

		/// <summary>
		/// Получить подключение к базе данных.
		/// </summary>
		/// <param name="provider">Провайдер баз данных.</param>
		/// <param name="connectionString">Строка подключения.</param>
		/// <returns>Подключение к базе данных.</returns>
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
				connection = _connections.FirstOrDefault(p => p.Provider == provider && p.ConnectionString.EqualsIgnoreCase(connectionString));

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
			Updated?.Invoke();
			return true;
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			_connections.AddRange(storage
				.GetValue<IEnumerable<DatabaseConnectionPair>>(nameof(Connections))
				.Where(p => p.Provider != null));
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