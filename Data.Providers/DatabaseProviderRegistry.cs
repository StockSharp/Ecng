namespace Ecng.Data.Providers
{
	using System;
	using System.Data.Common;
	using System.Linq;
	using System.Diagnostics;

	using Ecng.Collections;
	using Ecng.Common;

	public static class DatabaseProviderRegistry
	{
		private static readonly SynchronizedList<DatabaseProvider> _providers = new SynchronizedList<DatabaseProvider>();

		static DatabaseProviderRegistry()
		{
			AddProvider<SqlServerDatabaseProvider>();
			AddProvider<SQLiteDatabaseProvider>();
#if NETFRAMEWORK
			AddProvider<OdbcDatabaseProvider>();
			AddProvider<JetDatabaseProvider>();
#endif
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

		public static DatabaseProvider GetProviderBySystemName(string name)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			var row = DbProviderFactories.GetFactoryClasses().Select("InvariantName = '{0}'".Put(name)).FirstOrDefault();
			
			if (row == null)
				throw new ArgumentException("Provider with name '{0}' doesn't register.".Put(name), nameof(name));
			
			var type = row[3].To<Type>();
			return _providers.First(p => p.Factory.GetType() == type);
		}
	}
}