namespace Ecng.Data;

/// <summary>
/// Factory for creating database connections and table accessors.
/// </summary>
public interface IDatabaseProvider
{
	/// <summary>
	/// Creates a database connection from the specified connection settings.
	/// </summary>
	/// <param name="pair">Database connection settings.</param>
	/// <returns>A database connection instance.</returns>
	IDatabaseConnection CreateConnection(DatabaseConnectionPair pair);

	/// <summary>
	/// Gets a table accessor for the specified table.
	/// </summary>
	/// <param name="connection">Database connection.</param>
	/// <param name="tableName">Table name.</param>
	/// <returns>Table accessor with DDL and DML operations.</returns>
	IDatabaseTable GetTable(IDatabaseConnection connection, string tableName);
}
