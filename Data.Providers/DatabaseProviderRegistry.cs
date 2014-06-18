namespace Ecng.Data.Providers
{
	using System;
	using System.Data.Common;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	public static class DatabaseProviderRegistry
	{
		private static readonly SynchronizedList<DatabaseProvider> _providers = new SynchronizedList<DatabaseProvider>
		{
			new SqlServerDatabaseProvider(), new SQLiteDatabaseProvider(), new PostgreSqlDatabaseProvider(),
			new FirebirdDatabaseProvider(), new JetDatabaseProvider(), new PostgreSqlDatabaseProvider(),
		};

		public static ISynchronizedCollection<DatabaseProvider> Providers
		{
			get { return _providers; }
		}

		public static DatabaseProvider GetProviderBySystemName(string name)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			var row = DbProviderFactories.GetFactoryClasses().Select("InvariantName = '{0}'".Put(name)).FirstOrDefault();
			
			if (row == null)
				throw new ArgumentException("Provider with name '{0}' doesn't register.".Put(name), "name");
			
			var type = row[3].To<Type>();
			return _providers.First(p => p.Factory.GetType() == type);
		}
	}
}