namespace Ecng.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a collection adapter that converts between <see cref="ICollection{TSource}"/> and <see cref="ICollection{TTarget}"/> using duck typing.
/// Allows working with collections of different element types by providing conversion functions.
/// </summary>
/// <typeparam name="TSource">The source element type.</typeparam>
/// <typeparam name="TTarget">The target element type.</typeparam>
public class DuckTypingCollection<TSource, TTarget> : ICollection<TTarget>
{
	private readonly ICollection<TSource> _source;
	private readonly Func<TSource, TTarget> _sourceToTarget;
	private readonly Func<TTarget, TSource> _targetToSource;

	/// <summary>
	/// Initializes a new instance of the <see cref="DuckTypingCollection{TSource, TTarget}"/> class.
	/// </summary>
	/// <param name="source">The source collection to wrap.</param>
	/// <param name="sourceToTarget">Function to convert from source type to target type.</param>
	/// <param name="targetToSource">Function to convert from target type to source type.</param>
	/// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
	public DuckTypingCollection(ICollection<TSource> source, Func<TSource, TTarget> sourceToTarget, Func<TTarget, TSource> targetToSource)
	{
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_sourceToTarget = sourceToTarget ?? throw new ArgumentNullException(nameof(sourceToTarget));
		_targetToSource = targetToSource ?? throw new ArgumentNullException(nameof(targetToSource));
	}

	/// <summary>
	/// Gets the wrapped source collection.
	/// </summary>
	public ICollection<TSource> Source => _source;

	/// <summary>
	/// Gets the number of elements contained in the collection.
	/// </summary>
	public int Count => _source.Count;

	/// <summary>
	/// Gets a value indicating whether the collection is read-only.
	/// </summary>
	public bool IsReadOnly => _source.IsReadOnly;

	/// <summary>
	/// Adds an item to the collection.
	/// </summary>
	/// <param name="item">The item to add.</param>
	public void Add(TTarget item)
	{
		_source.Add(_targetToSource(item));
	}

	/// <summary>
	/// Removes all items from the collection.
	/// </summary>
	public void Clear()
	{
		_source.Clear();
	}

	/// <summary>
	/// Determines whether the collection contains a specific item.
	/// </summary>
	/// <param name="item">The item to locate.</param>
	/// <returns>true if the item is found; otherwise, false.</returns>
	public bool Contains(TTarget item)
	{
		return _source.Contains(_targetToSource(item));
	}

	/// <summary>
	/// Copies the elements of the collection to an array, starting at a particular array index.
	/// </summary>
	/// <param name="array">The destination array.</param>
	/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
	public void CopyTo(TTarget[] array, int arrayIndex)
	{
		if (array == null)
			throw new ArgumentNullException(nameof(array));

		if (arrayIndex < 0 || arrayIndex > array.Length)
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));

		if (array.Length - arrayIndex < Count)
			throw new ArgumentException("Destination array is not long enough.");

		int i = arrayIndex;
		foreach (var item in this)
		{
			array[i++] = item;
		}
	}

	/// <summary>
	/// Removes the first occurrence of a specific item from the collection.
	/// </summary>
	/// <param name="item">The item to remove.</param>
	/// <returns>true if the item was successfully removed; otherwise, false.</returns>
	public bool Remove(TTarget item)
	{
		return _source.Remove(_targetToSource(item));
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	public IEnumerator<TTarget> GetEnumerator()
	{
		return _source.Select(_sourceToTarget).GetEnumerator();
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
