namespace Ecng.Data.Providers
{
	using System.Data.Common;

	using Npgsql;

	public class PostgreSqlDatabaseProvider : DatabaseProvider
	{
		public PostgreSqlDatabaseProvider()
			: base(NpgsqlFactory.Instance, new PostgreSqlRenderer())
		{
		}

		protected override void DeriveParameters(DbCommand command)
		{
			NpgsqlCommandBuilder.DeriveParameters((NpgsqlCommand)command);
		}
	}
}