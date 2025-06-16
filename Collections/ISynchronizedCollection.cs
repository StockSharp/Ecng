namespace Ecng.Collections;

using System.Collections.Generic;
using Ecng.Common;

/// <summary>
/// Represents a collection that can be synchronized using a <see cref="SyncObject"/>.
/// </summary>
public interface ISynchronizedCollection
{
	/// <summary>
	/// Gets the synchronization object used for thread-safe operations.
	/// </summary>
	SyncObject SyncRoot { get; }
}

/// <summary>
/// Represents a generic synchronized collection.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public interface ISynchronizedCollection<T> : ISynchronizedCollection, ICollection<T>
{
}