namespace Ecng.Collections;

using System;
using System.Collections;
using System.Collections.Generic;

using Ecng.Common;

#if NET10_0
using SyncObject = System.Threading.Lock;
#endif

/// <summary>
/// Represents a thread-safe dictionary that provides synchronization for its operations.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
[Serializable]
public class SynchronizedDictionary<TKey, TValue> : ISynchronizedCollection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>
{
	private readonly IDictionary<TKey, TValue> _inner;

	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedDictionary{TKey, TValue}"/> class.
	/// </summary>
	public SynchronizedDictionary()
		: this(new Dictionary<TKey, TValue>())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedDictionary{TKey, TValue}"/> class with the specified initial capacity.
	/// </summary>
	/// <param name="capacity">The initial number of elements that the dictionary can contain.</param>
	public SynchronizedDictionary(int capacity)
		: this(new Dictionary<TKey, TValue>(capacity))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedDictionary{TKey, TValue}"/> class with the specified equality comparer.
	/// </summary>
	/// <param name="comparer">The equality comparer to use when comparing keys.</param>
	public SynchronizedDictionary(IEqualityComparer<TKey> comparer)
		: this(new Dictionary<TKey, TValue>(comparer))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedDictionary{TKey, TValue}"/> class with the specified initial capacity and equality comparer.
	/// </summary>
	/// <param name="capacity">The initial number of elements that the dictionary can contain.</param>
	/// <param name="comparer">The equality comparer to use when comparing keys.</param>
	public SynchronizedDictionary(int capacity, IEqualityComparer<TKey> comparer)
		: this(new Dictionary<TKey, TValue>(capacity, comparer))
	{
	}

	/// <summary>
	/// Gets the synchronization root object used to synchronize access to the dictionary.
	/// </summary>
	public SyncObject SyncRoot { get; } = new SyncObject();

	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedDictionary{TKey, TValue}"/> class with the specified inner dictionary.
	/// </summary>
	/// <param name="inner">The inner dictionary to wrap.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="inner"/> is null.</exception>
	protected SynchronizedDictionary(IDictionary<TKey, TValue> inner)
	{
		_inner = inner ?? throw new ArgumentNullException(nameof(inner));
	}

	/// <summary>
	/// Returns an enumerator that iterates through the dictionary.
	/// </summary>
	/// <returns>An enumerator that can be used to iterate through the dictionary.</returns>
	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		lock (SyncRoot)
			return _inner.GetEnumerator();
	}

	/// <summary>
	/// Returns an enumerator that iterates through the dictionary.
	/// </summary>
	/// <returns>An enumerator that can be used to iterate through the dictionary.</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	/// <summary>
	/// Adds a key-value pair to the dictionary.
	/// </summary>
	/// <param name="item">The key-value pair to add.</param>
	public void Add(KeyValuePair<TKey, TValue> item)
	{
		Add(item.Key, item.Value);
	}

	/// <summary>
	/// Removes all keys and values from the dictionary.
	/// </summary>
	public virtual void Clear()
	{
		lock (SyncRoot)
			_inner.Clear();
	}

	/// <summary>
	/// Determines whether the dictionary contains a specific key-value pair.
	/// </summary>
	/// <param name="item">The key-value pair to locate in the dictionary.</param>
	/// <returns><c>true</c> if the dictionary contains the key-value pair; otherwise, <c>false</c>.</returns>
	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		return ContainsKey(item.Key);
	}

	/// <summary>
	/// Copies the elements of the dictionary to an array, starting at a particular array index.
	/// </summary>
	/// <param name="array">The one-dimensional array that is the destination of the elements copied from the dictionary.</param>
	/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
	public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		lock (SyncRoot)
			_inner.CopyTo(array, arrayIndex);
	}

	/// <summary>
	/// Removes the first occurrence of a specific key-value pair from the dictionary.
	/// </summary>
	/// <param name="item">The key-value pair to remove.</param>
	/// <returns><c>true</c> if the key-value pair was successfully removed; otherwise, <c>false</c>.</returns>
	public bool Remove(KeyValuePair<TKey, TValue> item)
	{
		return Remove(item.Key);
	}

	/// <summary>
	/// Gets the number of key-value pairs contained in the dictionary.
	/// </summary>
	public int Count
	{
		get
		{
			lock (SyncRoot)
				return _inner.Count;
		}
	}

	/// <summary>
	/// Gets a value indicating whether the dictionary is read-only.
	/// </summary>
	public bool IsReadOnly => false;

	/// <summary>
	/// Determines whether the dictionary contains the specified key.
	/// </summary>
	/// <param name="key">The key to locate in the dictionary.</param>
	/// <returns><c>true</c> if the dictionary contains an element with the specified key; otherwise, <c>false</c>.</returns>
	public virtual bool ContainsKey(TKey key)
	{
		lock (SyncRoot)
			return _inner.ContainsKey(key);
	}

	/// <summary>
	/// Adds the specified key and value to the dictionary.
	/// </summary>
	/// <param name="key">The key of the element to add.</param>
	/// <param name="value">The value of the element to add.</param>
	public virtual void Add(TKey key, TValue value)
	{
		lock (SyncRoot)
			_inner.Add(key, value);
	}

	/// <summary>
	/// Removes the value with the specified key from the dictionary.
	/// </summary>
	/// <param name="key">The key of the element to remove.</param>
	/// <returns><c>true</c> if the element is successfully found and removed; otherwise, <c>false</c>.</returns>
	public virtual bool Remove(TKey key)
	{
		lock (SyncRoot)
			return _inner.Remove(key);
	}

	/// <summary>
	/// Gets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key of the value to get.</param>
	/// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter.</param>
	/// <returns><c>true</c> if the dictionary contains an element with the specified key; otherwise, <c>false</c>.</returns>
	public virtual bool TryGetValue(TKey key, out TValue value)
	{
		lock (SyncRoot)
			return _inner.TryGetValue(key, out value);
	}

	/// <summary>
	/// Gets or sets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key of the value to get or set.</param>
	/// <returns>The value associated with the specified key.</returns>
	public virtual TValue this[TKey key]
	{
		get
		{
			lock (SyncRoot)
				return _inner[key];
		}
		set
		{
			lock (SyncRoot)
				_inner[key] = value;
		}
	}

	/// <summary>
	/// Gets a collection containing the keys in the dictionary.
	/// </summary>
	public ICollection<TKey> Keys
	{
		get
		{
			lock (SyncRoot)
				return _inner.Keys;
		}
	}

	/// <summary>
	/// Gets a collection containing the values in the dictionary.
	/// </summary>
	public ICollection<TValue> Values
	{
		get
		{
			lock (SyncRoot)
				return _inner.Values;
		}
	}
}