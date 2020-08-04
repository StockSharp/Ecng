namespace Ecng.Data.Providers
{
	using System;
	using System.Data.Common;
	using System.Diagnostics;

	using Ecng.Common;
	using Ecng.Reflection;

	public class SQLiteDatabaseProvider : DatabaseProvider
	{
		private static readonly DbProviderFactory _factory;

		public static bool IsValid => _factory != null;

		static SQLiteDatabaseProvider()
		{
			try
			{
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