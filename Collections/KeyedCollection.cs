namespace Ecng.Collections;

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Represents an abstract dictionary-backed collection that manages key-value pairs with customizable behavior.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TValue">The type of values in the collection.</typeparam>
[Serializable]
public abstract class KeyedCollection<TKey, TValue>(IDictionary<TKey, TValue> innerDictionary) : IDictionary<TKey, TValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="KeyedCollection{TKey, TValue}"/> class with a default dictionary.
	/// </summary>
	protected KeyedCollection()
		: this(new Dictionary<TKey, TValue>())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="KeyedCollection{TKey, TValue}"/> class with a specified key comparer.
	/// </summary>
	/// <param name="comparer">The equality comparer for keys, or null to use the default comparer.</param>
	protected KeyedCollection(IEqualityComparer<TKey> comparer)
		: this(new Dictionary<TKey, TValue>(comparer))
	{
	}

	/// <summary>
	/// Gets the underlying dictionary that stores the key-value pairs.
	/// </summary>
	protected IDictionary<TKey, TValue> InnerDictionary { get; } = innerDictionary ?? throw new ArgumentNullException(nameof(innerDictionary));

	/// <summary>
	/// Adds the specified key-value pair to the collection.
	/// </summary>
	/// <param name="key">The key to add.</param>
	/// <param name="value">The value to add.</param>
	/// <exception cref="ArgumentException">Thrown when the key-value pair cannot be added, as determined by <see cref="CanAdd"/>.</exception>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null and the underlying dictionary does not permit null keys.</exception>
	public virtual void Add(TKey key, TValue value)
	{
		if (!CanAdd(key, value))
			throw new ArgumentException("Cannot add the specified key-value pair.", nameof(key));

		OnAdding(key, value);
		InnerDictionary.Add(key, value);
		OnAdded(key, value);
	}

	/// <summary>
	/// Gets or sets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key whose value is to be retrieved or set.</param>
	/// <returns>The value associated with the specified key.</returns>
	/// <exception cref="KeyNotFoundException">Thrown when getting a value and the key is not found in the collection.</exception>
	/// <exception cref="ArgumentNullException">Thrown when setting a value and <paramref name="key"/> is null, if the underlying dictionary does not permit null keys.</exception>
	public virtual TValue this[TKey key]
	{
		get => InnerDictionary[key];
		set
		{
			OnSetting(key, value);
			InnerDictionary[key] = value;
			OnSetted(key, value);
		}
	}

	/// <summary>
	/// Removes all items from the collection.
	/// </summary>
	public virtual void Clear()
	{
		OnClearing();
		InnerDictionary.Clear();
		OnCleared();
	}

	/// <summary>
	/// Removes the value with the specified key from the collection.
	/// </summary>
	/// <param name="key">The key of the element to remove.</param>
	/// <returns>True if the element was found and removed; otherwise, false.</returns>
	public virtual bool Remove(TKey key)
	{
		if (InnerDictionary.TryGetValue(key, out var value))
		{
			OnRemoving(key, value);
			var retVal = InnerDictionary.Remove(key);
			OnRemoved(key, value);
			return retVal;
		}
		else
			return false;
	}

	/// <summary>
	/// Attempts to retrieve the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key to locate.</param>
	/// <param name="value">When this method returns, contains the value associated with the key if found; otherwise, the default value of <typeparamref name="TValue"/>.</param>
	/// <returns>True if the key was found; otherwise, false.</returns>
	public virtual bool TryGetValue(TKey key, out TValue value)
	{
		return InnerDictionary.TryGetValue(key, out value);
	}

	/// <summary>
	/// Gets the number of key-value pairs in the collection.
	/// </summary>
	public virtual int Count => InnerDictionary.Count;

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.</returns>
	public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		return InnerDictionary.GetEnumerator();
	}

	/// <summary>
	/// Called before adding a key-value pair to the collection.
	/// </summary>
	/// <param name="key">The key being added.</param>
	/// <param name="value">The value being added.</param>
	/// <remarks>This method is intended for derived classes to implement custom logic before an addition occurs.</remarks>
	protected virtual void OnAdding(TKey key, TValue value) { }

	/// <summary>
	/// Called after a key-value pair has been added to the collection.
	/// </summary>
	/// <param name="key">The key that was added.</param>
	/// <param name="value">The value that was added.</param>
	/// <remarks>This method is intended for derived classes to implement custom logic after an addition occurs.</remarks>
	protected virtual void OnAdded(TKey key, TValue value) { }

	/// <summary>
	/// Called before setting a new value for an existing key.
	/// </summary>
	/// <param name="key">The key being updated.</param>
	/// <param name="value">The new value to set.</param>
	/// <remarks>This method is intended for derived classes to implement custom logic before a value is updated.</remarks>
	protected virtual void OnSetting(TKey key, TValue value) { }

	/// <summary>
	/// Called after a new value has been set for an existing key.
	/// </summary>
	/// <param name="key">The key that was updated.</param>
	/// <param name="value">The new value that was set.</param>
	/// <remarks>This method is intended for derived classes to implement custom logic after a value is updated.</remarks>
	protected virtual void OnSetted(TKey key, TValue value) { }

	/// <summary>
	/// Called before clearing all items from the collection.
	/// </summary>
	/// <remarks>This method is intended for derived classes to implement custom logic before clearing occurs.</remarks>
	protected virtual void OnClearing() {}

	/// <summary>
	/// Called after all items have been cleared from the collection.
	/// </summary>
	/// <remarks>This method is intended for derived classes to implement custom logic after clearing occurs.</remarks>
	protected virtual void OnCleared() {}

	/// <summary>
	/// Called before removing a key-value pair from the collection.
	/// </summary>
	/// <param name="key">The key being removed.</param>
	/// <param name="value">The value being removed.</param>
	/// <remarks>This method is intended for derived classes to implement custom logic before a removal occurs.</remarks>
	protected virtual void OnRemoving(TKey key, TValue value) { }

	/// <summary>
	/// Called after a key-value pair has been removed from the collection.
	/// </summary>
	/// <param name="key">The key that was removed.</param>
	/// <param name="value">The value that was removed.</param>
	/// <remarks>This method is intended for derived classes to implement custom logic after a removal occurs.</remarks>
	protected virtual void OnRemoved(TKey key, TValue value) { }

	/// <summary>
	/// Determines whether a key-value pair can be added to the collection.
	/// </summary>
	/// <param name="key">The key to check.</param>
	/// <param name="value">The value to check.</param>
	/// <returns>True if the pair can be added; otherwise, false.</returns>
	/// <remarks>This method is intended for derived classes to enforce custom addition constraints. The default implementation always returns true.</remarks>
	protected virtual bool CanAdd(TKey key, TValue value) => true;

	/// <summary>
	/// Gets a collection containing the keys in the dictionary.
	/// </summary>
	public ICollection<TKey> Keys => InnerDictionary.Keys;

	/// <summary>
	/// Gets a collection containing the values in the dictionary.
	/// </summary>
	public ICollection<TValue> Values => InnerDictionary.Values;

	/// <summary>
	/// Gets a value indicating whether the collection is read-only.
	/// </summary>
	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => InnerDictionary.IsReadOnly;

	/// <summary>
	/// Determines whether the collection contains an element with the specified key.
	/// </summary>
	/// <param name="key">The key to locate.</param>
	/// <returns>True if the key is found; otherwise, false.</returns>
	public bool ContainsKey(TKey key) => InnerDictionary.ContainsKey(key);

	/// <summary>
	/// Adds a key-value pair to the collection.
	/// </summary>
	/// <param name="item">The key-value pair to add.</param>
	/// <exception cref="ArgumentException">Thrown when the pair cannot be added, as determined by <see cref="CanAdd"/>.</exception>
	public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

	/// <summary>
	/// Determines whether the collection contains a specific key-value pair.
	/// </summary>
	/// <param name="item">The key-value pair to locate.</param>
	/// <returns>True if the pair is found; otherwise, false.</returns>
	/// <remarks>This implementation only checks the presence of the key, not the value equality.</remarks>
	public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);

	/// <summary>
	/// Copies the elements of the collection to an array, starting at the specified index.
	/// </summary>
	/// <param name="array">The destination array to copy to.</param>
	/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="arrayIndex"/> is negative.</exception>
	/// <exception cref="ArgumentException">Thrown when the destination array is too small to accommodate the elements.</exception>
	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => InnerDictionary.CopyTo(array, arrayIndex);

	/// <summary>
	/// Removes a specific key-value pair from the collection.
	/// </summary>
	/// <param name="item">The key-value pair to remove.</param>
	/// <returns>True if the pair was found and removed; otherwise, false.</returns>
	/// <remarks>This implementation only considers the key for removal, ignoring the value in <paramref name="item"/>.</remarks>
	public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

	/// <summary>
	/// Returns an enumerator that iterates through the collection (non-generic version).
	/// </summary>
	/// <returns>An <see cref="IEnumerator"/> that can be used to iterate through the collection.</returns>
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}