namespace Ecng.Serialization;

/// <summary>
/// Represents a database storage transaction.
/// </summary>
public interface IStorageTransaction : IDisposable
{
	/// <summary>
	/// Commits all changes made within this transaction.
	/// </summary>
	void Commit();
}