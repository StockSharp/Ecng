namespace Ecng.Data.Providers
{
	using System.Data.Common;
	using System.Data.OleDb;

	public class JetDatabaseProvider : DatabaseProvider
	{
		public JetDatabaseProvider()
			: base(OleDbFactory.Instance, new JetRenderer())
		{
		}

		protected override void DeriveParameters(DbCommand command)
		{
			OleDbCommandBuilder.DeriveParameters((OleDbCommand)command);
		}
	}
}