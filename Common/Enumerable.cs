namespace Ecng.Common;

#region Using Directives

using System.Collections;
using System.Collections.Generic;

#endregion

/// <summary>
/// Represents an abstract enumerable collection of elements.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public abstract class Enumerable<T> : IEnumerable<T>
{
	#region IEnumerable<T> Members

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator that can be used to iterate through the collection.</returns>
	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion

	#region IEnumerable Members

	/// <summary>
	/// Returns an enumerator that iterates through a collection.
	/// </summary>
	/// <returns>
	/// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
	/// </returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion

	/// <summary>
	/// When overridden in a derived class, returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator that can be used to iterate through the collection.</returns>
	protected abstract IEnumerator<T> GetEnumerator();
}