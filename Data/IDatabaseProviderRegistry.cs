namespace Ecng.Data;

/// <summary>
/// The interface for database provider registry.
/// </summary>
public interface IDatabaseProviderRegistry
{
	/// <summary>
	/// The list of available providers.
	/// </summary>
	string[] Providers { get; }

	/// <summary>
	/// Verify the connection.
	/// </summary>
	/// <param name="connection"><see cref="DatabaseConnectionPair"/></param>
	void Verify(DatabaseConnectionPair connection);
}