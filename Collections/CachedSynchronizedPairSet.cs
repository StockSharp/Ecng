namespace Ecng.Collections;

using System.Collections.Generic;

/// <summary>
/// Represents a thread-safe set of key-value pairs with cached keys, values, and pairs for improved performance.
/// </summary>
/// <typeparam name="TKey">The type of keys in the set.</typeparam>
/// <typeparam name="TValue">The type of values in the set.</typeparam>
public class CachedSynchronizedPairSet<TKey, TValue> : SynchronizedPairSet<TKey, TValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedPairSet{TKey, TValue}"/> class.
	/// </summary>
	public CachedSynchronizedPairSet()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedPairSet{TKey, TValue}"/> class with the specified key comparer.
	/// </summary>
	/// <param name="comparer">The equality comparer to use when comparing keys.</param>
	public CachedSynchronizedPairSet(IEqualityComparer<TKey> comparer)
		: base(comparer)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CachedSynchronizedPairSet{TKey, TValue}"/> class with the specified key and value comparers.
	/// </summary>
	/// <param name="keyComparer">The equality comparer to use when comparing keys.</param>
	/// <param name="valueComparer">The equality comparer to use when comparing values.</param>
	public CachedSynchronizedPairSet(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
		: base(keyComparer, valueComparer)
	{
	}

	private TKey[] _cachedKeys;

	/// <summary>
	/// Gets an array of cached keys from the set.
	/// </summary>
	/// <remarks>
	/// The keys are cached for performance and are reset when the set is modified.
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
	/// Gets an array of cached values from the set.
	/// </summary>
	/// <remarks>
	/// The values are cached for performance and are reset when the set is modified.
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
	/// Gets an array of cached key-value pairs from the set.
	/// </summary>
	/// <remarks>
	/// The key-value pairs are cached for performance and are reset when the set is modified.
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
	/// Adds the specified key and value to the set.
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
	/// Removes the value with the specified key from the set.
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
	/// Removes all keys and values from the set.
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
	/// Resets the cached keys, values, and key-value pairs when the set is modified.
	/// </summary>
	/// <param name="isKey">Indicates whether the cache reset is due to a key modification.</param>
	protected virtual void OnResetCache(bool isKey)
	{
		_cachedKeys = null;
		_cachedValues = null;
		_cachedPairs = null;
	}
}