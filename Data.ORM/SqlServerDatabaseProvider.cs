namespace Ecng.Data;

using Microsoft.Data.SqlClient;

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