namespace Ecng.Data.Providers
{
	using System.Data.Common;

	using FirebirdSql.Data.FirebirdClient;

	public class FirebirdDatabaseProvider : DatabaseProvider
	{
		public FirebirdDatabaseProvider()
			: base(FirebirdClientFactory.Instance, new FirebirdRenderer())
		{
		}

		protected override void DeriveParameters(DbCommand command)
		{
			FbCommandBuilder.DeriveParameters((FbCommand)command);
		}
	}
}