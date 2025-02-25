namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	/// <summary>
	/// Represents a dictionary-backed collection that uses a key/value pair mechanism.
	/// </summary>
	[Serializable]
	public abstract class KeyedCollection<TKey, TValue>(IDictionary<TKey, TValue> innerDictionary) : IDictionary<TKey, TValue>
	{
		protected KeyedCollection()
			: this(new Dictionary<TKey, TValue>())
		{
		}

		protected KeyedCollection(IEqualityComparer<TKey> comparer)
			: this(new Dictionary<TKey, TValue>(comparer))
		{
		}

		protected IDictionary<TKey, TValue> InnerDictionary { get; } = innerDictionary ?? throw new ArgumentNullException(nameof(innerDictionary));

		/// <summary>
		/// Adds the specified key/value pair to the dictionary.
		/// </summary>
		/// <param name="key">The key to add.</param>
		/// <param name="value">The value to add.</param>
		/// <exception cref="ArgumentException">Thrown when the key/value pair cannot be added.</exception>
		public virtual void Add(TKey key, TValue value)
		{
			if (!CanAdd(key, value))
				throw new ArgumentException();

			OnAdding(key, value);
			InnerDictionary.Add(key, value);
			OnAdded(key, value);
		}

		/// <summary>
		/// Gets or sets the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key of the value to get or set.</param>
		/// <returns>The value for the specified key.</returns>
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
		/// Removes the value with the specified key from the dictionary.
		/// </summary>
		/// <param name="key">The key of the element to remove.</param>
		/// <returns>True if the element was removed; otherwise false.</returns>
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
		/// Attempts to get the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key to locate.</param>
		/// <param name="value">When this method returns, contains the value for the key, if found.</param>
		/// <returns>True if the collection contains an element with the specified key; otherwise false.</returns>
		public virtual bool TryGetValue(TKey key, out TValue value)
		{
			return InnerDictionary.TryGetValue(key, out value);
		}

		/// <summary>
		/// Gets the number of elements in the collection.
		/// </summary>
		public virtual int Count => InnerDictionary.Count;

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator for the collection.</returns>
		public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return InnerDictionary.GetEnumerator();
		}

		protected virtual void OnAdding(TKey key, TValue value) { }
		protected virtual void OnAdded(TKey key, TValue value) { }

		protected virtual void OnSetting(TKey key, TValue value) { }
		protected virtual void OnSetted(TKey key, TValue value) { }

		protected virtual void OnClearing() {}
		protected virtual void OnCleared() {}

		protected virtual void OnRemoving(TKey key, TValue value) { }
		protected virtual void OnRemoved(TKey key, TValue value) { }

		protected virtual bool CanAdd(TKey key, TValue value) => true;

		/// <summary>
		/// Gets a collection containing the keys in the dictionary.
		/// </summary>
		public ICollection<TKey> Keys => InnerDictionary.Keys;

		/// <summary>
		/// Gets a collection containing the values in the dictionary.
		/// </summary>
		public ICollection<TValue> Values => InnerDictionary.Values;

		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => InnerDictionary.IsReadOnly;

		/// <summary>
		/// Determines whether the dictionary contains an element with the specified key.
		/// </summary>
		/// <param name="key">The key to locate.</param>
		/// <returns>True if the dictionary contains an element with the key; otherwise false.</returns>
		public bool ContainsKey(TKey key) => InnerDictionary.ContainsKey(key);

		/// <summary>
		/// Adds a key/value pair to the dictionary.
		/// </summary>
		/// <param name="item">The key/value pair to add.</param>
		public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

		/// <summary>
		/// Determines whether the dictionary contains a specific key/value pair.
		/// </summary>
		/// <param name="item">The key/value pair to locate.</param>
		/// <returns>True if the pair is found; otherwise false.</returns>
		public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);

		/// <summary>
		/// Copies the elements of the dictionary to an array, starting at the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional array that is the destination of the elements.</param>
		/// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => InnerDictionary.CopyTo(array, arrayIndex);

		/// <summary>
		/// Removes a key/value pair from the dictionary.
		/// </summary>
		/// <param name="item">The key/value pair to remove.</param>
		/// <returns>True if the pair was removed; otherwise false.</returns>
		public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}