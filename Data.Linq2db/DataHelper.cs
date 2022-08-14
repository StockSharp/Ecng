namespace Ecng.Data
{
	using System;

	using Ecng.Common;

	using LinqToDB.Data;

	/// <summary>
	/// Extensions for <see cref="Data"/>.
	/// </summary>
	public static class DataHelper
	{
		/// <summary>
		/// Create connection.
		/// </summary>
		/// <param name="pair"><see cref="DatabaseConnectionPair"/>.</param>
		/// <returns><see cref="DataConnection"/>.</returns>
		public static DataConnection CreateConnection(this DatabaseConnectionPair pair)
		{
			if (pair is null)
				throw new ArgumentNullException(nameof(pair));

			var provider = pair.Provider;

			if (provider is null)
				throw new InvalidOperationException("Provider is not set.");

			var connStr = pair.ConnectionString;

			if (connStr.IsEmpty())
				throw new InvalidOperationException("Cannot create a connection, because some data was not entered.");

			return new DataConnection(DatabaseProviderRegistry.CreateProvider(provider), connStr);
		}

		/// <summary>
		/// Verify the connection is ok.
		/// </summary>
		/// <param name="pair">Connection.</param>
		public static void Verify(this DatabaseConnectionPair pair)
		{
			using var db = pair.CreateConnection();
			using var conn = db.DataProvider.CreateConnection(db.ConnectionString);
			conn.Open();
		}
	}
}