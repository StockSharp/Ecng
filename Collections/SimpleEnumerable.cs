namespace Ecng.Collections;

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Represents a simple implementation of an enumerable that uses a factory function to create its enumerator.
/// </summary>
/// <typeparam name="T">The type of elements in the enumerable.</typeparam>
public class SimpleEnumerable<T>(Func<IEnumerator<T>> createEnumerator) : IEnumerable<T>
{
	private readonly Func<IEnumerator<T>> _createEnumerator = createEnumerator ?? throw new ArgumentNullException(nameof(createEnumerator));

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An <see cref="IEnumerator{T}"/> for the collection.</returns>
	public IEnumerator<T> GetEnumerator()
	{
		return _createEnumerator();
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection (non-generic version).
	/// </summary>
	/// <returns>An <see cref="IEnumerator"/> for the collection.</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}