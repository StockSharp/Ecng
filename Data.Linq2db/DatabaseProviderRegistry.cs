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
}