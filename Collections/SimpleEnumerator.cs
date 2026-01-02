namespace Ecng.Collections;

using System.Collections;
using System.Collections.Generic;

using Ecng.Common;

/// <summary>
/// Provides an abstract base class for implementing a simple enumerator with disposable functionality.
/// </summary>
/// <typeparam name="T">The type of elements being enumerated.</typeparam>
[System.Obsolete("Use yield return instead.")]
public abstract class SimpleEnumerator<T> : Disposable, IEnumerator<T>
{
	/// <summary>
	/// Advances the enumerator to the next element of the collection.
	/// </summary>
	/// <returns>True if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
	public abstract bool MoveNext();

	/// <summary>
	/// Sets the enumerator to its initial position, which is before the first element in the collection.
	/// </summary>
	/// <remarks>This implementation does nothing by default and can be overridden by derived classes.</remarks>
	public virtual void Reset()
	{
	}

	/// <summary>
	/// Gets the current element in the collection.
	/// </summary>
	public T Current { get; protected set; }

	/// <summary>
	/// Gets the current element in the collection as an object (non-generic version).
	/// </summary>
	object IEnumerator.Current => Current;
}