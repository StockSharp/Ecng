namespace Ecng.Data.Providers
{
	using System.Data.Common;
	using System.Data.Odbc;

	public class OdbcDatabaseProvider<TRenderer> : DatabaseProvider
		where TRenderer : SqlRenderer, new()
	{
		public OdbcDatabaseProvider()
			: base(OdbcFactory.Instance, new TRenderer())
		{
		}

		protected override void DeriveParameters(DbCommand command)
		{
			OdbcCommandBuilder.DeriveParameters((OdbcCommand)command);
		}
	}
}
