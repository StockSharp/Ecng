namespace Ecng.Data
{
	using System;
	using System.Collections.Generic;
	using System.Data.Common;
	using System.Linq;
	using System.IO;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Data.Sql;
	using Ecng.Serialization;

	public static class Extensions
	{
		/// <summary>
		/// Execute the query.
		/// </summary>
		/// <param name="database">The database.</param>
		/// <param name="query">The query to execute.</param>
		/// <param name="schema">The schema.</param>
		public static void Execute(this Database database, Query query, Schema schema)
		{
			database.Execute(database.GetCommand(query, schema, new FieldList(), new FieldList()), new SerializationItemCollection(), false);
		}

		/// <summary>
		/// Execute the query.
		/// </summary>
		/// <param name="database">The database.</param>
		/// <param name="query">The query to execute.</param>
		public static void Execute(this Database database, string query)
		{
			database
				.GetCommand(Query.Execute(query), null, new FieldList(), new FieldList(), false)
				.ExecuteNonQuery(new SerializationItemCollection());
		}

		/// <summary>
		/// First time database initialization.
		/// </summary>
		/// <param name="database">The database.</param>
		/// <param name="databaseRaw">Raw bytes of database file.</param>
		/// <param name="init">Initialization callback.</param>
		/// <returns>Path to the database file.</returns>
		public static string FirstTimeInit(this Database database, byte[] databaseRaw, Action<Database> init = null)
		{
			if (database == null)
				throw new ArgumentNullException(nameof(database));

			if (databaseRaw == null)
				throw new ArgumentNullException(nameof(databaseRaw));

			var conStr = new DbConnectionStringBuilder
			{
				ConnectionString = database.ConnectionString
			};

			const string key = "Data Source";

			var dbFile = (string)conStr.Cast<KeyValuePair<string, object>>().ToDictionary(StringComparer.InvariantCultureIgnoreCase).TryGetValue(key);

			if (dbFile == null)
				return null;

			dbFile = dbFile.ToFullPathIfNeed();

			conStr[key] = dbFile;
			database.ConnectionString = conStr.ToString();

			dbFile.CreateDirIfNotExists();

			if (!File.Exists(dbFile))
			{
				databaseRaw.Save(dbFile);
				database.UpdateDatabaseWalMode();

				init?.Invoke(database);
			}

			return dbFile;
		}

		public static void UpdateDatabaseWalMode(this Database database)
		{
			if (database == null)
				throw new ArgumentNullException(nameof(database));

			var walQuery = Query.Execute("PRAGMA journal_mode=WAL;");
			var walCmd = database.GetCommand(walQuery, null, new FieldList(), new FieldList(), false);
			database.Execute(walCmd, new SerializationItemCollection(), false);
		}
	}
}