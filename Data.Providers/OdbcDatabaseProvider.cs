namespace Ecng.Data.Providers
{
	using System.Data.Common;
	using System.Data.Odbc;

	public class OdbcDatabaseProvider : DatabaseProvider
	{
		public OdbcDatabaseProvider()
			: base(OdbcFactory.Instance, new OdbcRenderer())
		{
		}

		protected override void DeriveParameters(DbCommand command)
		{
			OdbcCommandBuilder.DeriveParameters((OdbcCommand)command);
		}
	}
}
