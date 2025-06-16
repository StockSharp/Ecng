namespace Ecng.Collections;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a collection of key-value pairs that supports bidirectional lookup by key or value.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TValue">The type of values in the collection.</typeparam>
[Serializable]
public sealed class PairSet<TKey, TValue> : KeyedCollection<TKey, TValue>
{
	#region Private Fields

	private readonly Dictionary<TValue, TKey> _values;

	#endregion

	/// <summary>
	/// Initializes a new instance of the <see cref="PairSet{TKey, TValue}"/> class with default comparers.
	/// </summary>
	public PairSet()
	{
		_values = [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PairSet{TKey, TValue}"/> class with a specified key comparer.
	/// </summary>
	/// <param name="comparer">The equality comparer for keys, or null to use the default comparer.</param>
	public PairSet(IEqualityComparer<TKey> comparer)
		: base(comparer)
	{
		_values = [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PairSet{TKey, TValue}"/> class with specified key and value comparers.
	/// </summary>
	/// <param name="keyComparer">The equality comparer for keys, or null to use the default comparer.</param>
	/// <param name="valueComparer">The equality comparer for values, or null to use the default comparer.</param>
	public PairSet(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
		: base(keyComparer)
	{
		_values = new Dictionary<TValue, TKey>(valueComparer);
	}

	#region Item

	/// <summary>
	/// Gets the key associated with the specified value.
	/// </summary>
	/// <param name="value">The value to look up.</param>
	/// <returns>The key associated with the specified value.</returns>
	/// <exception cref="KeyNotFoundException">Thrown when the value is not found in the collection.</exception>
	public TKey this[TValue value] => _values[value];

	#endregion
	
	#region GetKey

	/// <summary>
	/// Retrieves the key associated with the specified value.
	/// </summary>
	/// <param name="value">The value to look up.</param>
	/// <returns>The key associated with the specified value.</returns>
	/// <exception cref="KeyNotFoundException">Thrown when the value is not found in the collection.</exception>
	public TKey GetKey(TValue value)
	{
		return this[value];
	}

	#endregion

	#region GetValue

	/// <summary>
	/// Retrieves the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key to look up.</param>
	/// <returns>The value associated with the specified key.</returns>
	/// <exception cref="KeyNotFoundException">Thrown when the key is not found in the collection.</exception>
	public TValue GetValue(TKey key)
	{
		return base[key];
	}

	#endregion

	/// <summary>
	/// Sets a new key for an existing value, replacing any previous association.
	/// </summary>
	/// <param name="value">The value to associate with the new key.</param>
	/// <param name="key">The new key to associate with the value.</param>
	public void SetKey(TValue value, TKey key)
	{
		RemoveByValue(value);
		Add(key, value);
	}

	#region SetValue

	/// <summary>
	/// Sets a new value for an existing key, updating the internal value-to-key mapping.
	/// </summary>
	/// <param name="key">The key whose value is to be updated.</param>
	/// <param name="value">The new value to associate with the key.</param>
	public void SetValue(TKey key, TValue value)
	{
		base[key] = value;
	}

	#endregion

	/// <summary>
	/// Determines whether a key-value pair can be added to the collection.
	/// </summary>
	/// <param name="key">The key to check.</param>
	/// <param name="value">The value to check.</param>
	/// <returns>True if the value is not already associated with a key; otherwise, false.</returns>
	protected override bool CanAdd(TKey key, TValue value)
	{
		return !_values.ContainsKey(value);
	}

	#region KeyedCollection<TKey, TValue> Members

	/// <summary>
	/// Called before adding a key-value pair to the collection, updating the value-to-key mapping.
	/// </summary>
	/// <param name="key">The key being added.</param>
	/// <param name="value">The value being added.</param>
	protected override void OnAdding(TKey key, TValue value)
	{
		_values.Add(value, key);
	}

	/// <summary>
	/// Called before setting a new value for an existing key, updating the value-to-key mapping.
	/// </summary>
	/// <param name="key">The key being updated.</param>
	/// <param name="value">The new value to set.</param>
	protected override void OnSetting(TKey key, TValue value)
	{
		_values[value] = key;
	}

	/// <summary>
	/// Called before clearing the collection, clearing the value-to-key mapping.
	/// </summary>
	protected override void OnClearing()
	{
		_values.Clear();
	}

	/// <summary>
	/// Called before removing a key-value pair, updating the value-to-key mapping.
	/// </summary>
	/// <param name="key">The key being removed.</param>
	/// <param name="value">The value being removed.</param>
	protected override void OnRemoving(TKey key, TValue value)
	{
		_values.Remove(value);
	}

	#endregion

	/// <summary>
	/// Attempts to retrieve the key associated with a specified value.
	/// </summary>
	/// <param name="value">The value to look up.</param>
	/// <param name="key">When this method returns, contains the key if found; otherwise, the default value of <typeparamref name="TKey"/>.</param>
	/// <returns>True if the value was found; otherwise, false.</returns>
	public bool TryGetKey(TValue value, out TKey key)
	{
		return _values.TryGetValue(value, out key);
	}

	/// <summary>
	/// Attempts to add a key-value pair to the collection if neither the key nor value already exists.
	/// </summary>
	/// <param name="key">The key to add.</param>
	/// <param name="value">The value to add.</param>
	/// <returns>True if the pair was added; false if either the key or value already exists.</returns>
	public bool TryAdd(TKey key, TValue value)
	{
		if (ContainsKey(key) || _values.ContainsKey(value))
			return false;

		Add(key, value);
		return true;
	}

	/// <summary>
	/// Removes a key-value pair from the collection based on the specified value.
	/// </summary>
	/// <param name="value">The value to remove.</param>
	/// <returns>True if the value was found and removed; otherwise, false.</returns>
	public bool RemoveByValue(TValue value)
	{
		return _values.ContainsKey(value) && Remove(_values[value]);
	}

	/// <summary>
	/// Determines whether the collection contains a specific value.
	/// </summary>
	/// <param name="value">The value to check.</param>
	/// <returns>True if the value exists in the collection; otherwise, false.</returns>
	public bool ContainsValue(TValue value)
	{
		return _values.ContainsKey(value);
	}
}