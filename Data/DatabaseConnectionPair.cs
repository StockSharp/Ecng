namespace Ecng.Data
{
	using System;
	using System.Data;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Localization;
	using Ecng.Serialization;

	/// <summary>
	/// Provider and connection string pair.
	/// </summary>
	public class DatabaseConnectionPair : NotifiableObject, IPersistable
	{
		private Type _provider = DatabaseProviderRegistry.Providers.FirstOrDefault();

		/// <summary>
		/// Provider type.
		/// </summary>
		public Type Provider
		{
			get => _provider;
			set
			{
				_provider = value;
				UpdateTitle();
			}
		}

		private string _connectionString;

		/// <summary>
		/// Connection settings.
		/// </summary>
		public string ConnectionString
		{
			get => _connectionString;
			set
			{
				_connectionString = value;
				UpdateTitle();
			}
		}

		/// <summary>
		/// Connection title.
		/// </summary>
		public string Title => $"({Provider.Name}) {ConnectionString}";

		private void UpdateTitle()
		{
			NotifyChanged(nameof(Title));
		}

		/// <inheritdoc />
		public override string ToString() => Title;

		/// <summary>
		/// </summary>
		public IDbConnection CreateConnection()
		{
			if (Provider is null)
				throw new InvalidOperationException("Provider is not set.".Translate());

			if (ConnectionString.IsEmpty())
				throw new InvalidOperationException("Cannot create a connection, because some data was not entered.".Translate());

			var connection = Provider.CreateInstance<IDbConnection>();
			connection.ConnectionString = ConnectionString;
			return connection;
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			Provider = storage.GetValue<Type>(nameof(Provider));
			ConnectionString = storage.GetValue<string>(nameof(ConnectionString));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage
				.Set(nameof(Provider), Provider)
				.Set(nameof(ConnectionString), ConnectionString)
				;
		}
	}
}