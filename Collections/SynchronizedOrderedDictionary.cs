namespace Ecng.Collections;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides a thread-safe dictionary with keys ordered by a specified comparer.
/// </summary>
public class SynchronizedOrderedDictionary<TKey, TValue> : SynchronizedDictionary<TKey, TValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedOrderedDictionary{TKey,TValue}"/> class using the default comparer.
	/// </summary>
	public SynchronizedOrderedDictionary()
		: base(new SortedDictionary<TKey, TValue>())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedOrderedDictionary{TKey,TValue}"/> class using the specified comparer.
	/// </summary>
	/// <param name="comparer">The comparer used to sort the keys.</param>
	public SynchronizedOrderedDictionary(IComparer<TKey> comparer)
		: base(new SortedDictionary<TKey, TValue>(comparer))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedOrderedDictionary{TKey,TValue}"/> class using a custom comparison function.
	/// </summary>
	/// <param name="comparer">A function that compares two keys and returns an integer indicating their relative order.</param>
	public SynchronizedOrderedDictionary(Func<TKey, TKey, int> comparer)
		: this(comparer.ToComparer())
	{
	}
}