namespace Ecng.Data;

/// <summary>
/// </summary>
public interface IDatabaseProviderRegistry
{
	/// <summary>
	/// </summary>
	string[] Providers { get; }

	/// <summary>
	/// </summary>
	void Verify(DatabaseConnectionPair connection);
}