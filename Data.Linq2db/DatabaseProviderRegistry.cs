namespace Ecng.Data;

using LinqToDB;

public class DatabaseProviderRegistry : IDatabaseProviderRegistry
{
	public virtual string[] Providers { get; } =
	[
		ProviderName.SqlServer,
		ProviderName.SQLite,
		ProviderName.MySql,
	];

	public virtual void Verify(DatabaseConnectionPair connection)
	{
		using var db = connection.CreateConnection();
		using var conn = db.DataProvider.CreateConnection(db.ConnectionString);
		conn.Open();
	}
}