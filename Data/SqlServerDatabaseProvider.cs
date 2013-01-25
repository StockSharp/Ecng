namespace Ecng.Data
{
	using System.Data.Common;
	using System.Data.SqlClient;

	public class SqlServerDatabaseProvider : DatabaseProvider
	{
		public SqlServerDatabaseProvider()
			: base(SqlClientFactory.Instance, new SqlServerRenderer())
		{
		}

		protected internal override void DeriveParameters(DbCommand command)
		{
			SqlCommandBuilder.DeriveParameters((SqlCommand)command);
		}
	}
}