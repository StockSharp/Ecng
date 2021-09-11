namespace Ecng.Data
{
	using System;

	using Ecng.Common;
	using Ecng.Localization;

	using LinqToDB.Data;
	using LinqToDB.DataProvider;

	/// <summary>
	/// Extensions for <see cref="Data"/>.
	/// </summary>
	public static class DataHelper
	{
		/// <summary>
		/// </summary>
		public static DataConnection CreateConnection(this DatabaseConnectionPair pair)
		{
			if (pair is null)
				throw new ArgumentNullException(nameof(pair));

			var provider = pair.Provider;

			if (provider is null)
				throw new InvalidOperationException("Provider is not set.".Translate());

			var connStr = pair.ConnectionString;

			if (connStr.IsEmpty())
				throw new InvalidOperationException("Cannot create a connection, because some data was not entered.".Translate());

			return new DataConnection(provider.CreateInstance<IDataProvider>(), connStr);
		}

		/// <summary>
		/// Verify the connection is ok.
		/// </summary>
		/// <param name="pair">Connection.</param>
		public static void Verify(this DatabaseConnectionPair pair)
		{
			using var db = pair.CreateConnection();
			db.Connection.Open();
		}
	}
}