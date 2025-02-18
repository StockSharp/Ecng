namespace Ecng.Data;

using LinqToDB;

/// <summary>
/// Provides a registry for database providers and verifies database connections.
/// </summary>
public class DatabaseProviderRegistry : IDatabaseProviderRegistry
{
	/// <summary>
	/// Gets the list of available database providers.
	/// </summary>
	public virtual string[] Providers { get; } =
	[
		ProviderName.SqlServer,
		ProviderName.SQLite,
		ProviderName.MySql,
	];

	/// <summary>
	/// Verifies the specified database connection by attempting to open a connection.
	/// </summary>
	/// <param name="connection">The database connection pair, including provider and connection string.</param>
	public virtual void Verify(DatabaseConnectionPair connection)
	{
		using var db = connection.CreateConnection();
		using var conn = db.DataProvider.CreateConnection(db.ConnectionString);
		conn.Open();
	}
}