namespace Ecng.Serialization;

using System;

/// <summary>
/// Defines a context for batch operations that supports committing transactions and resource disposal.
/// </summary>
public interface IBatchContext : IDisposable
{
	/// <summary>
	/// Commits the current batch of operations.
	/// </summary>
	void Commit();
}