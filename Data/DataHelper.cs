namespace Ecng.Data
{
	using System;
	using System.Data;

	using Ecng.Common;
	using Ecng.Localization;

	/// <summary>
	/// Extensions for <see cref="Data"/>.
	/// </summary>
	public static class DataHelper
	{
		/// <summary>
		/// </summary>
		public static IDbConnection CreateConnection(this DatabaseConnectionPair pair)
		{
			if (pair is null)
				throw new ArgumentNullException(nameof(pair));

			var provider = pair.Provider;

			if (provider is null)
				throw new InvalidOperationException("Provider is not set.".Translate());

			var connStr = pair.ConnectionString;

			if (connStr.IsEmpty())
				throw new InvalidOperationException("Cannot create a connection, because some data was not entered.".Translate());

			var connection = provider.CreateInstance<IDbConnection>();
			connection.ConnectionString = connStr;
			return connection;
		}

		/// <summary>
		/// Verify the connection is ok.
		/// </summary>
		/// <param name="pair">Connection.</param>
		public static void Verify(this DatabaseConnectionPair pair)
		{
			using var testConn = pair.CreateConnection();
			testConn.Open();
		}
	}
}