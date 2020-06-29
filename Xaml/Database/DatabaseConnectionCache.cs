namespace Ecng.Xaml.Database
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Data;
	using Ecng.Data.Providers;
	using Ecng.Serialization;

	/// <summary>
	/// ��� <see cref="DatabaseConnectionPair"/>.
	/// </summary>
	public class DatabaseConnectionCache : IPersistable
	{
		private readonly CachedSynchronizedSet<DatabaseConnectionPair> _connections = new CachedSynchronizedSet<DatabaseConnectionPair>();

		public DatabaseConnectionCache()
		{
		}

		//private static readonly Lazy<DatabaseConnectionCache> _instance = new Lazy<DatabaseConnectionCache>(() => new DatabaseConnectionCache());

		///// <summary>
		///// ���.
		///// </summary>
		//public static DatabaseConnectionCache Instance => _instance.Value;

		/// <summary>
		/// ������ ���� �����������.
		/// </summary>
		public IEnumerable<DatabaseConnectionPair> Connections => _connections.Cache;

		/// <summary>
		/// ������� �������� ������ �����������.
		/// </summary>
		public event Action<DatabaseConnectionPair> NewConnectionCreated;

		/// <summary>
		/// ������� �������� �����������.
		/// </summary>
		public event Action<DatabaseConnectionPair> ConnectionDeleted;

		/// <summary>
		/// �������� ����������� � ���� ������.
		/// </summary>
		/// <param name="provider">��������� ��� ������.</param>
		/// <param name="connectionString">������ �����������.</param>
		/// <returns>����������� � ���� ������.</returns>
		public DatabaseConnectionPair GetConnection(DatabaseProvider provider, string connectionString)
		{
			var connection = Connections.FirstOrDefault(p => p.Provider == provider && p.ConnectionString.CompareIgnoreCase(connectionString));

			if (connection == null)
			{
				connection = new DatabaseConnectionPair { Provider = provider, ConnectionString = connectionString };
				AddConnection(connection);
			}

			return connection;
		}

		/// <summary>
		/// �������� ����� ����������� � ���� ������.
		/// </summary>
		/// <param name="connection">����� �����������.</param>
		private void AddConnection(DatabaseConnectionPair connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			_connections.Add(connection);
			NewConnectionCreated?.Invoke(connection);
		}

		/// <summary>
		/// ������� ����������� � ���� ������.
		/// </summary>
		/// <param name="connection">�����������.</param>
		public void DeleteConnection(DatabaseConnectionPair connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			if (_connections.Remove(connection))
				ConnectionDeleted?.Invoke(connection);
		}

		/// <summary>
		/// ��������� ���������.
		/// </summary>
		/// <param name="storage">��������� ��������.</param>
		public void Load(SettingsStorage storage)
		{
			var connections = storage
				.GetValue<IEnumerable<SettingsStorage>>(nameof(Connections))
				.Select(s =>
				{
					var providerName = s.GetValue<string>(nameof(DatabaseConnectionPair.Provider));
					var provider = DatabaseProviderRegistry.Providers.FirstOrDefault(p => p.Name.CompareIgnoreCase(providerName));

					return provider == null
						? null
						: new DatabaseConnectionPair
						{
							Provider = provider,
							ConnectionString = s.GetValue<string>(nameof(DatabaseConnectionPair.ConnectionString))
						};
				})
				.Where(p => p != null)
				.ToArray();

			lock (_connections.SyncRoot)
				_connections.AddRange(connections);
		}

		/// <summary>
		/// ��������� ���������.
		/// </summary>
		/// <param name="storage">��������� ��������.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Connections), Connections.Select(pair => new SettingsStorage
			{
				[nameof(DatabaseConnectionPair.Provider)] = pair.Provider.Name,
				[nameof(DatabaseConnectionPair.ConnectionString)] = pair.ConnectionString
			}).ToArray());
		}
	}
}