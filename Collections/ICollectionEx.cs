namespace Ecng.Collections;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides an extended set of methods and events for collections of a specified type.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public interface ICollectionEx<T> : ICollection<T>
{
	/// <summary>
	/// Occurs when a range of elements is added.
	/// </summary>
	event Action<IEnumerable<T>> AddedRange;

	/// <summary>
	/// Occurs when a range of elements is removed.
	/// </summary>
	event Action<IEnumerable<T>> RemovedRange;

	/// <summary>
	/// Adds a range of elements to the collection.
	/// </summary>
	/// <param name="items">The elements to add.</param>
	void AddRange(IEnumerable<T> items);

	/// <summary>
	/// Removes a range of elements from the collection.
	/// </summary>
	/// <param name="items">The elements to remove.</param>
	void RemoveRange(IEnumerable<T> items);

	/// <summary>
	/// Removes a specified number of elements starting at a certain index.
	/// </summary>
	/// <param name="index">The starting index.</param>
	/// <param name="count">The number of elements to remove.</param>
	/// <returns>The number of removed elements.</returns>
	int RemoveRange(int index, int count);
}