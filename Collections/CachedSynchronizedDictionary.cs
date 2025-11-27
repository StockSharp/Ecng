namespace Ecng.Collections;

using System.Collections.Generic;

/// <summary>
/// Represents a thread-safe dictionary with cached keys, values, and key-value pairs for improved performance.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public class CachedSynchronizedDictionary<TKey, TValue> : SynchronizedDictionary<TKey, TValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedDictionary{TKey, TValue}"/> class.
	/// </summary>
	public CachedSynchronizedDictionary()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedDictionary{TKey, TValue}"/> class with the specified initial capacity.
	/// </summary>
	/// <param name="capacity">The initial number of elements that the dictionary can contain.</param>
	public CachedSynchronizedDictionary(int capacity)
		: base(capacity)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedDictionary{TKey, TValue}"/> class with the specified equality comparer.
	/// </summary>
	/// <param name="comparer">The equality comparer to use when comparing keys.</param>
	public CachedSynchronizedDictionary(IEqualityComparer<TKey> comparer)
		: base(comparer)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedDictionary{TKey, TValue}"/> class with the specified initial capacity and equality comparer.
	/// </summary>
	/// <param name="capacity">The initial number of elements that the dictionary can contain.</param>
	/// <param name="comparer">The equality comparer to use when comparing keys.</param>
	public CachedSynchronizedDictionary(int capacity, IEqualityComparer<TKey> comparer)
		: base(capacity, comparer)
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
			using (SyncRoot.EnterScope())
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
			using (SyncRoot.EnterScope())
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
			using (SyncRoot.EnterScope())
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
			using (SyncRoot.EnterScope())
			{
				var isKey = false;

				if (_cachedKeys != null && !ContainsKey(key))
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
		using (SyncRoot.EnterScope())
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
		using (SyncRoot.EnterScope())
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
		using (SyncRoot.EnterScope())
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
