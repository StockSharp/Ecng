namespace Ecng.Data.Providers
{
	using System;
	using System.Data.Common;
	using System.IO;

	using Ecng.Common;
	using Ecng.Data.Providers.Properties;
	using Ecng.Reflection;

	public class SQLiteDatabaseProvider : DatabaseProvider
	{
		private static readonly DbProviderFactory _factory;

		static SQLiteDatabaseProvider()
		{
			File.WriteAllBytes("System.Data.SQLite.dll", Environment.Is64BitProcess ? Resources.SQLite64 : Resources.SQLite32);

			_factory = "System.Data.SQLite.SQLiteFactory, System.Data.SQLite"
				.To<Type>()
				.GetValue<VoidType, DbProviderFactory>("Instance", null);
		}

		public SQLiteDatabaseProvider()
			: base(_factory, new SQLiteRenderer())
		{
		}

		protected override void DeriveParameters(DbCommand command)
		{
			throw new NotSupportedException();
		}
	}
}