namespace Ecng.Collections;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a thread-safe ordered dictionary with cached keys, values, and key-value pairs for improved performance.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public class CachedSynchronizedOrderedDictionary<TKey, TValue> : SynchronizedOrderedDictionary<TKey, TValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedOrderedDictionary{TKey, TValue}"/> class.
	/// </summary>
	public CachedSynchronizedOrderedDictionary()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedOrderedDictionary{TKey, TValue}"/> class with the specified key comparer.
	/// </summary>
	/// <param name="comparer">The comparer to use when comparing keys.</param>
	public CachedSynchronizedOrderedDictionary(IComparer<TKey> comparer)
		: base(comparer)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedOrderedDictionary{TKey, TValue}"/> class with the specified key comparison function.
	/// </summary>
	/// <param name="comparer">The function to use when comparing keys.</param>
	public CachedSynchronizedOrderedDictionary(Func<TKey, TKey, int> comparer)
		: base(comparer)
	{
	}

	private TKey[] _cachedKeys;

	/// <summary>
	/// Gets an array of cached keys from the dictionary.
	/// </summary>
	/// <remarks>
	/// The keys are cached for performance and are reset when the dictionary is modified.
	/// </remarks>
	public TKey[] CachedKeys
	{
		get
		{
			lock (SyncRoot)
				return _cachedKeys ??= [.. Keys];
		}
	}

	private TValue[] _cachedValues;

	/// <summary>
	/// Gets an array of cached values from the dictionary.
	/// </summary>
	/// <remarks>
	/// The values are cached for performance and are reset when the dictionary is modified.
	/// </remarks>
	public TValue[] CachedValues
	{
		get
		{
			lock (SyncRoot)
				return _cachedValues ??= [.. Values];
		}
	}

	private KeyValuePair<TKey, TValue>[] _cachedPairs;

	/// <summary>
	/// Gets an array of cached key-value pairs from the dictionary.
	/// </summary>
	/// <remarks>
	/// The key-value pairs are cached for performance and are reset when the dictionary is modified.
	/// </remarks>
	public KeyValuePair<TKey, TValue>[] CachedPairs
	{
		get
		{
			lock (SyncRoot)
				return _cachedPairs ??= [.. this];
		}
	}

	/// <summary>
	/// Gets or sets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key of the value to get or set.</param>
	/// <returns>The value associated with the specified key.</returns>
	public override TValue this[TKey key]
	{
		set
		{
			lock (SyncRoot)
			{
				var isKey = false;

				if (!ContainsKey(key))
					isKey = true;

				OnResetCache(isKey);

				base[key] = value;
			}
		}
	}

	/// <summary>
	/// Adds the specified key and value to the dictionary.
	/// </summary>
	/// <param name="key">The key of the element to add.</param>
	/// <param name="value">The value of the element to add.</param>
	public override void Add(TKey key, TValue value)
	{
		lock (SyncRoot)
		{
			base.Add(key, value);

			OnResetCache(true);
		}
	}

	/// <summary>
	/// Removes the value with the specified key from the dictionary.
	/// </summary>
	/// <param name="key">The key of the element to remove.</param>
	/// <returns><c>true</c> if the element is successfully found and removed; otherwise, <c>false</c>.</returns>
	public override bool Remove(TKey key)
	{
		lock (SyncRoot)
		{
			if (base.Remove(key))
			{
				OnResetCache(true);

				return true;
			}

			return false;
		}
	}

	/// <summary>
	/// Removes all keys and values from the dictionary.
	/// </summary>
	public override void Clear()
	{
		lock (SyncRoot)
		{
			base.Clear();

			OnResetCache(true);
		}
	}

	/// <summary>
	/// Resets the cached keys, values, and key-value pairs when the dictionary is modified.
	/// </summary>
	/// <param name="isKey">Indicates whether the cache reset is due to a key modification.</param>
	protected virtual void OnResetCache(bool isKey)
	{
		_cachedKeys = null;
		_cachedValues = null;
		_cachedPairs = null;
	}
}