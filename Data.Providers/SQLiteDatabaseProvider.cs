namespace Ecng.Data.Providers
{
	using System;
	using System.Data.Common;
	using System.Diagnostics;

	using Ecng.Common;
	using Ecng.Data.Providers.Properties;
	using Ecng.Reflection;
	using Ecng.Serialization;

	public class SQLiteDatabaseProvider : DatabaseProvider
	{
		private static readonly DbProviderFactory _factory;

		static SQLiteDatabaseProvider()
		{
			try
			{
				(Environment.Is64BitProcess ? Resources.SQLite64 : Resources.SQLite32).Save("System.Data.SQLite.dll");

				_factory = "System.Data.SQLite.SQLiteFactory, System.Data.SQLite"
					.To<Type>()
					.GetValue<VoidType, DbProviderFactory>("Instance", null);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex);
			}
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