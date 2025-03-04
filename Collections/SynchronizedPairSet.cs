namespace Ecng.Collections;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a thread-safe collection that maintains key/value pairs and allows bidirectional lookups.
/// </summary>
[Serializable]
public class SynchronizedPairSet<TKey, TValue> : SynchronizedKeyedCollection<TKey, TValue>
{
	#region Private Fields

	private readonly Dictionary<TValue, TKey> _values;

	#endregion

	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedPairSet{TKey, TValue}"/> class.
	/// </summary>
	public SynchronizedPairSet()
	{
		_values = [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedPairSet{TKey, TValue}"/> class with a specified comparer for keys.
	/// </summary>
	/// <param name="comparer">An equality comparer for keys.</param>
	public SynchronizedPairSet(IEqualityComparer<TKey> comparer)
		: base(comparer)
	{
		_values = [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedPairSet{TKey, TValue}"/> class with specified comparers for keys and values.
	/// </summary>
	/// <param name="keyComparer">An equality comparer for keys.</param>
	/// <param name="valueComparer">An equality comparer for values.</param>
	public SynchronizedPairSet(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
		: base(keyComparer)
	{
		_values = new Dictionary<TValue, TKey>(valueComparer);
	}

	/// <summary>
	/// Adds the specified key and value to the collection.
	/// </summary>
	/// <param name="key">The key to insert.</param>
	/// <param name="value">The value to insert.</param>
	public override void Add(TKey key, TValue value)
	{
		lock (SyncRoot)
			base.Add(key, value);
	}

	#region Item

	/// <summary>
	/// Gets the key associated with the specified value.
	/// </summary>
	/// <param name="value">The value for which to retrieve the key.</param>
	/// <returns>The key corresponding to the specified value.</returns>
	public TKey this[TValue value]
	{
		get
		{
			lock (SyncRoot)
				return _values[value];
		}
	}

	#endregion

	#region GetKey

	/// <summary>
	/// Retrieves the key associated with the specified value.
	/// </summary>
	/// <param name="value">The value whose key is requested.</param>
	/// <returns>The key associated with the given value.</returns>
	public TKey GetKey(TValue value)
	{
		return this[value];
	}

	#endregion

	#region GetValue

	/// <summary>
	/// Retrieves the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key whose value is requested.</param>
	/// <returns>The value associated with the given key.</returns>
	public TValue GetValue(TKey key)
	{
		return base[key];
	}

	#endregion

	/// <summary>
	/// Removes the old pair for the specified key and adds the new key/value pair. Raises an event when setting.
	/// </summary>
	/// <param name="key">The key to set.</param>
	/// <param name="value">The value to associate with the key.</param>
	public void SetKey(TKey key, TValue value)
	{
		lock (SyncRoot)
		{
			Remove(key);

			Add(key, value);
			OnSetting(key, value);
		}
	}

	#region SetValue

	/// <summary>
	/// Updates the value associated with a key.
	/// </summary>
	/// <param name="key">The key to update.</param>
	/// <param name="value">The new value.</param>
	public void SetValue(TKey key, TValue value)
	{
		lock (SyncRoot)
			this[key] = value;
	}

	#endregion

	/// <summary>
	/// Determines whether a key/value pair can be added based on whether the value is already present.
	/// </summary>
	/// <param name="key">The key to be added.</param>
	/// <param name="value">The value to be added.</param>
	/// <returns><c>true</c> if the value is not already contained; otherwise <c>false</c>.</returns>
	protected override bool CanAdd(TKey key, TValue value)
	{
		return !_values.ContainsKey(value);
	}

	#region SynchronizedKeyedCollection<TKey, TValue> Members

	/// <summary>
	/// Called when an item is being added. Adds the value/key mapping to the internal dictionary.
	/// </summary>
	/// <param name="key">The key being added.</param>
	/// <param name="value">The value being added.</param>
	protected override void OnAdding(TKey key, TValue value)
	{
		_values.Add(value, key);
	}

	/// <summary>
	/// Called when an existing item is being set. Updates the value/key mapping in the internal dictionary.
	/// </summary>
	/// <param name="key">The key being set.</param>
	/// <param name="value">The value being set.</param>
	protected override void OnSetting(TKey key, TValue value)
	{
		_values[value] = key;
	}

	/// <summary>
	/// Called when the collection is being cleared. Clears the internal dictionary.
	/// </summary>
	protected override void OnClearing()
	{
		_values.Clear();
	}

	/// <summary>
	/// Called when an item is being removed. Removes the value/key mapping from the internal dictionary.
	/// </summary>
	/// <param name="key">The key being removed.</param>
	/// <param name="value">The value being removed.</param>
	protected override void OnRemoving(TKey key, TValue value)
	{
		_values.Remove(value);
	}

	#endregion

	/// <summary>
	/// Attempts to add a new key/value pair if neither key nor value is already present.
	/// </summary>
	/// <param name="key">The key to add.</param>
	/// <param name="value">The value to add.</param>
	/// <returns><c>true</c> if the pair was added; otherwise <c>false</c>.</returns>
	public bool TryAdd(TKey key, TValue value)
	{
		lock (SyncRoot)
		{
			if (ContainsKey(key) || _values.ContainsKey(value))
				return false;

			Add(key, value);
			return true;
		}
	}

	/// <summary>
	/// Attempts to get a key for the specified value.
	/// </summary>
	/// <param name="value">The value whose key is requested.</param>
	/// <returns>The key if found; otherwise the default.</returns>
	public TKey TryGetKey(TValue value)
	{
		lock (SyncRoot)
			return _values.TryGetValue(value);
	}

	/// <summary>
	/// Attempts to get a key for the specified value, returning a boolean indicating success.
	/// </summary>
	/// <param name="value">The value whose key is requested.</param>
	/// <param name="key">When this method returns, contains the key if found.</param>
	/// <returns><c>true</c> if the key was found; otherwise <c>false</c>.</returns>
	public bool TryGetKey(TValue value, out TKey key)
	{
		lock (SyncRoot)
			return _values.TryGetValue(value, out key);
	}

	/// <summary>
	/// Attempts to get and remove the key associated with the specified value.
	/// </summary>
	/// <param name="value">The value for which to retrieve and remove the key.</param>
	/// <param name="key">When this method returns, contains the removed key if found.</param>
	/// <returns><c>true</c> if the key was removed successfully; otherwise <c>false</c>.</returns>
	public bool TryGetKeyAndRemove(TValue value, out TKey key)
	{
		lock (SyncRoot)
		{
			if (!_values.TryGetAndRemove(value, out key))
				return false;

			return base.Remove(key);
		}
	}

	/// <summary>
	/// Removes the element with the specified value if it is found.
	/// </summary>
	/// <param name="value">The value to remove.</param>
	/// <returns><c>true</c> if the item was removed; otherwise <c>false</c>.</returns>
	public bool RemoveByValue(TValue value)
	{
		lock (SyncRoot)
			return _values.ContainsKey(value) && Remove(_values[value]);
	}

	/// <summary>
	/// Determines whether the collection contains an entry with the specified value.
	/// </summary>
	/// <param name="value">The value to locate.</param>
	/// <returns><c>true</c> if the value is found; otherwise <c>false</c>.</returns>
	public bool ContainsValue(TValue value)
	{
		lock (SyncRoot)
			return _values.ContainsKey(value);
	}
}