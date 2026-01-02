namespace Ecng.Common;

/// <summary>
/// Represents an object that supports committing changes.
/// </summary>
public interface ICommitable
{
	/// <summary>
	/// Commits the changes.
	/// </summary>
	void Commit();
}
