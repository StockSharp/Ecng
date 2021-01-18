namespace Ecng.Data.Providers
{
	using System;
	using System.Diagnostics;

	using Ecng.Collections;

	public static class DatabaseProviderRegistry
	{
		private static readonly SynchronizedList<DatabaseProvider> _providers = new SynchronizedList<DatabaseProvider>();

		static DatabaseProviderRegistry()
		{
			AddProvider<SqlServerDatabaseProvider>();
			AddProvider<SQLiteDatabaseProvider>();
		}

		private static void AddProvider<TProvider>()
			where TProvider : DatabaseProvider, new()
		{
			try
			{
				_providers.Add(new TProvider());
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex);
			}
		}

		public static ISynchronizedCollection<DatabaseProvider> Providers => _providers;
	}
}