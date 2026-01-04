namespace Ecng.Common;

using System.Threading;

/// <summary>
/// Defines an object that provides synchronization capabilities.
/// </summary>
public interface ISynchronizable
{
	/// <summary>
	/// Gets the synchronization object used for thread-safe operations.
	/// </summary>
	Lock SyncRoot { get; }
}
